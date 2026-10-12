using System;
using System.Collections.Generic;
using VzOverFlow.Models;

namespace VzOverFlow.Helpers
{
    public class BadgeMetadata
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "";
        public BadgeTier Tier { get; set; } = BadgeTier.Bronze;
    }

    public enum BadgeTier
    {
        Bronze,
        Silver,
        Gold
    }

    public static class BadgeHelper
    {
        private static readonly Dictionary<BadgeType, BadgeMetadata> _metadata = new()
        {
            // --- Questions ---
            { BadgeType.Curious, new BadgeMetadata { Name = "Tò mò", Description = "Đặt câu hỏi đầu tiên", Icon = "🧐", Tier = BadgeTier.Bronze } },
            { BadgeType.Inquisitive, new BadgeMetadata { Name = "Ham học", Description = "Đặt 25 câu hỏi hữu ích", Icon = "📚", Tier = BadgeTier.Silver } },
            { BadgeType.Questioner, new BadgeMetadata { Name = "Nhà khảo cứu", Description = "Đặt 100 câu hỏi đóng góp cho cộng đồng", Icon = "🔬", Tier = BadgeTier.Gold } },

            // --- Answers ---
            { BadgeType.Teacher, new BadgeMetadata { Name = "Người hướng dẫn", Description = "Câu trả lời đầu tiên", Icon = "👨‍🏫", Tier = BadgeTier.Bronze } },
            { BadgeType.Educator, new BadgeMetadata { Name = "Nhà giáo", Description = "Trả lời 30 câu hỏi", Icon = "🎓", Tier = BadgeTier.Silver } },
            { BadgeType.Scholar, new BadgeMetadata { Name = "Học giả", Description = "Trả lời 150 câu hỏi", Icon = "🏛️", Tier = BadgeTier.Gold } },

            // --- Votes ---
            { BadgeType.Supporter, new BadgeMetadata { Name = "Người ủng hộ", Description = "Vote 20 lần", Icon = "👍", Tier = BadgeTier.Bronze } },
            { BadgeType.Critic, new BadgeMetadata { Name = "Nhà phê bình", Description = "Vote 200 lần", Icon = "🗳️", Tier = BadgeTier.Silver } },
            { BadgeType.Judge, new BadgeMetadata { Name = "Thẩm phán", Description = "Vote 1000 lần", Icon = "⚖️", Tier = BadgeTier.Gold } },

            // --- Quality ---
            { BadgeType.NiceAnswer, new BadgeMetadata { Name = "Câu trả lời hay", Description = "Đạt 10 upvotes", Icon = "⭐", Tier = BadgeTier.Bronze } },
            { BadgeType.GoodAnswer, new BadgeMetadata { Name = "Câu trả lời tốt", Description = "Đạt 50 upvotes", Icon = "🌟", Tier = BadgeTier.Silver } },
            { BadgeType.GreatAnswer, new BadgeMetadata { Name = "Câu trả lời tuyệt vời", Description = "Đạt 100 upvotes", Icon = "🏆", Tier = BadgeTier.Gold } },
            { BadgeType.Accepted, new BadgeMetadata { Name = "Được công nhận", Description = "Có câu trả lời được chấp nhận", Icon = "✅", Tier = BadgeTier.Bronze } },

            // --- Activity ---
            { BadgeType.Enthusiast, new BadgeMetadata { Name = "Nhiệt huyết", Description = "Đăng nhập 7 ngày liên tiếp", Icon = "🔥", Tier = BadgeTier.Silver } },
            { BadgeType.Dedicated, new BadgeMetadata { Name = "Tận tụy", Description = "Đăng nhập 60 ngày liên tiếp", Icon = "🗓️", Tier = BadgeTier.Gold } },
            { BadgeType.Fanatic, new BadgeMetadata { Name = "Cuồng nhiệt", Description = "Đăng nhập 365 ngày liên tiếp", Icon = "🚀", Tier = BadgeTier.Gold } },
            { BadgeType.Popular, new BadgeMetadata { Name = "Phổ biến", Description = "Câu hỏi đạt 500 views", Icon = "👀", Tier = BadgeTier.Silver } },
            { BadgeType.Famous, new BadgeMetadata { Name = "Nổi tiếng", Description = "Câu hỏi đạt 5000 views", Icon = "🌟", Tier = BadgeTier.Gold } },
        };

        public static BadgeMetadata GetMetadata(BadgeType type)
        {
            return _metadata.ContainsKey(type) ? _metadata[type] : new BadgeMetadata { Name = type.ToString(), Description = "Huy hiệu bí ẩn", Icon = "❓", Tier = BadgeTier.Bronze };
        }
    }
}