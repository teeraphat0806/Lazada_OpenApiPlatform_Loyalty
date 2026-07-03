using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Lazop.Domain.Enums;
using Lazop.Domain.RequestModels.WebhookRequestModels;
using Lazop.Domain.Interfaces.Services.WebhookServices;

namespace Web.Lazop.BackgroundServices
{
    public class LazadaWebhookQueueWorker : BackgroundService
    {
        private readonly ILogger<LazadaWebhookQueueWorker> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private const string QueueKey = "lazada:webhook:queue";

        public LazadaWebhookQueueWorker(
            ILogger<LazadaWebhookQueueWorker> logger,
            IServiceScopeFactory scopeFactory,
            IConnectionMultiplexer redis)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Lazada Webhook Background Queue Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Fetch message from Redis queue
                    var payload = await _db.ListRightPopAsync(QueueKey);
                    if (!payload.HasValue)
                    {
                        await Task.Delay(1000, stoppingToken);
                        continue;
                    }

                    string jsonBody = payload.ToString();
                    _logger.LogInformation("Fetched webhook message from Redis queue.");

                    await ProcessWebhookAsync(jsonBody, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while fetching or processing webhook from Redis queue.");
                    await Task.Delay(2000, stoppingToken); // Small delay on error to prevent CPU thrashing
                }
            }

            _logger.LogInformation("Lazada Webhook Background Queue Worker is stopping.");
        }

        private async Task ProcessWebhookAsync(string jsonBody, CancellationToken cancellationToken)
        {
            try
            {
                var data = JsonSerializer.Deserialize<LazadaWebhookRequest>(jsonBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data == null)
                {
                    _logger.LogWarning("Webhook payload deserialized to null: {Body}", jsonBody);
                    return;
                }

                // Deduplication Key: sellerId + orderId + status
                string? sellerId = data.SellerId;
                string? orderId = data.Data?.TradeOrderId ?? data.Data?.ReverseOrderId;
                string? status = data.Data?.OrderStatus ?? data.Data?.ReverseStatus;

                if (!string.IsNullOrWhiteSpace(sellerId) && !string.IsNullOrWhiteSpace(orderId) && !string.IsNullOrWhiteSpace(status))
                {
                    string dedupKey = $"lazada:webhook:processed:{sellerId}:{orderId}:{status}";
                    // Check and set duplicate key with 1-hour expiration
                    bool isNew = await _db.StringSetAsync(dedupKey, "1", TimeSpan.FromHours(1), When.NotExists);
                    if (!isNew)
                    {
                        _logger.LogWarning("Duplicate webhook message detected ({Key}). Skipping processing.", dedupKey);
                        return;
                    }
                }

                // Resolve scoped service inside scope
                using (var scope = _scopeFactory.CreateScope())
                {
                    var webhookService = scope.ServiceProvider.GetRequiredService<ILazadaWebhookService>();
                    var messageType = data.MessageType ?? data.MsgType;

                    if (messageType == WebPushType.TradeOrder)
                    {
                        _logger.LogInformation("Processing TradeOrder webhook asynchronously...");
                        webhookService.CreateOrUpdateOrder(data);
                    }
                    else if (messageType == WebPushType.ReverseOrder)
                    {
                        _logger.LogInformation("Processing ReverseOrder webhook asynchronously...");
                        webhookService.CreateOrUpdateReverseOrder(data);
                    }
                    else
                    {
                        _logger.LogWarning("Unknown webhook message type: {Type}", messageType);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process webhook message: {Body}", jsonBody);
            }
        }
    }
}
