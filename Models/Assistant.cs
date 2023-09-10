using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace achappey.ChatGPTeams.Models
{

    public class Assistant
    {

        public string Name { get; set; }

        public string Prompt { get; set; }

        public Department Department { get; set; }

        public int Id { get; set; }

        public float Temperature { get; set; }

        public IEnumerable<Function> Functions { get; set; }

//        public IEnumerable<User> Owners { get; set; }
        public User Owner { get; set; }

        public Model Model { get; set; }

        public IEnumerable<Resource> Resources { get; set; }

        public Visibility Visibility { get; set; }

        public Assistant()
        {
        }


    }


    public class LookupField
    {

        public string LookupValue { get; set; }

        public int LookupId { get; set; }

    }
}