using System;
using System.Collections.Generic;
using System.Text;

namespace Huebert
{
    public enum SolarElevation
    {
        ASTRONOMICAL_DAWN = -18,
        ASTRONOMICAL_DUSK = -18,
        NAUTICAL_DAWN = -12,
        NAUTICAL_DUSK = -12,
        CIVIL_DAWN = -6,
        CIVIL_DUSK = -6,
        SUNRISE = 0,
        SUNSET = 0,
        GOLDEN_HOUR = 6,
        CITY_INDOOR_LIGHTS = 12,
    }

    public enum SunDirection
    {
        SunRise = 1,
        SunSet = -1
    }

    public class AstroTime
    {
        private const double DegToRad = Math.PI / 180;
        private const double RadToDeg = 180 / Math.PI;

        /// <summary>
        /// Converts a DateTime to a julian date.
        /// </summary>
        private static double CalcJD(DateTime dateTime)
        {
            var dt = new DateTimeOffset(dateTime);
            // Calc integer part (days)
            var jday = (1461 * (dt.Year + 4800 + (dt.Month - 14) / 12)) / 4 + (367 * (dt.Month - 2 - 12 * ((dt.Month - 14) / 12))) / 12 - (3 * ((dt.Year + 4900 + (dt.Month - 14) / 12) / 100)) / 4 + dt.Day - 32075;

            // Calc floating point part (fraction of a day)
            var jdatetime = jday + (dt.Hour - 12.0) / 24.0 + (dt.Minute / 1440.0) + (dt.Second / 86400.0) + (dt.Millisecond / 86400000.0);

            return jdatetime + (dt.Offset.TotalSeconds / 86400);
        }

        /// <summary>
        /// Convert Julian Day to centuries since J2000.0.
        /// </summary>
        /// <param name="julianDate"></param>
        /// <returns></returns>
        private static double JulianDateToJulianCentury(double julianDate)
        {
            return (julianDate - 2451545.0) / 36525.0;
        }

        private static double JulianCenturyToJulianDate(double jCentury)
        {
            return jCentury * 36525.0 + 2451545;
        }

        /// <summary>
        /// Calculates the suns geometric mean longitude in degrees.
        /// </summary>
        /// <param name="julianCenturies"></param>
        /// <returns>Geometric Mean Longitude of the sun in degrees.</returns>
        private static double GetSunGeomMeanLong(double julianCenturies)
        {
            var lon = 280.46646 + julianCenturies * (36000.76983 + 0.0003032 * julianCenturies);

            while (lon > 360.0)
                lon -= 360.0;
            while (lon < 0.0)
                lon += 360.0;

            return lon;
        }

        /// <summary>
        /// Calculates the mean obliquity of the ecliptic
        /// </summary>
        /// <param name="julianCenturies"></param>
        /// <returns></returns>
        private static double GetMeanObliquityOfEcliptic(double julianCenturies)
        {
            var seconds = 21.448 - julianCenturies * (46.8150 + julianCenturies * (0.00059 - julianCenturies * (0.001813)));
            return 23.0 + (26.0 + (seconds / 60.0)) / 60.0;
        }

        private static double GetObliquityCorrection(double julianCenturies)
        {
            var meanObliquity = GetMeanObliquityOfEcliptic(julianCenturies);
            var omega = 125.04 - 1934.136 * julianCenturies;
            return meanObliquity + 0.00256 * Math.Cos(omega * DegToRad);

        }

        /// <summary>
        /// Calculates the eccentricity of earth's orbit
        /// </summary>
        /// <param name="julianCenturies"></param>
        /// <returns>Unitless eccentricity</returns>
        private static double GetEccentricityEarthOrbit(double julianCenturies)
        {
            return 0.016708634 - julianCenturies * (0.000042037 + 0.0000001267 * julianCenturies);
        }

        /// <summary>
        /// Calculates the Geometric Mean Anomaly of the Sun
        /// </summary>
        /// <param name="julianCenturies"></param>
        /// <returns>The Geometric Mean Anomaly of the Sun in degrees</returns>
        private static double getGeomMeanAnomalySun(double julianCenturies)
        {
            return 357.52911 + julianCenturies * (35999.05029 - 0.0001537 * julianCenturies);
        }

        /// <summary>
        /// Calculates the difference between true solar time and mean solar time
        /// </summary>
        /// <param name="julianCenturies"></param>
        /// <returns>Equation of time in minutes</returns>
        private static double GetEquationOfTime(double julianCenturies)
        {
            var epsilon = GetObliquityCorrection(julianCenturies);
            var meanLong = GetSunGeomMeanLong(julianCenturies);
            var ecc = GetEccentricityEarthOrbit(julianCenturies);
            var meanAnomaly = getGeomMeanAnomalySun(julianCenturies);

            var y = Math.Tan(DegToRad * epsilon / 2.0);
            y *= y;

            var sin2l0 = Math.Sin(2.0 * DegToRad * meanLong);
            var sinm = Math.Sin(DegToRad * meanAnomaly);
            var cos2l0 = Math.Cos(2.0 * DegToRad * meanLong);
            var sin4l0 = Math.Sin(4.0 * DegToRad * meanLong);
            var sin2m = Math.Sin(2.0 * DegToRad * meanAnomaly);

            var eTime = y * sin2l0 - 2.0 * ecc * sinm + 4.0 * ecc * y * sinm * cos2l0 - 0.5 * y * y * sin4l0 - 1.25 * ecc * ecc * sin2m;
            return RadToDeg * eTime * 4.0;
        }

