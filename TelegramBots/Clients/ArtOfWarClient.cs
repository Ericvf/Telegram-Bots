using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TelegramBots.Clients
{
    public class ArtOfWarClient : IArtOfWarClient
    {

        private readonly string[] _artOfWarQuotes;

        public ArtOfWarClient()
        {
            var json = File.ReadAllText("artofwarquotes.json");

            _artOfWarQuotes = JsonConvert.DeserializeObject<string[]>(json);
        }

        public Task<string> GetRandomQuote()
        {
            if (_artOfWarQuotes == null || !_artOfWarQuotes.Any())
            {
                return Task.FromResult("No Art of War Quotes loaded");
            }

            return Task.FromResult(_artOfWarQuotes[new Random().Next(0, _artOfWarQuotes.Length)]);
        }
    }

    public interface IArtOfWarClient
    {
        Task<string> GetRandomQuote();
    }
}
