using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TelegramBots.Clients
{
    public class DatamuseClient : IDictionaryClient
    {
        private const string DATAMUSE_API = "https://api.datamuse.com/words?ml={0}&sp=b*&max=1&md=d";
        private static readonly HttpClient httpClient = new HttpClient();

        public async Task<IEnumerable<string>> GetDefinitions(string input)
        {

            var jsonResult = await httpClient.GetStringAsync(string.Format(DATAMUSE_API, input));

            var model = JsonConvert.DeserializeObject<DatamuseModel[]>(jsonResult);

            return model.FirstOrDefault()?.defs;
        }

        #region DatamuseModel

        public class DatamuseModel
        {
            public string word { get; set; }
            public int score { get; set; }
            public string[] tags { get; set; }
            public string[] defs { get; set; }
        }

        #endregion
    }
}