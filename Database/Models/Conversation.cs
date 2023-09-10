#nullable enable

using System;
using System.Collections.Generic;

namespace achappey.ChatGPTeams.Database.Models
{
    public class Conversation
    {
        public string Id { get; set; } = null!;

        public Assistant? Assistant { get; set; }

        public int? AssistantId { get; set; }


        public IList<Resource>? Resources { get; set; }

        public IList<Function>? Functions { get; set; }

        public float Temperature { get; set; }

        public ChatType ChatType { get; set; }

        public DateTimeOffset? CutOff { get; set; }
    }

}
