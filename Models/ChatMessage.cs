using System;

namespace StudyGo.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public string EncryptedContent { get; set; }
        public DateTime SentAt { get; set; }
        public Chat Chat { get; set; }
        public User Sender { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}