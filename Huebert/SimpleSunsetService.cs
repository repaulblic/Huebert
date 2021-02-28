using Microsoft.Extensions.Logging;
using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Huebert
{
    //Add method to override/pause timer
    class SimpleSunsetService
    {
        private ILocalHueClient _client;
        private DateTime todaysGoldenSet, todaysSunset, todaysSunrise, todaysGoldenRise;
        private Configuration _config;
        Timer timeSetter;
        Timer lightSetter;
        ILogger _logger;

        public SimpleSunsetService(ILogger logger, ILocalHueClient hueClient, Configuration config)
        {
            DateTime today = DateTime.Now;
            today = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, today.Kind);
            _client = hueClient ?? throw new ArgumentNullException();
            todaysGoldenSet = AstroTime.GetDuskTime(today, (float)config.Location.Lat, (float)config.Location.Lon, SolarElevation.GOLDEN_HOUR);
            todaysSunset = AstroTime.GetDuskTime(today, (float)config.Location.Lat, (float)config.Location.Lon, SolarElevation.SUNSET);
            todaysSunrise = AstroTime.GetDawnTime(today, (float)config.Location.Lat, (float)config.Location.Lon, SolarElevation.SUNRISE);
            todaysGoldenRise = AstroTime.GetDawnTime(today, (float)config.Location.Lat, (float)config.Location.Lon, SolarElevation.GOLDEN_HOUR);
            _config = config;
            _logger = logger;

            _logger.LogInformation(string.Format("Set todays Sunrise to {0}", todaysSunrise));
            _logger.LogInformation(string.Format("Set todays GoldenRise to {0}", todaysGoldenRise));
            _logger.LogInformation(string.Format("Set todays GoldenSet to {0}", todaysGoldenSet));
            _logger.LogInformation(string.Format("Set todays Sunset to {0}", todaysSunset));

            timeSetter = new Timer(SetTimesForDay, null, 60 * 60 * 1000, 60 * 60 * 1000);

            lightSetter = new Timer(CheckForLightUpdate, null, 0, 1 * 60 * 1000);
        }

        private void SetTimesForDay(Object source)
        {
            DateTime today = DateTime.Now;
            today = new DateTime(today.Year, today.Month, today.Day, 0, 0, 0, today.Kind);

            //Day already set
            if (todaysGoldenRise != default(DateTime) && today.Day == todaysGoldenRise.Day)
            {
                return;
            }
            else
            {
                todaysGoldenRise = AstroTime.GetDawnTime(today, (float)_config.Location.Lat, (float)_config.Location.Lon, SolarElevation.GOLDEN_HOUR);
                todaysSunrise = AstroTime.GetDawnTime(today, (float)_config.Location.Lat, (float)_config.Location.Lon, SolarElevation.SUNRISE);
                todaysGoldenSet = AstroTime.GetDuskTime(today, (float)_config.Location.Lat, (float)_config.Location.Lon, SolarElevation.GOLDEN_HOUR);
                todaysSunset = AstroTime.GetDuskTime(today, (float)_config.Location.Lat, (float)_config.Location.Lon, SolarElevation.SUNSET);

                _logger.LogInformation(string.Format("Set todays Sunrise to {0}", todaysSunrise));
                _logger.LogInformation(string.Format("Set todays GoldenRise to {0}", todaysGoldenRise));
                _logger.LogInformation(string.Format("Set todays GoldenSet to {0}", todaysGoldenSet));
                _logger.LogInformation(string.Format("Set todays Sunset to {0}", todaysSunset));
            }
        }

        private async void CheckForLightUpdate(Object source)
        {
            DateTime now = DateTime.Now;
            var lights = await _client.GetLightsAsync();
            lights = lights.Where(i => _config.SimpleSchedule.DeviceIds.Contains(i.Id));
            int targetCT = 0;

            //Set lights to daylight CT
            if (todaysGoldenRise < now && now < todaysGoldenSet)
            {
                targetCT = (ushort)_config.SimpleSchedule.DayColorTemperature;
            }

            else if (todaysSunrise < now && now < todaysGoldenRise)
            {
                var ctDelta = (ushort)_config.SimpleSchedule.DayColorTemperature - (ushort)_config.SimpleSchedule.SunsetColorTemperature;
                var intervalDelta = todaysGoldenRise - todaysSunrise;
                var nowDelta = now - todaysSunrise;

                var progressInInterval = nowDelta / intervalDelta;

                var ctToAdd = Convert.ToInt32(ctDelta * progressInInterval);

                targetCT = (ushort)_config.SimpleSchedule.SunsetColorTemperature + ctToAdd;
            }

            else if (todaysGoldenSet < now && now < todaysSunset)
            {
                var ctDelta = (ushort)_config.SimpleSchedule.DayColorTemperature - (ushort)_config.SimpleSchedule.SunsetColorTemperature;
                var intervalDelta = todaysSunset - todaysGoldenSet;
                var nowDelta = now - todaysGoldenSet;

                var progressInInterval = nowDelta / intervalDelta;

                var ctToAdd = Convert.ToInt32(ctDelta * progressInInterval);

                targetCT = (ushort)_config.SimpleSchedule.DayColorTemperature + ctToAdd;
            }

            //Target was set and there exist lights that dont match target.
            if (targetCT > 0 && lights.Where(i => i.State.ColorTemperature != 1000000 / targetCT).Any())
            {
                _logger.LogInformation($"Updating light ids [{string.Join(',', _config.SimpleSchedule.DeviceIds)}] to CT {targetCT}K ({1000000 / targetCT})");
                //var cmd = new LightCommand() { ColorTemperature = 1000000 / (targetCT) };
                //await _client.SendCommandAsync(cmd, _config.SimpleSchedule.DeviceIds);
            }

        }

    }
}
