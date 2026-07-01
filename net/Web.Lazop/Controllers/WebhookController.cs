using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Lazop.Domain.Enums;
using Lazop.Domain.Models;
using Lazop.Domain.RequestModels.WebhookRequestModels;
using Lazop.Domain.Utils;
using Lazop.Domain.Interfaces.Services.WebhookServices;
using Lazop.Service.ImplementServices.WebhookServices;

namespace Web.Lazop.Controllers
{
    [ApiController]
    [Route("api/lazada")]
    public class LazadaWebhookController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<LazadaWebhookController> _logger;
        private readonly ILazadaWebhookService _webhookService;
        private readonly string _appKey;
        private readonly string _appSecret;

        public LazadaWebhookController(
            IConfiguration configuration, 
            ILogger<LazadaWebhookController> logger,
            ILazadaWebhookService webhookService)
        {
            _configuration = configuration;
            _logger = logger;
            _webhookService = webhookService;
            _appKey = _configuration["LazadaConfig:AppKey"] ?? "139831";
            _appSecret = _configuration["LazadaConfig:AppSecret"] ?? "eWWFgFKgXLHm8cD9Ox68cuHno2Z3jZV3";
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveNotification()
        {
            // 1. ดึงข้อมูล Signature จาก Header "Authorization"
            if (!Request.Headers.TryGetValue("Authorization", out var incomingSignatureHeader))
            {
                _logger.LogWarning("Lazada Webhook: Missing Authorization header");
                return BadRequest("Missing Signature.");
            }

            string? signature = incomingSignatureHeader.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature))
            {
                return BadRequest("Missing Signature.");
            }

            // 2. อ่านเนื้อหา JSON Body ที่ส่งมาจาก Lazada
            string jsonBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                jsonBody = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(jsonBody))
            {
                return BadRequest("Missing payload.");
            }

            _logger.LogInformation("Lazada Webhook Body: {Body}", jsonBody);
            _logger.LogInformation("Lazada Webhook AppKey: {AppKey}", _appKey);
            _logger.LogInformation("Lazada Webhook AppSecret: {AppSecret}", _appSecret);

            // 3. สร้าง Base String ตามสูตร: {AppKey} + {MessageBody}
            string baseString = _appKey + jsonBody;

            // 4. คำนวณ Signature ฝั่งเราเพื่อไปเปรียบเทียบ
            string? calculatedSignature = LazadaSignatureUtil.GetSignature(baseString, _appSecret);

            // 5. ตรวจสอบความถูกต้อง (เปรียบเทียบแบบ Case-Insensitive)
            if (calculatedSignature == null || !calculatedSignature.Equals(signature, StringComparison.OrdinalIgnoreCase))
            {
                string maskedSecret = string.IsNullOrEmpty(_appSecret) ? "null" : $"{_appSecret.Substring(0, Math.Min(3, _appSecret.Length))}...{_appSecret.Substring(Math.Max(0, _appSecret.Length - 3))}";
                
                var headers = string.Join("; ", Request.Headers.Select(h => $"{h.Key}={h.Value}"));
                var queryParams = string.Join("; ", Request.Query.Select(q => $"{q.Key}={q.Value}"));
                
                _logger.LogWarning("Lazada Webhook DEBUG: AppKey={AppKey}, AppSecret={AppSecret}", _appKey, maskedSecret);
                _logger.LogWarning("Lazada Webhook DEBUG: Headers: {Headers}", headers);
                _logger.LogWarning("Lazada Webhook DEBUG: QueryParams: {QueryParams}", queryParams);
                _logger.LogWarning("Lazada Webhook DEBUG: Body Length={Length}, Body='[START]{Body}[END]'", jsonBody.Length, jsonBody);
                _logger.LogWarning("Lazada Webhook: Signature mismatch. Expected: {Expected}, Received: {Received}", calculatedSignature, signature);
                return BadRequest("Signature not matched.");
            }

            // Log ข้อมูลเมื่อทำการเปิดตั้งค่าไว้
            if (_configuration.GetValue<bool>("LazadaConfig:LogMessage", true))
            {
                _logger.LogInformation("Lazada Webhook received valid message.");
                _logger.LogInformation("Lazada Webhook Body: {Body}", jsonBody);
            }

            try
            {
                // บันทึก Log ข้อมูล Webhook ลง Memory Storage
                InMemoryStorage.LazadaMessages.Add(new LazadaMessage
                {
                    Action = "webhook",
                    Response = jsonBody,
                    CreatedTime = DateTime.UtcNow
                });

                // Deserialize JSON payload
                var data = JsonSerializer.Deserialize<LazadaWebhookRequest>(jsonBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (data != null)
                {
                    var messageType = data.MessageType ?? data.MsgType;

                    if (messageType == WebPushType.TradeOrder)
                    {
                        _webhookService.CreateOrUpdateOrder(data);
                    }
                    else if (messageType == WebPushType.ReverseOrder)
                    {
                        _webhookService.CreateOrUpdateReverseOrder(data);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lazada Webhook processing error");
            }

            // ตอบกลับด้วยสถานะ 200 OK ทันทีภายใน 500ms ตามกฎข้อบังคับของ Lazada Webhook
            return Ok();
        }

        /// <summary>
        /// ดึงข้อมูลทั้งหมดที่บันทึกอยู่ในหน่วยความจำชั่วคราว (ใช้เพื่อการตรวจสอบผลลัพธ์)
        /// Route: GET /api/lazada/webhook/debug
        /// </summary>
        [HttpGet("webhook/debug")]
        public IActionResult GetDebugData()
        {
            return Ok(new
            {
                Messages = InMemoryStorage.LazadaMessages.ToList(),
                Orders = InMemoryStorage.LazadaOrders.Values.ToList(),
                ReverseOrders = InMemoryStorage.LazadaReverseOrders.Values.ToList()
            });
        }
    }
}