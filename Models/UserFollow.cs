using System;

namespace VzOverFlow.Models
{
    public class UserFollow
    {
        public int FollowerId { get; set; }
        public User Follower { get; set; } = null!;

        public int FollowedUserId { get; set; }
        public User FollowedUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}