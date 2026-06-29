using System;
using System.Text.Json;
using Lazop.Domain.Interfaces;
using Lazop.Domain.Interfaces.Services.SellerServices;
using Lazop.Domain.Models;
using Lazop.Domain.RequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Domain.Enums;
using Lazop.Service.ImplementServices.WebhookServices;

namespace Lazop.Service.ImplementServices.SellerServices
{
    public class SellerService : ISellerService
    {
        private readonly ILazopClient _lazopClient;

        public SellerService(ILazopClient lazopClient)
        {
            _lazopClient = lazopClient;
        }

        public LazadaSeller? GetLocalSellerInfo(string sellerShortCode)
        {
            if (InMemoryStorage.LazadaSellers.TryGetValue(sellerShortCode, out var seller))
            {
                return seller;
            }

            // Fallback: search by short_code field
            foreach (var s in InMemoryStorage.LazadaSellers.Values)
            {
                if (string.Equals(s.ShortCode, sellerShortCode, StringComparison.OrdinalIgnoreCase))
                {
                    return s;
                }
            }

            return null;
        }

        public LazopResponse GetSellerProfile(string sellerShortCode)
        {
            // Find active token from InMemoryStorage
            if (!InMemoryStorage.LazadaAccessTokens.TryGetValue(sellerShortCode, out var accessToken) || string.IsNullOrEmpty(accessToken.AccessToken))
            {
                return new LazopResponse { Code = "Error", Message = "Active Access Token not found for this seller." };
            }

            var request = new LazopRequest("/seller/get");
            request.SetHttpMethod("GET");

            var response = _lazopClient.Execute(request, accessToken.AccessToken);

            if (!response.IsError() && !string.IsNullOrEmpty(response.Body))
            {
                try
                {
                    using (var doc = JsonDocument.Parse(response.Body))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("data", out var dataElement))
                        {
                            string? sellerIdStr = dataElement.TryGetProperty("seller_id", out var sIdProp) ? sIdProp.GetRawText() : null;
                            
                            // Try to strip quotes if serialized as string
                            if (sellerIdStr != null && sellerIdStr.StartsWith("\"") && sellerIdStr.EndsWith("\""))
                            {
                                sellerIdStr = sellerIdStr.Substring(1, sellerIdStr.Length - 2);
                            }

                            if (long.TryParse(sellerIdStr, out long sellerId))
                            {
                                var seller = InMemoryStorage.LazadaSellers.GetOrAdd(sellerIdStr!, id => new LazadaSeller { Id = sellerId });
                                
                                seller.Name = dataElement.TryGetProperty("name", out var nProp) ? nProp.GetString() : null;
                                seller.Email = dataElement.TryGetProperty("email", out var eProp) ? eProp.GetString() : null;
                                seller.LogoUrl = dataElement.TryGetProperty("logo_url", out var lProp) ? lProp.GetString() : null;
                                seller.Location = dataElement.TryGetProperty("location", out var locProp) ? locProp.GetString() : null;
                                seller.ShortCode = dataElement.TryGetProperty("short_code", out var scProp) ? scProp.GetString() : null;
                                seller.CompanyName = dataElement.TryGetProperty("name_company", out var compProp) ? compProp.GetString() : null;
                                seller.CountryCode = dataElement.TryGetProperty("country_code", out var ccProp) ? ccProp.GetString()?.ToUpper() : null;

                                // Parse verified status
                                if (dataElement.TryGetProperty("verified", out var verProp))
                                {
                                    bool isVerified = verProp.ValueKind == JsonValueKind.True || 
                                                     (verProp.ValueKind == JsonValueKind.String && string.Equals(verProp.GetString(), "true", StringComparison.OrdinalIgnoreCase));
                                    seller.Verified = isVerified ? Affirmative.Yes : Affirmative.No;
                                }

                                // Parse cross border status
                                if (dataElement.TryGetProperty("cb", out var cbProp))
                                {
                                    bool isCb = cbProp.ValueKind == JsonValueKind.True || 
                                                (cbProp.ValueKind == JsonValueKind.String && string.Equals(cbProp.GetString(), "true", StringComparison.OrdinalIgnoreCase));
                                    seller.CrossBorder = isCb ? Affirmative.Yes : Affirmative.No;
                                }

                                // Parse status
                                if (dataElement.TryGetProperty("status", out var statProp))
                                {
                                    string? statusStr = statProp.GetString()?.ToUpper();
                                    seller.Status = statusStr switch
                                    {
                                        "ACTIVE" => ActiveStatus.Active,
                                        "INACTIVE" => ActiveStatus.Inactive,
                                        "DELETED" => ActiveStatus.Deleted,
                                        _ => ActiveStatus.Others
                                    };
                                }

                                seller.UpdatedAt = DateTime.UtcNow;
                                seller.AccessToken = accessToken;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Ignore parsing error
                }
            }

            return response;
        }
    }
}
