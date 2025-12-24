using System.Linq;

namespace VzOverFlow.Helpers
{
    public static class SpamChecker
    {
        private static readonly string[] Blacklist = { "nhà cái", "cá độ", "sex", "18+", "viagra" , "drug"};

        public static bool ContainsSpam(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            var lowerContent = content.ToLower();
            return Blacklist.Any(word => lowerContent.Contains(word));
        }
    }
}