using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelegramBots.Clients
{
    public class UrbanDictionaryClient : IDictionaryClient
    {
        private const string URBANDICTIONARY_API = "http://api.urbandictionary.com/v0/define?term={0}";
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<IEnumerable<string>> GetDefinitions(string input)
        {
            var jsonResult = await httpClient.GetStringAsync(string.Format(URBANDICTIONARY_API, input));

            var model = JsonConvert.DeserializeObject<UrbanDictionaryModel>(jsonResult);

            var result = model.list
                .OrderBy(_ => Guid.NewGuid())
                .FirstOrDefault();

            return new[] {
                result.definition,
                result.example
            };
        }

        #region UrbanDictionaryModel

        public class UrbanDictionaryModel
        {
            public List[] list { get; set; }
        }

        public class List
        {
            public string definition { get; set; }
            public string permalink { get; set; }
            public int thumbs_up { get; set; }
            public string[] sound_urls { get; set; }
            public string author { get; set; }
            public string word { get; set; }
            public int defid { get; set; }
            public string current_vote { get; set; }
            public DateTime written_on { get; set; }
            public string example { get; set; }
            public int thumbs_down { get; set; }
        }

        #endregion
    }
}