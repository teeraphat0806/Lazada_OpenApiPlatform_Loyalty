using System;
using Microsoft.AspNetCore.Mvc;
using Lazop.Domain.Interfaces;
using Lazop.Domain.RequestModels;
using Lazop.Domain.ViewModels;
using Lazop.Domain.Utils;

namespace Web.Lazop.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class LazadaAuthController : ControllerBase
    {
        private readonly ILazopClient _lazopClient;
        private readonly IConfiguration _configuration;

        public LazadaAuthController(ILazopClient lazopClient, IConfiguration configuration)
        {
            _lazopClient = lazopClient;
            _configuration = configuration;
        }

        [HttpGet("/")]
        public IActionResult GetDashboard()
        {
            string appKey = _configuration["LazadaConfig:AppKey"] ?? "139717";
            string redirectUri = _configuration["LazadaConfig:RedirectUri"] ?? "https://n7r50lzb-5000.asse.devtunnels.ms/api/lazada/callback";
            
            string realLazadaAuthUrl = $"https://auth.lazada.com/oauth/authorize?response_type=code&force_auth=true&redirect_uri={Uri.EscapeDataString(redirectUri)}&client_id={appKey}&country=th&terminal=sandbox";
            string tokenFromSandbox = _configuration["LazadaConfig:SandboxToken"] ?? "259dc77e767142488ff445aef7ec7000"; 
            
            string authUrl = string.IsNullOrEmpty(tokenFromSandbox) 
                ? realLazadaAuthUrl 
                : $"https://member.lazada.co.th/user/smartLogin?spm=a1zq7z.27200901.0.0.12527c73twVwAm&token={tokenFromSandbox}&redirect={Uri.EscapeDataString(realLazadaAuthUrl)}&traffic=seller&loginScene=TDBANK";

            string html = $@"
            <html>
            <head>
                <title>Lazada SDK OAuth Test Dashboard</title>
                <meta charset='utf-8'>
                <style>
                    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background-color: #f7f9fc; margin: 0; padding: 40px; color: #333; }}
                    .container {{ max-width: 650px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); }}
                    h2 {{ color: #f53d2d; margin-top: 0; font-size: 24px; border-bottom: 2px solid #f7f9fc; padding-bottom: 15px; }}
                    p {{ font-size: 15px; line-height: 1.6; color: #555; }}
                    ol {{ padding-left: 20px; color: #555; }}
                    li {{ margin-bottom: 10px; font-size: 14px; }}
                    .btn {{ display: inline-block; background-color: #f53d2d; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 15px; margin-top: 15px; transition: background-color 0.2s; }}
                    .btn:hover {{ background-color: #e02f20; }}
                    .info-box {{ background-color: #fff9e6; border-left: 4px solid #ffc107; padding: 15px; border-radius: 4px; margin-top: 20px; font-size: 13.5px; }}
                    code {{ background: #f1f3f5; padding: 2px 6px; border-radius: 4px; font-family: monospace; font-size: 13px; }}
                    .config {{ background-color: #f8f9fa; border: 1px solid #dee2e6; padding: 15px; border-radius: 6px; margin: 20px 0; font-family: monospace; font-size: 13px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <h2>🍊 Lazada OAuth Test Dashboard</h2>
                    <p>เว็บบอร์ดจำลองสำหรับทดสอบการรับสิทธิ์ (OAuth Connection) รันอยู่บนพอร์ต <b>5000</b> สำเร็จแล้ว!</p>
                    
                    <p><b>ข้อมูลการตั้งค่าปัจจุบันของคุณ:</b></p>
                    <div class='config'>
                        <b>App Key:</b> {appKey}<br/>
                        <b>Redirect URI (Callback):</b> {redirectUri}
                    </div>

                    <p><b>ขั้นตอนการทดสอบรัน:</b></p>
                    <ol>
                        <li>เปิด VS Code แถบ <b>Ports</b> แล้วเพิ่มพอร์ต <b>5000</b> เพื่อเปิด Public Forwarded URL</li>
                        <li>คัดลอกลิงก์ Forwarded URL ที่ได้จาก VS Code (เช่น <code>https://xxxx.devtunnels.ms</code>) แล้วมาแก้ไขในตัวแปร <code>redirectUri</code> ในไฟล์ <code>appsettings.json</code> ให้ตรงกัน</li>
                        <li>แก้ไขไฟล์ <code>appsettings.json</code> เพื่อใส่ App Key และ App Secret ของคุณให้ถูกต้อง</li>
                        <li>หลังจากรันและหน้าเว็บนี้แสดงข้อมูลตรงแล้ว ให้คลิกปุ่มเชื่อมต่อด้านล่างเพื่อเริ่มการขอสิทธิ์</li>
                    </ol>
                    
                    <div class='info-box'>
                        <strong>คำแนะนำการเชื่อมต่อ:</strong><br/>
                        เมื่อกดปุ่มด้านล่างสำเร็จ ระบบจะพาคุณไปยังหน้าล็อกอินของ Lazada และเมื่อกดยินยอม สิทธิ์จะถูกส่งกลับมาที่ Callback API ของคุณโดยอัตโนมัติ
                    </div>
                    
                    <a class='btn' href='{authUrl}' target='_blank'>เชื่อมต่อร้านค้า Lazada</a>
                </div>
            </body>
            </html>";

            return Content(html, "text/html");
        }

        [HttpGet("/api/lazada/callback")]
        public IActionResult Callback([FromQuery] string? code, [FromQuery(Name = "error_message")] string? errorMessage)
        {
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return BadRequest(new { Status = "Error", Message = $"Lazada Auth Error: {errorMessage}" });
            }
            
            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { Status = "Error", Message = "ไม่พบ Authorization Code (code parameter)" });
            }

            try
            {
                string appKey = _configuration["LazadaConfig:AppKey"] ?? "139717";
                string appSecret = _configuration["LazadaConfig:AppSecret"] ?? "TkOaWRFeJdPBd1iKPrYEkS3Lf4f8cAuP";
                
                var authClient = new global::Lazop.Service.ImplementServices.LazopServices.LazopClient(
                    UrlConstants.API_AUTHORIZATION_URL,
                    appKey,
                    appSecret
                );

                LazopRequest request = new LazopRequest("/auth/token/create");
                request.AddApiParameter("code", code);

                LazopResponse response = authClient.Execute(request);

                if (!response.IsError())
                {
                    string jsonBody = response.Body;
                    string successHtml = $@"
                    <html>
                    <head>
                        <title>Lazada Auth Success</title>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: -apple-system, BlinkMacSystemFont, sans-serif; background-color: #e8f5e9; padding: 40px; }}
                            .card {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border-top: 5px solid #2e7d32; }}
                            h2 {{ color: #2e7d32; margin-top: 0; }}
                            pre {{ background: #f5f5f5; padding: 15px; border-radius: 6px; font-family: monospace; font-size: 13px; overflow-x: auto; }}
                        </style>
                    </head>
                    <body>
                        <div class='card'>
                            <h2>🎉 เชื่อมต่อร้านค้า Lazada สำเร็จ!</h2>
                            <p>ได้รับ Access Token และข้อมูลร้านค้าจาก Lazada เรียบร้อยแล้ว:</p>
                            <pre>{jsonBody}</pre>
                            <p style='color: #666; font-size: 13px;'>คุณสามารถคัดลอก Access Token และ Refresh Token ด้านบนไปใช้พัฒนาต่อได้เลย</p>
                        </div>
                    </body>
                    </html>";
                    return Content(successHtml, "text/html");
                }
                else
                {
                    string failHtml = $@"
                    <html>
                    <head>
                        <title>Lazada Auth Failed</title>
                        <meta charset='utf-8'>
                        <style>
                            body {{ font-family: -apple-system, BlinkMacSystemFont, sans-serif; background-color: #ffebee; padding: 40px; }}
                            .card {{ max-width: 600px; margin: 0 auto; background: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border-top: 5px solid #c62828; }}
                            h2 {{ color: #c62828; margin-top: 0; }}
                        </style>
                    </head>
                    <body>
                        <div class='card'>
                            <h2>❌ การแลก Token ล้มเหลว</h2>
                            <p>เกิดข้อผิดพลาดจาก Lazada Gateway:</p>
                            <p><b>รหัสข้อผิดพลาด:</b> {response.Code}</p>
                            <p><b>ข้อความ:</b> {response.Message}</p>
                            <p><b>ผลลัพธ์ตอบกลับดิบ:</b> {response.Body}</p>
                        </div>
                    </body>
                    </html>";
                    return Content(failHtml, "text/html");
                }
            }
            catch (Exception ex)
            {
                return Problem($"เกิดข้อผิดพลาดระดับระบบในการแลก Token: {ex.Message}");
            }
        }
    }
}
