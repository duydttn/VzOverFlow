using System.Linq;
using System.Text.RegularExpressions;

namespace VzOverFlow.Helpers
{
    public static class SpamChecker
    {
        private static readonly string[] SpamPatterns = 
        {
            @"nhà\s*[\W_]*cái", @"cá\s*[\W_]*độ", @"s\s*[\W_]*e\s*[\W_]*x", @"18\+", @"viagra", @"drug",
            @"bet\s*[\W_]*88", @"kubet", @"w\s*[\W_]*88", @"f\s*[\W_]*88", @"lô\s*[\W_]*đề", @"xổ\s*[\W_]*số",
            @"tài\s*[\W_]*xỉu", @"g\s*[\W_]*a\s*[\W_]*m\s*[\W_]*b\s*[\W_]*l\s*[\W_]*e", @"c\s*[\W_]*a\s*[\W_]*s\s*[\W_]*i\s*[\W_]*n\s*[\W_]*o",
            @"http(s)?://(?!([\w-]+\.)?vn/|([\w-]+\.)?com/|([\w-]+\.)?org/|([\w-]+\.)?net/|([\w-]+\.)?edu/|([\w-]+\.)?gov/)"
        };

        public static bool ContainsSpam(string content, int userReputation = 0)
        {
            if (string.IsNullOrEmpty(content)) return false;

            // 1. Trusted User Bypass (Reputation > 50)
            bool isTrusted = userReputation > 50;

            var lowerContent = content.ToLower();

            // 2. Regex Keyword Check
            foreach (var pattern in SpamPatterns)
            {
                if (Regex.IsMatch(lowerContent, pattern, RegexOptions.IgnoreCase))
                    return true;
            }

            // 3. Structure Analysis (Skip for trusted users)
            if (!isTrusted)
            {
                 // Check caps lock ratio (> 70% caps for long content)
                 if (content.Length > 20 && GetCapsLockRatio(content) > 0.7)
                     return true;

                 // Check link count (> 3 links)
                 if (Regex.Matches(content, @"http(s)?://").Count > 3)
                     return true;
                     
                 // Check rapid character repetition (e.g., "aaaaa"), ignoring whitespace
                //  if (Regex.IsMatch(content, @"([^\s])\1{4,}"))
                //      return true;
            }

            return false;
        }

        private static double GetCapsLockRatio(string content)
        {
            int upperCount = content.Count(char.IsUpper);
            int letterCount = content.Count(char.IsLetter);
            return letterCount == 0 ? 0 : (double)upperCount / letterCount;
        }
    }
}