        private static double GetSunEqOfCenter(double julianCenturies)
        {
            var geoMean = getGeomMeanAnomalySun(julianCenturies);
            var geoMeanRad = DegToRad * geoMean;
            var sinMean = Math.Sin(geoMeanRad);
            var sin2Mean = Math.Sin(geoMeanRad + geoMeanRad);
            var sin3Mean = Math.Sin(geoMeanRad + geoMeanRad + geoMeanRad);

            return sinMean * (1.914602 - julianCenturies * (0.004817 + 0.000014 * julianCenturies)) + sin2Mean * (0.019993 - 0.000101 * julianCenturies) + sin3Mean * 0.000289;
        }

        private static double GetSunTrueLong(double julianCenturies)
        {
            return GetSunGeomMeanLong(julianCenturies) + GetSunEqOfCenter(julianCenturies);
        }

        private static double GetSunApparentLong(double julianCenturies)
        {
            var trueLong = GetSunTrueLong(julianCenturies);
            var omega = 125.04 - 1934.136 * julianCenturies;
            return trueLong - 0.00569 - 0.00478 * Math.Sin(DegToRad * omega);
        }

        private static double GetSunDeclination(double julianCenturies)
        {
            var e = GetObliquityCorrection(julianCenturies);
            var lambda = GetSunApparentLong(julianCenturies);
            var sint = Math.Sin(DegToRad * e) * Math.Sin(DegToRad * lambda);

            return RadToDeg * Math.Asin(sint);
        }

        private static double GetSolarNoonUTC(double julianCenturies, double lon)
        {
            var tNoon = JulianDateToJulianCentury(JulianCenturyToJulianDate(julianCenturies) + lon / 360.0);
            var eqTime = GetEquationOfTime(tNoon);
            var solarNoonUTC = 720 + (lon * 4) - eqTime;
            var newTime = JulianDateToJulianCentury(JulianCenturyToJulianDate(julianCenturies) - 0.5 + solarNoonUTC / 1440.0);
            eqTime = GetEquationOfTime(newTime);
            return 720 + (lon * 4) - eqTime;

        }

        private static double GetRefractiveCorrection(SolarElevation solarElevation)
        {
            return solarElevation == 0 ? 0.833 : -(int)solarElevation;
        }

        private static double GetHourAngleAtSolarElevation(double lat, double solarDecliantion, SolarElevation solarElevation)
        {
            var latRad = DegToRad * lat;
            var sdRad = DegToRad * solarDecliantion;

            var correction = GetRefractiveCorrection(solarElevation);

            return Math.Acos(Math.Cos(DegToRad * (90 + correction)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad));
        }

        private static double GetTimeWhenSunIsAtAngleFromHorizon(double julianDate, double lat, double lon, SolarElevation elevation, SunDirection direction)
        {
            lon = lon * -1;
            var jCentury = JulianDateToJulianCentury(julianDate);

            var noonMin = GetSolarNoonUTC(jCentury, lon);
            var noonTime = JulianDateToJulianCentury(julianDate + noonMin / 1440.0);

            var eqTime = GetEquationOfTime(noonTime);
            var solarDeclination = GetSunDeclination(noonTime);
            var hourAngle = (double)direction * GetHourAngleAtSolarElevation(lat, solarDeclination, elevation);

            var delta = lon - RadToDeg * hourAngle;
            var timeDiff = 4 * delta;
            var timeUTC = 720 + timeDiff - eqTime;

            var newTime = JulianDateToJulianCentury(JulianCenturyToJulianDate(jCentury) + timeUTC / 1440.0);
            eqTime = GetEquationOfTime(newTime);
            solarDeclination = GetSunDeclination(newTime);
            hourAngle = (double)direction * GetHourAngleAtSolarElevation(lat, solarDeclination, elevation);

            delta = lon - RadToDeg * hourAngle;
            timeDiff = 4 * delta;
            return 720 + timeDiff - eqTime;
        }

        public static DateTime GetDawnTime(DateTime dt, double lat, double lon, SolarElevation elevation)
        {
            var julianDate = CalcJD(dt);
            var sunRiseUTC = Math.Floor(GetTimeWhenSunIsAtAngleFromHorizon(julianDate, lat, lon, elevation, SunDirection.SunRise) * 60) * 1e9;
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc).AddTicks(Convert.ToInt64(sunRiseUTC / 100)).ToLocalTime();
        }

        public static DateTime GetSunriseTime(DateTime dt, double lat, double lon)
        {
            return GetDawnTime(dt, lat, lon, SolarElevation.SUNRISE);
        }

        public static DateTime GetDuskTime(DateTime dt, double lat, double lon, SolarElevation elevation)
        {
            var julianDate = CalcJD(dt);
            var sunsetUTC = Math.Floor(GetTimeWhenSunIsAtAngleFromHorizon(julianDate, lat, lon, elevation, SunDirection.SunSet) * 60) * 1e9;
            return new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc).AddTicks(Convert.ToInt64(sunsetUTC / 100)).ToLocalTime();
        }

        public static DateTime GetSunsetTime(DateTime dt, double lat, double lon)
        {
            return GetDuskTime(dt, lat, lon, SolarElevation.SUNSET);
        }
    }
}
