using System;
using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models
{
    public enum ChatMessageType
    {
        Text = 0,
        Sticker = 1,
        File = 2,
        Image = 3
    }

    public class ChatMessage
    {
        public int Id { get; set; }

        [MaxLength(2000)]
        public string? Content { get; set; }

        public ChatMessageType MessageType { get; set; } = ChatMessageType.Text;

        [MaxLength(50)]
        public string? StickerId { get; set; }

        [MaxLength(500)]
        public string? FileName { get; set; }

        [MaxLength(500)]
        public string? FileUrl { get; set; }

        public long? FileSize { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public int RoomId { get; set; }
        public ChatRoom Room { get; set; } = null!;
    }
}