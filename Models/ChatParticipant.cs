using System;

namespace StudyGo.Models
{
    public class ChatParticipant
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }

        public Chat Chat { get; set; }
        public User User { get; set; }
    }
}