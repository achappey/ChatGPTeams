
#nullable enable

using System.Collections.Generic;

namespace achappey.ChatGPTeams.Models
{
    public class Vault
    {
        public string Id { get; set; } = null!;

        public string Title { get; set; } = null!;

        public IEnumerable<User> Readers { get; set; } = null!;

        public IEnumerable<User> Owners { get; set; } = null!;


    }
}
