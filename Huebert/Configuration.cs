using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Huebert
{
    /// <summary>
    /// Base configuration model
    /// </summary>
    public class Configuration
    {
        /// <summary>
        /// Hue bridge info
        /// </summary>
        public BridgeInfo BridgeInfo { get; set; }

        /// <summary>
        /// Bridge location info
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Light schedules
        /// </summary>
        public List<Schedule> Schedules { get; set; }

        public static Configuration LoadConfiguration()
        {
            var config = new Configuration();
            if (File.Exists("HuebertConfig.json"))
            {
                try
                {
                    var jsonStr = File.ReadAllText("HuebertConfig.json");
                    config = Newtonsoft.Json.JsonConvert.DeserializeObject<Configuration>(jsonStr);
                }
                catch(Newtonsoft.Json.JsonSerializationException)
                {
                    //Malformed file, save bad file and create new one
                    //File.Move("HuebertConfig.json", $"HuebertConfig_{DateTime.Now}.json");
                }
            }
            config = config ?? new Configuration();
            config.BridgeInfo = config?.BridgeInfo ?? new BridgeInfo();
            config.Location = config?.Location ?? new Location();
            config.Schedules = config?.Schedules ?? new List<Schedule>();

            return config;
        }
    }

    public class BridgeInfo
    {
        /// <summary>
        /// Bridge IP address
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// Bridge generated key to ID Huebert.
        /// </summary>
        public string UserKey { get; set; }
    }

    public class Location
    {
        /// <summary>
        /// Latitude
        /// </summary>
        public float? Lat { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        public float? Lon { get; set; }

        /// <summary>
        /// Location name
        /// </summary>
        public string Name { get; set; }
    }

    public class Schedule
    {
        /// <summary>
        /// Schedule name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Device ids associated with this schedule
        /// </summary>
        public int[] DeviceIds { get; set; }

        /// <summary>
        /// Indicates if schedule should run when lights first appear. 
        /// </summary>
        public bool RunWhenLightsAppear { get; set; }

        /// <summary>
        /// Default color temperature
        /// </summary>
        public ushort DefaultColorTemperature { get; set; }

        /// <summary>
        /// Default brightness
        /// </summary>
        public byte DefaultBrightness { get; set; }

    }
}
