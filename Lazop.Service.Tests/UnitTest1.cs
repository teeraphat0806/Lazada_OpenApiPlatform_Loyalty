using Xunit;
using Moq;
using Microsoft.Extensions.Configuration;
using Lazop.Domain.Interfaces;
using Lazop.Domain.RequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Service.ImplementServices.AuthServices;
using Lazop.Service.ImplementServices.WebhookServices;

namespace Lazop.Service.Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<ILazopClient> _mockLazopClient;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _mockLazopClient = new Mock<ILazopClient>();
            _mockConfiguration = new Mock<IConfiguration>();

            // ตั้งค่า mock Configuration
            _mockConfiguration.Setup(c => c["LazadaConfig:AppKey"]).Returns("123456");
            _mockConfiguration.Setup(c => c["LazadaConfig:RedirectUri"]).Returns("https://example.com/callback");

            _authService = new AuthService(_mockLazopClient.Object, _mockConfiguration.Object);
        }

        [Fact]
        public void GetAuthorizationUrl_ShouldReturnCorrectUrl()
        {
            // Act
            string url = _authService.GetAuthorizationUrl();

            // Assert
            Assert.Contains("client_id=123456", url);
            Assert.Contains("redirect_uri=https%3A%2F%2Fexample.com%2Fcallback", url);
        }

        [Fact]
        public void CreateToken_Success_ShouldSaveTokenToInMemoryStorage()
        {
            // Arrange
            string code = "dummy_auth_code";
            string mockResponseBody = @"{
                ""access_token"": ""mock_access_token_123"",
                ""refresh_token"": ""mock_refresh_token_123"",
                ""expires_in"": 86400,
                ""refresh_expires_in"": 2592000,
                ""country_user_info"": [
                    {
                        ""seller_id"": ""99999"",
                        ""user_id"": ""88888"",
                        ""country"": ""th"",
                        ""short_code"": ""TH12345""
                    }
                ]
            }";

            _mockLazopClient.Setup(x => x.Execute(It.IsAny<LazopRequest>()))
                            .Returns(new LazopResponse { Body = mockResponseBody, Code = "0" });

            // Act
            var response = _authService.CreateToken(code);

            // Assert
            Assert.False(response.IsError());
            Assert.True(InMemoryStorage.LazadaAccessTokens.ContainsKey("99999"));
            Assert.Equal("mock_access_token_123", InMemoryStorage.LazadaAccessTokens["99999"].AccessToken);
            Assert.True(InMemoryStorage.LazadaSellers.ContainsKey("99999"));
        }
    }
}
