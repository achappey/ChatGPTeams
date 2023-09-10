using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace achappey.ChatGPTeams.Extensions
{
    public static class HttpExtensions
    {
        public static async Task<T> FromJson<T>(this HttpResponseMessage responseMessage)
        {
            if (responseMessage.IsSuccessStatusCode)
            {
                var content = await responseMessage.Content.ReadAsStringAsync();
                return content.FromJson<T>();
            }

            throw new Exception(responseMessage.ReasonPhrase);
        }

    }
}