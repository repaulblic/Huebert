using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using Q42.HueApi.ColorConverters.Original;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Huebert
{
    public class Hubert : BackgroundService
    {
        private readonly ILogger<Hubert> _logger;
        private ILocalHueClient _hueClient;
        private SimpleSunsetService _sunsetService;
        public Hubert(ILogger<Hubert> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //This interface is not supported by the streaming client
            _hueClient = null;
            var config = Configuration.LoadConfiguration();
            IRestClient restClient = new RestSharp.RestClient();

            //Check for Lat/Lon
            if (config.Location?.Lat == null || config?.Location?.Lon == null)
            {
                config.Location = GetLocation();
            }

            //Get hue client
            config.BridgeInfo.IPAddress = string.IsNullOrWhiteSpace(config?.BridgeInfo?.IPAddress) ? await GetBridgeIP() : config.BridgeInfo.IPAddress;

            _hueClient = new LocalHueClient(config.BridgeInfo.IPAddress);

            while (string.IsNullOrWhiteSpace(config.BridgeInfo.UserKey))
            {
                try
                {
                    //Registering with bridge
                    _logger.LogInformation("Press bridge button!");

                    config.BridgeInfo.UserKey = await _hueClient.RegisterAsync("HueSharp", $"HueBridge");
                }
                catch (LinkButtonNotPressedException)
                {
                    Thread.Sleep(3000);
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            SaveConfig(config);
            _hueClient.Initialize(config.BridgeInfo.UserKey);
            //TODO: Load schedules
            //TODO: Verify schedules
            //TODO: Generate default schedules when none exist

            //Generate simple schedule
            if (config.SimpleSchedule == null || !config.SimpleSchedule.IsValid())
            {
                var lights = await _hueClient.GetLightsAsync();
                config.SimpleSchedule.DeviceIds = lights.Select(i => i.Id).ToArray();
                config.SimpleSchedule.Name = "Default simple schedule";
                config.SimpleSchedule.DayColorTemperature = 5000;
                config.SimpleSchedule.SunsetColorTemperature = 2700;
                config.SimpleSchedule.Brightness = 100;
            }

            SaveConfig(config);

            _sunsetService = new SimpleSunsetService(_logger, _hueClient, config);


            while (!stoppingToken.IsCancellationRequested)
            {
                //await WriteLightsToLogger();
                //await Task.Delay(10000, stoppingToken);

            }
        }

        private async Task WriteLightsToLogger()
        {
            var lights = await _hueClient.GetLightsAsync();

            StringBuilder sb = new StringBuilder();
            var header = string.Format("{0,-16}|{7, 3}|{1,16}|{2,8}|{3,12}|{4,12}|{5,16}|{6,10}", "Name", "Temperature", "Hue", "Saturation", "Brightness", "Color Coords", "Hex Color", "ID");

            sb.AppendLine(header);
            sb.AppendLine(String.Concat(Enumerable.Repeat("-", header.Length)));
            foreach (var light in lights)
            {
                var rgbColor = light.State.ToRGBColor();
                string rgbString = $"[{rgbColor.R},{rgbColor.G},{rgbColor.B}]";

                sb.AppendLine(string.Format("{0,-16}|{7, 3}|{1,16}|{2,8}|{3,12}|{4,12}|{5,16}|{6,10}",
                        light.Name,
                        light.State.ColorTemperature,
                        light.State.Hue,
                        light.State.Saturation,
                        light.State.Brightness,
                        Newtonsoft.Json.JsonConvert.SerializeObject(light.State.ColorCoordinates),
                        light.State.ToHex(),
                        light.Id));
            }

            _logger.LogInformation(sb.ToString());
        }

        private void SaveConfig(Configuration config)
        {
            File.WriteAllText("HuebertConfig.json", Newtonsoft.Json.JsonConvert.SerializeObject(config, Newtonsoft.Json.Formatting.Indented));
        }

        private async Task<string> GetBridgeIP()
        {
            _logger.LogInformation("Searching for bridges...");

            var bridges = await HueBridgeDiscovery.FastDiscoveryWithNetworkScanFallbackAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));

            while (bridges.Count == 0)
            {
                _logger.LogError("Could not find a bridge. Retrying...");
                Thread.Sleep(3000);
                bridges = await HueBridgeDiscovery.FastDiscoveryWithNetworkScanFallbackAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
            }

            _logger.LogInformation($"Found {bridges.Count} bridges. IPs: {string.Join(',', bridges.Select(i => i.IpAddress).ToArray())}");

            var selectedBridge = bridges.First();

            _logger.LogInformation($"Using bridge {selectedBridge.IpAddress}");

            return selectedBridge.IpAddress;
        }

        private Location GetLocation()
        {
            Location loc = new Location() { Lat = 0, Lon = 0, Name = "Null Island" };
            IRestClient restClient = new RestSharp.RestClient();
            RestRequest req = new RestRequest("https://ipinfo.io/json");
            var resp = restClient.Get(req);

            if (resp.IsSuccessful)
            {
                var jtoken = JToken.Parse(resp.Content);
                string locString = jtoken.SelectToken("loc")?.Value<string>();
                string cityString = jtoken.SelectToken("city")?.Value<string>();

                if (!string.IsNullOrWhiteSpace(locString))
                {
                    var locationParts = locString.Split(',');

                    if (locationParts.Length == 2)
                    {
                        loc.Lat = float.Parse(locationParts[0]);
                        loc.Lon = float.Parse(locationParts[1]);
                    }
                }
            }
            else
            {
                _logger.LogWarning("Failed to get location from IP");
            }

            return loc;
        }
    }
}
