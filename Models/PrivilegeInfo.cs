using System.Collections.Generic;

namespace VzOverFlow.Models
{
    public enum Privilege
    {
        VoteUp = 1,
        Comment = 2,
        VoteDown = 3,
        CreateTag = 4,
        EditPost = 5,
        ModerationTools = 6
    }

    public class PrivilegeInfo
    {
        public Privilege Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RequiredReputation { get; set; }
        public bool IsUnlocked { get; set; }
    }

    public static class PrivilegeDefinitions
    {
        public static List<PrivilegeInfo> GetAll(int currentReputation = 0)
        {
            var privileges = new List<PrivilegeInfo>
            {
                new PrivilegeInfo 
                { 
                    Type = Privilege.VoteUp, 
                    Name = "Bình chọn lên (Upvote)", 
                    Description = "Cho phép bạn bình chọn lên cho các câu hỏi và câu trả lời hữu ích.", 
                    RequiredReputation = 15 
                },
                new PrivilegeInfo 
                { 
                    Type = Privilege.Comment, 
                    Name = "Bình luận", 
                    Description = "Cho phép bạn để lại bình luận trên bất kỳ bài viết nào.", 
                    RequiredReputation = 50 
                },
                new PrivilegeInfo 
                { 
                    Type = Privilege.VoteDown, 
                    Name = "Bình chọn xuống (Downvote)", 
                    Description = "Cho phép bạn bình chọn xuống cho các nội dung kém chất lượng.", 
                    RequiredReputation = 125 
                },
                new PrivilegeInfo 
                { 
                    Type = Privilege.CreateTag, 
                    Name = "Tạo thẻ mới", 
                    Description = "Cho phép bạn tạo các thẻ (tag) mới chưa có trong hệ thống.", 
                    RequiredReputation = 200 
                },
                new PrivilegeInfo 
                { 
                    Type = Privilege.EditPost, 
                    Name = "Chỉnh sửa bài viết", 
                    Description = "Cho phép bạn chỉnh sửa tiêu đề và nội dung của các bài viết cộng đồng.", 
                    RequiredReputation = 2000 
                }
            };

            foreach (var p in privileges)
            {
                p.IsUnlocked = currentReputation >= p.RequiredReputation;
            }

            return privileges;
        }
    }
}