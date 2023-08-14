using System.IO;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace achappey.ChatGPTeams.Extensions
{
    public static class GraphDataExtensions
    {
        public static async Task<DriveItem> GetDriveItem(this GraphServiceClient client, string link)
        {
            string base64Value = System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(link));
            string encodedUrl = "u!" + base64Value.TrimEnd('=').Replace('/', '_').Replace('+', '-');

            return await client.Shares[encodedUrl].DriveItem
            .Request()
            .GetAsync();
        }

        public static async Task<byte[]> GetDriveItemContent(this GraphServiceClient client, string driveId, string itemId)
        {
            using var stream = await client.Drives[driveId].Items[itemId].Content
                .Request()
                .GetAsync();

            byte[] byteArray;
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                byteArray = memoryStream.ToArray();
            }

            return byteArray;
        }
    }
}