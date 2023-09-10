
#nullable enable

using System.Collections.Generic;

namespace achappey.ChatGPTeams.Database.Models
{
    public class Prompt
    {
        public int Id { get; set; }

        public string Content { get; set; } = null!;
        
        public string Title { get; set; } = null!;

        public string? Category { get; set; }

        public User Owner { get; set; } = null!;

        public string OwnerId { get; set; } = null!;

        public Department? Department { get; set; }

        public int? DepartmentId { get; set; }

        public Assistant? Assistant { get; set; }

        public int? AssistantId { get; set; }

        public IEnumerable<Function>? Functions { get; set; }

        public Visibility Visibility { get; set; }

    }
}
