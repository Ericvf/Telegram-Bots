using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace TelegramBots.Clients
{
    public class GiphyClient : IGiphyClient
    {
        private readonly static System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();
        private readonly static Random random = new Random();

        public string ApiKey { get; }

        public GiphyClient(IGiphyConfig giphyConfig)
        {
            ApiKey = giphyConfig.Key;
        }

        public async Task<string> SearchGifyImage(string search)
        {
            var apiUrl = $"https://api.giphy.com/v1/gifs/search?api_key={ApiKey}&q={search}&offset=0&limit=10";

            var jsonResult = await httpClient.GetStringAsync(apiUrl);

            var giphyModel = JsonConvert.DeserializeObject<GiphyModel>(jsonResult);

            if (giphyModel.data.Length == 0)
            {
                return null;
            }

            var randomIndex = random.Next(0, 9) % giphyModel.data.Length;
            var randomImage = giphyModel.data[randomIndex].id;

            return $"https://media.giphy.com/media/{randomImage}/giphy.gif";
        }

        public Task<string> GetStringAsync(string requestUrl)
        {
            return httpClient.GetStringAsync(requestUrl);
        }

        #region GiphyModel

        public class GiphyModel
        {
            public Data[] data { get; set; }
        }


        public class Data
        {
            public string type { get; set; }
            public string id { get; set; }
            public string slug { get; set; }
            public string url { get; set; }
            public string bitly_gif_url { get; set; }
            public string bitly_url { get; set; }
            public string embed_url { get; set; }
            public string username { get; set; }
            public string source { get; set; }
            public string rating { get; set; }
            public string content_url { get; set; }
            public string source_tld { get; set; }
            public string source_post_url { get; set; }
            public int is_indexable { get; set; }
            public int is_sticker { get; set; }
            public string import_datetime { get; set; }
            public string trending_datetime { get; set; }
            public string title { get; set; }
        }

        #endregion
    }

    public interface IGiphyClient
    {
        Task<string> GetStringAsync(string requestUrl);

        Task<string> SearchGifyImage(string search);
    }
}
