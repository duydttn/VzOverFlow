using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using QRCoder;

namespace VzOverFlow.Services
{
    public interface IAuthenticatorService
    {
      /// <summary>
        /// Generate a new authenticator key for user
        /// </summary>
        string GenerateAuthenticatorKey();

        /// <summary>
        /// Generate QR Code URI for authenticator apps (Google Authenticator, Microsoft Authenticator, etc.)
        /// </summary>
        string GenerateQrCodeUri(string email, string authenticatorKey);

        /// <summary>
        /// Generate QR Code as Base64 image
        /// </summary>
        string GenerateQrCodeImage(string qrCodeUri);

        /// <summary>
        /// Validate TOTP code from authenticator app
        /// </summary>
        bool ValidateTwoFactorCode(string authenticatorKey, string code);

        /// <summary>
        /// Format authenticator key for manual entry (groups of 4 characters)
        /// </summary>
        string FormatKeyForManualEntry(string key);
    }

    public class AuthenticatorService : IAuthenticatorService
    {
        private const string Issuer = "VzOverFlow";

    public string GenerateAuthenticatorKey()
     {
    // Generate a random 20-byte (160-bit) key
      var key = new byte[20];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
        }

    // Convert to Base32 (standard for TOTP)
            return Base32Encode(key);
        }

   public string GenerateQrCodeUri(string email, string authenticatorKey)
        {
          // Format: otpauth://totp/{Issuer}:{Email}?secret={Key}&issuer={Issuer}
       var encodedEmail = Uri.EscapeDataString(email);
    var encodedIssuer = Uri.EscapeDataString(Issuer);

return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={authenticatorKey}&issuer={encodedIssuer}";
     }

     public string GenerateQrCodeImage(string qrCodeUri)
  {
      using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
       
            var qrCodeBytes = qrCode.GetGraphic(20);
            return $"data:image/png;base64,{Convert.ToBase64String(qrCodeBytes)}";
        }

  public bool ValidateTwoFactorCode(string authenticatorKey, string code)
 {
    if (string.IsNullOrWhiteSpace(code) || code.Length != 6)
            {
 return false;
       }

            // Remove spaces and convert to uppercase
   var cleanKey = authenticatorKey.Replace(" ", "").ToUpper();
         
     // Get current Unix timestamp (30-second intervals)
         var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
      var timeStep = unixTimestamp / 30;

    // Check current time step and ±1 step (90 seconds window)
  for (int i = -1; i <= 1; i++)
            {
                var totp = GenerateTOTP(cleanKey, timeStep + i);
         if (totp == code)
  {
         return true;
    }
     }

    return false;
        }

        public string FormatKeyForManualEntry(string key)
        {
        // Format as groups of 4: ABCD EFGH IJKL MNOP
         var formatted = new StringBuilder();
      for (int i = 0; i < key.Length; i++)
       {
         if (i > 0 && i % 4 == 0)
         {
 formatted.Append(' ');
    }
       formatted.Append(key[i]);
        }
            return formatted.ToString();
        }

  #region Private Helper Methods

        private string GenerateTOTP(string key, long timeStep)
        {
    var keyBytes = Base32Decode(key);
            var timeBytes = BitConverter.GetBytes(timeStep);
    
   if (BitConverter.IsLittleEndian)
    {
                Array.Reverse(timeBytes);
   }

       using var hmac = new HMACSHA1(keyBytes);
   var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation
       var offset = hash[hash.Length - 1] & 0x0F;
            var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
             | ((hash[offset + 2] & 0xFF) << 8)
  | (hash[offset + 3] & 0xFF);

            var otp = binary % 1000000;
            return otp.ToString("D6");
        }

     private static string Base32Encode(byte[] data)
        {
   const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
     var result = new StringBuilder();
    
      for (int i = 0; i < data.Length; i += 5)
   {
    int byteCount = Math.Min(5, data.Length - i);
 ulong buffer = 0;
        
    for (int j = 0; j < byteCount; j++)
        {
       buffer = (buffer << 8) | data[i + j];
     }
   
       int bitCount = byteCount * 8;
   while (bitCount > 0)
              {
           int index = (int)((buffer >> (bitCount - 5)) & 0x1F);
        result.Append(base32Chars[index]);
               bitCount -= 5;
            }
            }
            
     return result.ToString();
        }

      private static byte[] Base32Decode(string encoded)
        {
      const string base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        encoded = encoded.ToUpper().Replace(" ", "").Replace("-", "");
            
 var bits = new StringBuilder();
          foreach (char c in encoded)
     {
                int value = base32Chars.IndexOf(c);
 if (value < 0) continue;
    bits.Append(Convert.ToString(value, 2).PadLeft(5, '0'));
     }
            
    var result = new List<byte>();
            for (int i = 0; i + 8 <= bits.Length; i += 8)
            {
    result.Add(Convert.ToByte(bits.ToString(i, 8), 2));
   }
    
     return result.ToArray();
        }

   #endregion
    }
}
