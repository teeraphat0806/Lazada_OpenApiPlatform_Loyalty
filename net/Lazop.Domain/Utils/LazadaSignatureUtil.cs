using System;
using System.Security.Cryptography;
using System.Text;

namespace Lazop.Domain.Utils
{
    public static class LazadaSignatureUtil
    {
        /// <summary>
        /// ฟังก์ชันสร้าง Signature เพื่อใช้ตรวจสอบความถูกต้องของข้อมูลจาก Lazada
        /// </summary>
        /// <param name="baseString">ผลรวมของ {AppKey} + {MessageBody}</param>
        /// <param name="appSecret">รหัส AppSecret ของคุณ</param>
        /// <returns>Signature รูปแบบ Hex ตัวพิมพ์เล็ก</returns>
        public static string? GetSignature(string baseString, string appSecret)
        {
            if (string.IsNullOrEmpty(baseString) || string.IsNullOrEmpty(appSecret))
            {
                return null;
            }

            try
            {
                var keyBytes = Encoding.UTF8.GetBytes(appSecret);
                var baseBytes = Encoding.UTF8.GetBytes(baseString);

                using (var hmac = new HMACSHA256(keyBytes))
                {
                    var hashBytes = hmac.ComputeHash(baseBytes);
                    return ConvertToHexString(hashBytes);
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// แปลง Byte Array เป็น Hex String ตัวพิมพ์เล็ก
        /// </summary>
        private static string ConvertToHexString(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}