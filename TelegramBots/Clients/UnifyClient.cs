using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnifiApi;
using UnifiApi.Responses;

namespace TelegramBots.Clients
{
    public class UnifyClient : IUnifyClient
    {
        private ExtendedUnifiClient unifiClient;

        public async Task<IEnumerable<EventModel>> GetEvents(string controller, string username, string password, int max = 10, int maxHours = 4320)
        {
            if (unifiClient == null)
            {
                Console.WriteLine($"Logging into {controller}");
                unifiClient = new ExtendedUnifiClient($"https://{controller}/", null, true);
                await unifiClient.LoginAsync(username, password);
            }

            try
            {
                var result = await unifiClient.ListOnlineClientsAsync();

                var allKnownDevicesResponse = await unifiClient.ListDevicesAsync();
                var allKnownDevices = allKnownDevicesResponse.Data
                    .ToDictionary(k => $"{k.Mac}", d => d.Name ?? d.Hostname ?? d.Mac);

                var allKnownClientsResponse = await unifiClient.ListAllClientsAsync();
                var allKnownClients = allKnownClientsResponse.Data
                    .ToDictionary(k => $"{k.Mac}", d => d.Name ?? d.Hostname ?? d.Mac);

                var merged = allKnownDevices
                    .Concat(allKnownClients)
                    .GroupBy(i => i.Key)
                    .ToDictionary(
                        group => group.Key,
                        group => group.First().Value);

                Console.WriteLine($"GetEventsAsync");
                var events = await unifiClient.GetEventsAsync(0, max, maxHours);

                var selectedEvents = from d in events.Data
                                     let msg = Regex.Replace(d.Message,
                                         @"(User|AP)\[([0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2}:[0-9a-f]{2})\]",
                                         x => merged.ContainsKey(x.Groups[2].Value)
                                             ? "*" + merged[x.Groups[2].Value] + "*"
                                             : x.Value)
                                     select new EventModel()
                                     {
                                         Key = d.Key,
                                         Message = msg,
                                         Date = d.Time,
                                     };

                return selectedEvents;
            }
            catch (Exception ex)
            {
                Console.WriteLine("UnifyClient exception:" + ex.Message);

                if (unifiClient != null)
                {
                    unifiClient.Dispose();
                    unifiClient = null;
                }
            }

            return null;
        }

        public class ExtendedUnifiClient : Client
        {
            public ExtendedUnifiClient(string baseUrl, string site = null, bool ignoreSslCertificate = false)
                : base(baseUrl, site, ignoreSslCertificate)
            {
            }

            public async Task<BaseResponse<Event>> GetEventsAsync(int start = 0, int limit = 1000, int within = 4320)
            {
                var path = $"/api/s/{Site}/stat/event";

                var oJsonObject = new JObject();
                oJsonObject.Add("_start", start);
                oJsonObject.Add("_limit", limit);
                oJsonObject.Add("within", within);

                var response = await ExecuteJsonCommandAsync(path, oJsonObject);
                return JsonConvert.DeserializeObject<BaseResponse<Event>>(response.Result);
            }
        }

        public class Event
        {
            private static readonly DateTime BaseDateTime = new DateTime(1970, 1, 1);

            [JsonProperty(PropertyName = "key")]
            public string Key { get; set; }

            [JsonProperty(PropertyName = "msg")]
            public string Message { get; set; }

            [JsonProperty(PropertyName = "sw_displayName")]
            public string DisplayName { get; set; }

            [JsonProperty(PropertyName = "sw_name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "time")]
            public long TimeSeconds { get; set; }

            public DateTime Time => BaseDateTime.AddMilliseconds(TimeSeconds).ToLocalTime();
        }

    }

    public class EventModel
    {
        public string Message { get; set; }

        public DateTime Date { get; set; }

        public string Key { get; set; }
    }

    public interface IUnifyClient
    {
        Task<IEnumerable<EventModel>> GetEvents(string controller, string username, string password, int max = 10, int maxHours = 4320);
    }
}
