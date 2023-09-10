
#nullable enable


namespace achappey.ChatGPTeams.Models
{
    public class User
    {
        public string Id { get; set; } = null!;
        public Department Department { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Name { get; set; } = null!;
        //public string ContentType { get; set; } = null!;
        
        public string Mail { get; set; } = null!;
    }
}
