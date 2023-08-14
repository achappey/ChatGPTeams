
namespace achappey.ChatGPTeams.Models
{
    public enum ContentType
    {
        HTML,
        DOWNLOAD
    }

    public class EmbeddingScore
    {
        public string Url { get; set; }
        public string Text { get; set; }
        public double Score { get; set; }
    }


}
