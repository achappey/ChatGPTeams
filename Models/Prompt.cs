
#nullable enable

using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models
{
    public class Prompt
    {
        public int Id { get; set; }

        public string Content { get; set; } = null!;
        
        public string? Title { get; set; }

        public string? Category { get; set; }

        public User Owner { get; set; } = null!;

        public Department? Department { get; set; }

        public Assistant? Assistant { get; set; }

        public IEnumerable<Function>? Functions { get; set; }

        public Visibility Visibility { get; set; }

    }

    
    public enum Visibility
    {
        Owner,
        Department,
        Everyone
    }
}
