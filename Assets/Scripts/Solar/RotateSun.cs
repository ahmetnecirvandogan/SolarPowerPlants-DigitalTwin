using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class RotateSun : MonoBehaviour
{
    [Header("Time Mode")]
    [Tooltip("When enabled, synchronizes sun position directly with system clock (UTC).")]
    [SerializeField] private bool useSystemTime = true;

    [Tooltip("Speed multiplier for simulated time when useSystemTime is false.")]
    [Range(0.1f, 3600f)]
    [SerializeField] private float timeScaleMultiplier = 1f;

    [Tooltip("Simulated hour of the day (0 - 24) when useSystemTime is false.")]
    [Range(0f, 24f)]
    [SerializeField] private float simulatedHour = 12f;

    [Tooltip("Simulated day of year (1 - 365) when useSystemTime is false.")]
    [Range(1, 365)]
    [SerializeField] private int simulatedDayOfYear = 252; // Sept 9

    [Header("Geographic Coordinates & Scene Orientation")]
    [Tooltip("Latitude of the solar plant (-90 to 90, Ankara ~ 39.93).")]
    [Range(-90f, 90f)]
    [SerializeField] private float latitude = 39.93f;

    [Tooltip("Longitude of the solar plant (-180 to 180, Ankara ~ 32.86).")]
    [Range(-180f, 180f)]
    [SerializeField] private float longitude = 32.86f;

    [Tooltip("Compass yaw angle in degrees. 0 means +Z is North, 90 means +X is North, etc.")]
    [Range(0f, 360f)]
    [SerializeField] private float northOffset = 0f;

    [Header("Light Settings")]
    [Tooltip("Automatically dims or disables directional light intensity at night.")]
    [SerializeField] private bool adjustIntensityForNight = true;

    [SerializeField] private float dayIntensity = 1.2f;

    [Header("Calculated Solar Position (Read-Only)")]
    [SerializeField] private float currentElevation;
    [SerializeField] private float currentAzimuth;
    [SerializeField] private bool isDaytime;

    private Light sunLight;

    public float CurrentElevation => currentElevation;
    public float CurrentAzimuth => currentAzimuth;
    public bool IsDaytime => isDaytime;

    private void Awake()
    {
        sunLight = GetComponent<Light>();
    }

    private void Start()
    {
        UpdateSunPosition();
    }

    private void Update()
    {
        if (useSystemTime)
        {
            UpdateSunPosition();
        }
        else
        {
            if (Application.isPlaying)
            {
                simulatedHour += (Time.deltaTime * timeScaleMultiplier) / 3600f;
                if (simulatedHour >= 24f)
                {
                    simulatedHour -= 24f;
                    simulatedDayOfYear = (simulatedDayOfYear % 365) + 1;
                }
            }
            UpdateSunPosition();
        }
    }

    public void UpdateSunPosition()
    {
        DateTime utcNow;
        if (useSystemTime)
        {
            utcNow = DateTime.UtcNow;
            simulatedHour = (float)(DateTime.Now.TimeOfDay.TotalHours);
            simulatedDayOfYear = DateTime.Now.DayOfYear;
        }
        else
        {
            DateTime approxDate = new DateTime(DateTime.UtcNow.Year, 1, 1).AddDays(simulatedDayOfYear - 1);
            float utcEquivalentHour = simulatedHour - (longitude / 15f);
            if (utcEquivalentHour < 0f) utcEquivalentHour += 24f;
            if (utcEquivalentHour >= 24f) utcEquivalentHour -= 24f;

            int uHour = Mathf.FloorToInt(utcEquivalentHour);
            float uMinFloat = (utcEquivalentHour - uHour) * 60f;
            int uMin = Mathf.FloorToInt(uMinFloat);
            int uSec = Mathf.FloorToInt((uMinFloat - uMin) * 60f);

            utcNow = new DateTime(approxDate.Year, approxDate.Month, approxDate.Day, uHour, uMin, uSec, DateTimeKind.Utc);
        }

        CalculateSolarPosition(utcNow, latitude, longitude, out currentElevation, out currentAzimuth);
        isDaytime = currentElevation > 0f;

        ApplySunTransform();
        ApplyLighting();
    }

    private void ApplySunTransform()
    {
        // Azimuth is clockwise from North (0° = +Z, 90° = +X East, 180° = -Z South, 270° = -X West)
        float effectiveAzimuthRad = (currentAzimuth + northOffset) * Mathf.Deg2Rad;
        float elevRad = currentElevation * Mathf.Deg2Rad;

        // Position vector pointing FROM origin TOWARDS the sun in the sky
        Vector3 sunDirectionInSky = new Vector3(
            Mathf.Cos(elevRad) * Mathf.Sin(effectiveAzimuthRad),
            Mathf.Sin(elevRad),
            Mathf.Cos(elevRad) * Mathf.Cos(effectiveAzimuthRad)
        );

        // Directional light shines FROM the sun DOWN onto the scene
        Vector3 lightForward = -sunDirectionInSky;

        if (lightForward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lightForward, Vector3.up);
        }
    }

    private void ApplyLighting()
    {
        if (!adjustIntensityForNight || sunLight == null) return;

        if (currentElevation > 5f)
        {
            sunLight.intensity = dayIntensity;
        }
        else if (currentElevation > 0f)
        {
            // Smooth sunrise / sunset fade between 0° and 5° elevation
            sunLight.intensity = Mathf.Lerp(0f, dayIntensity, currentElevation / 5f);
        }
        else
        {
            sunLight.intensity = 0f;
        }
    }

    /// <summary>
    /// Computes accurate solar elevation and azimuth using the standard NOAA Solar Position Algorithm.
    /// </summary>
    private static void CalculateSolarPosition(DateTime utcTime, float lat, float lon, out float elevation, out float azimuth)
    {
        int dayOfYear = utcTime.DayOfYear;
        double utcHours = utcTime.Hour + utcTime.Minute / 60.0 + utcTime.Second / 3600.0 + utcTime.Millisecond / 3600000.0;

        // Fractional year in radians
        double gamma = 2.0 * Math.PI / 365.0 * (dayOfYear - 1 + (utcHours - 12.0) / 24.0);

        // Equation of time (in minutes)
        double eqtime = 229.18 * (0.000075 + 0.001868 * Math.Cos(gamma) - 0.032077 * Math.Sin(gamma)
                     - 0.014615 * Math.Cos(2.0 * gamma) - 0.040849 * Math.Sin(2.0 * gamma));

        // Solar declination (in radians)
        double decl = 0.006918 - 0.399912 * Math.Cos(gamma) + 0.070257 * Math.Sin(gamma)
                   - 0.006758 * Math.Cos(2.0 * gamma) + 0.000907 * Math.Sin(2.0 * gamma)
                   - 0.002697 * Math.Cos(3.0 * gamma) + 0.00148 * Math.Sin(3.0 * gamma);

        // True solar time in minutes
        double timeOffset = eqtime + 4.0 * lon;
        double tst = utcHours * 60.0 + timeOffset;

        // Solar hour angle in radians (-180° to 180°)
        double ha = (tst / 4.0) - 180.0;
        double haRad = ha * (Math.PI / 180.0);
        double latRad = lat * (Math.PI / 180.0);

        // Solar zenith angle
        double cosZenith = Math.Sin(latRad) * Math.Sin(decl) + Math.Cos(latRad) * Math.Cos(decl) * Math.Cos(haRad);
        cosZenith = Math.Max(-1.0, Math.Min(1.0, cosZenith));
        double zenith = Math.Acos(cosZenith);
        elevation = (float)(90.0 - (zenith * (180.0 / Math.PI)));

        // Solar azimuth angle (degrees clockwise from North: 0° = N, 90° = E, 180° = S, 270° = W)
        double azRad = Math.Atan2(Math.Sin(haRad), Math.Cos(haRad) * Math.Sin(latRad) - Math.Tan(decl) * Math.Cos(latRad)) + Math.PI;
        azimuth = (float)((azRad * (180.0 / Math.PI)) % 360.0);
        if (azimuth < 0f) azimuth += 360f;
    }
}
