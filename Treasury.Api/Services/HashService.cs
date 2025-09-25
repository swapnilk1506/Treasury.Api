using System.Security.Cryptography;
using System.Text;

namespace Treasury.Api.Services
{
    public class HashService{public string Compute(string input) { using var sha = SHA256.Create();var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));return Convert.ToHexString(bytes);/*uppercase hex*/  }
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return "";
            }
            var cleaned = new string(s.ToUpperInvariant()
                .Where(ch => char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)).ToArray());
            return string.Join(' ', cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }
                                
    }
}
    