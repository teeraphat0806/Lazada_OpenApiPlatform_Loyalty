using Lazop.Domain.Interfaces.Services.AuthServices;
using Lazop.Domain.Models;
using Lazop.Domain.Interfaces;
using Lazop.Domain.RequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Service.ImplementServices.WebhookServices;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Lazop.Service.ImplementServices.AuthServices
{
    public class AuthService : IAuthService
    {
        private readonly ILazopClient _lazopClient;
        private readonly IConfiguration _configuration;

        public AuthService(ILazopClient lazopClient, IConfiguration configuration)
        {
            _lazopClient = lazopClient;
            _configuration = configuration;
        }

        public string GetAuthorizationUrl()
        {
            string appKey = _configuration["LazadaConfig:AppKey"] ?? string.Empty;
            string redirectUri = _configuration["LazadaConfig:RedirectUri"] ?? string.Empty;

            return $"https://auth.lazada.com/oauth/authorize?response_type=code&force_auth=true&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={appKey}";
        }

        public LazopResponse CreateToken(string code)
        {
            var request = new LazopRequest("/auth/token/create");
            request.SetHttpMethod("POST");
            request.AddApiParameter("code", code);

            var response = _lazopClient.Execute(request);

            if (!response.IsError() && !string.IsNullOrEmpty(response.Body))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(response.Body))
                    {
                        var root = doc.RootElement;

                        // Parse country_user_info array
                        JsonElement userInfoElement = default;
                        if (root.TryGetProperty("country_user_info", out var listProp) && listProp.ValueKind == JsonValueKind.Array && listProp.GetArrayLength() > 0)
                        {
                            userInfoElement = listProp[0];
                        }

                        if (userInfoElement.ValueKind != JsonValueKind.Undefined)
                        {
                            string? sellerIdStr = userInfoElement.TryGetProperty("seller_id", out var sIdProp) ? sIdProp.GetString() : null;
                            if (long.TryParse(sellerIdStr, out long sellerId))
                            {
                                var seller = InMemoryStorage.LazadaSellers.GetOrAdd(sellerIdStr!, id => new LazadaSeller { Id = sellerId });
                                seller.UserId = userInfoElement.TryGetProperty("user_id", out var uIdProp) && long.TryParse(uIdProp.GetString(), out long uId) ? uId : null;
                                seller.CountryCode = userInfoElement.TryGetProperty("country", out var countryProp) ? countryProp.GetString()?.ToUpper() : null;
                                seller.ShortCode = userInfoElement.TryGetProperty("short_code", out var scProp) ? scProp.GetString() : null;
                                seller.UpdatedAt = DateTime.UtcNow;

                                // Common Token Data
                                string? accessToken = root.TryGetProperty("access_token", out var atProp) ? atProp.GetString() : null;
                                string? refreshToken = root.TryGetProperty("refresh_token", out var rtProp) ? rtProp.GetString() : null;
                                int expiresSeconds = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 0;
                                int refreshExpiresSeconds = root.TryGetProperty("refresh_expires_in", out var reProp) ? reProp.GetInt32() : 0;

                                var token = InMemoryStorage.LazadaAccessTokens.GetOrAdd(sellerIdStr!, id => new LazadaAccessToken { SellerId = sellerId });
                                token.AccessToken = accessToken;
                                token.RefreshToken = refreshToken;
                                token.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresSeconds);
                                token.RefreshExpiresAt = DateTime.UtcNow.AddSeconds(refreshExpiresSeconds);
                                token.Code = code;
                                token.UserInfoJson = userInfoElement.GetRawText();
                                token.CountryCode = seller.CountryCode;
                                token.AccountId = root.TryGetProperty("account_id", out var accIdProp) ? accIdProp.GetString() : null;
                                token.Account = root.TryGetProperty("account", out var accProp) ? accProp.GetString() : null;
                                token.AccountPlatform = root.TryGetProperty("account_platform", out var platProp) ? platProp.GetString() : null;
                                token.UpdatedAt = DateTime.UtcNow;

                                // Bind token navigation property
                                seller.AccessToken = token;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // If parsing failed, we still return the response.
                }
            }

            return response;
        }

        public LazopResponse RefreshToken(string sellerShortCode)
        {
            if (!InMemoryStorage.LazadaAccessTokens.TryGetValue(sellerShortCode, out var existingToken) || string.IsNullOrEmpty(existingToken.RefreshToken))
            {
                return new LazopResponse { Code = "Error", Message = "No refresh token found locally for this seller." };
            }

            var request = new LazopRequest("/auth/token/refresh");
            request.SetHttpMethod("POST");
            request.AddApiParameter("refresh_token", existingToken.RefreshToken);

            var response = _lazopClient.Execute(request);

            if (!response.IsError() && !string.IsNullOrEmpty(response.Body))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(response.Body))
                    {
                        var root = doc.RootElement;

                        // Refresh returns country_user_info_list array
                        JsonElement userInfoElement = default;
                        if (root.TryGetProperty("country_user_info_list", out var listProp) && listProp.ValueKind == JsonValueKind.Array && listProp.GetArrayLength() > 0)
                        {
                            userInfoElement = listProp[0];
                        }

                        if (userInfoElement.ValueKind != JsonValueKind.Undefined)
                        {
                            string? sellerIdStr = userInfoElement.TryGetProperty("seller_id", out var sIdProp) ? sIdProp.GetString() : null;
                            if (long.TryParse(sellerIdStr, out long sellerId))
                            {
                                string? accessToken = root.TryGetProperty("access_token", out var atProp) ? atProp.GetString() : null;
                                string? refreshToken = root.TryGetProperty("refresh_token", out var rtProp) ? rtProp.GetString() : null;
                                int expiresSeconds = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 0;
                                int refreshExpiresSeconds = root.TryGetProperty("refresh_expires_in", out var reProp) ? reProp.GetInt32() : 0;

                                existingToken.AccessToken = accessToken;
                                existingToken.RefreshToken = refreshToken;
                                existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresSeconds);
                                existingToken.RefreshExpiresAt = DateTime.UtcNow.AddSeconds(refreshExpiresSeconds);
                                existingToken.UserInfoJson = userInfoElement.GetRawText();
                                existingToken.CountryCode = userInfoElement.TryGetProperty("country", out var countryProp) ? countryProp.GetString()?.ToUpper() : null;
                                existingToken.AccountId = root.TryGetProperty("account_id", out var accIdProp) ? accIdProp.GetString() : null;
                                existingToken.Account = root.TryGetProperty("account", out var accProp) ? accProp.GetString() : null;
                                existingToken.AccountPlatform = root.TryGetProperty("account_platform", out var platProp) ? platProp.GetString() : null;
                                existingToken.UpdatedAt = DateTime.UtcNow;

                                if (InMemoryStorage.LazadaSellers.TryGetValue(sellerIdStr!, out var seller))
                                {
                                    seller.AccessToken = existingToken;
                                    seller.UpdatedAt = DateTime.UtcNow;
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore and return response
                }
            }

            return response;
        }
    }
}
