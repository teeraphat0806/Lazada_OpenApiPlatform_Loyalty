using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Lazop.Domain.Interfaces;
using Lazop.Domain.RequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Domain.Utils;
using Lazop.Domain.Interfaces.Services.ProductServices;
using Lazop.Domain.Interfaces.Services.OrderServices;
using Lazop.Domain.Interfaces.Services.WebhookServices;
using Lazop.Service.ImplementServices.LazopServices;
using Lazop.Service.ImplementServices.ProductServices;
using Lazop.Service.ImplementServices.OrderServices;
using Lazop.Service.ImplementServices.WebhookServices;

namespace Web.Lazop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Fetch settings from configuration (appsettings.json) with fallback to original values
            string appKey = builder.Configuration["LazadaConfig:AppKey"] ?? "139831";
            string appSecret = builder.Configuration["LazadaConfig:AppSecret"] ?? "8fbYLuuWYmrjEaWsqRctpQBwweJjTI1d";

            // Register MVC Controllers
            builder.Services.AddControllers();

            // Register Lazada API infrastructure in DI
            builder.Services.AddScoped<ILazopClient>(sp => new LazopClient(
                UrlConstants.API_GATEWAY_URL_TH, // Use Thailand gateway for business APIs!
                appKey,
                appSecret
            ));

            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddScoped<ILazadaWebhookService, LazadaWebhookService>();

            var app = builder.Build();

            // Enable Controller Routing
            app.MapControllers();

            app.Run();
        }
    }
}
