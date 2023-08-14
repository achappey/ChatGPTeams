
#nullable enable


namespace achappey.ChatGPTeams.Models
{
    public class User
    {
        public string AadObjectId { get; set; } = null!;
        public Department Department { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        
        public string Mail { get; set; } = null!;
        public int Id { get; set; }
    }
}
