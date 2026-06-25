using System;
using System.Collections.Generic;
using StudyGo.Enums;

namespace StudyGo.Models
{
    public class Chat
    {
        public Guid Id { get; set; }
        public ChatType Type { get; set; }

        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
        public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();

        public string? Name { get; set; }
    }
}
