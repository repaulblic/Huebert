using System;
using System.Collections.Generic;
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
        public Schedule[] Schedules { get; set; }
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
        public long? Lat { get; set; }

        /// <summary>
        /// Longitude
        /// </summary>
        public long? Lon { get; set; }

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
