using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace achappey.ChatGPTeams.Database.Models
{

    public class Assistant
    {

        public string Name { get; set; }

        public string Prompt { get; set; }

        public Department Department { get; set; }
        
        public int? DepartmentId { get; set; }

        public int  Id { get; set; }

        public float Temperature { get; set; }

        public IEnumerable<Function> Functions { get; set; }

        public IEnumerable<Conversation> Conversations { get; set; }

        public IEnumerable<Resource> Resources { get; set; }

        public User Owner { get; set; } = null!;

        public string OwnerId { get; set; } = null!;

        public Model Model { get; set; }

        public int ModelId { get; set; }
        
        public Visibility Visibility { get; set; }

        public Assistant()
        {
        }

    }
    
    public enum Visibility
    {
        Owner,
        Department,
        Everyone
    }
}
