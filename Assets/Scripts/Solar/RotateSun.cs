using System;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Light))]
public class RotateSun : MonoBehaviour
{
    public enum TimeMode
    {
        Live,
        Historical
    }

    [Header("Time Mode")]
    [SerializeField] private TimeMode timeMode = TimeMode.Live;

    [Header("Historical Date & Time")]
    [SerializeField] private int historicalYear = 2026;

    [Range(1, 12)]
    [SerializeField] private int historicalMonth = 1;

    [Range(1, 31)]
    [SerializeField] private int historicalDay = 1;

    [Range(0, 23)]
    [SerializeField] private int historicalHour = 12;

    [Range(0, 59)]
    [SerializeField] private int historicalMinute = 0;

    [Header("Time Simulation")]
    [Tooltip("When enabled in Historical mode, time advances automatically.")]
    [SerializeField] private bool playHistoricalTime = false;

    [Tooltip("How many simulated seconds pass per real second.")]
    [Range(0.1f, 3600f)]
    [SerializeField] private float timeScaleMultiplier = 60f;

    [Header("Geographic Coordinates & Scene Orientation")]
    [Tooltip("Latitude of the solar plant.")]
    [Range(-90f, 90f)]
    [SerializeField] private float latitude = 39.93f;

    [Tooltip("Longitude of the solar plant.")]
    [Range(-180f, 180f)]
    [SerializeField] private float longitude = 32.86f;

    [Tooltip("Compass yaw angle in degrees.")]
    [Range(0f, 360f)]
    [SerializeField] private float northOffset = 0f;

    [Header("Location")]
    [SerializeField] private string locationName = "Ankara, Türkiye";

    [Header("Light Settings")]
    [SerializeField] private bool adjustIntensityForNight = true;

    [SerializeField] private float dayIntensity = 1.2f;

    [Header("Calculated Solar Position")]
    [SerializeField] private float currentElevation;
    [SerializeField] private float currentAzimuth;
    [SerializeField] private bool isDaytime;

    private Light sunLight;

    // ========================================================================
    // PUBLIC PROPERTIES
    // ========================================================================

    public float CurrentElevation => currentElevation;

    public float CurrentAzimuth => currentAzimuth;

    public bool IsDaytime => isDaytime;

    public float Latitude => latitude;

    public float Longitude => longitude;

    public string LocationName => locationName;

    public TimeMode CurrentTimeMode => timeMode;

    public bool IsLiveMode =>
        timeMode == TimeMode.Live;

    public bool IsHistoricalMode =>
        timeMode == TimeMode.Historical;

    public DateTime CurrentLocalDateTime
    {
        get
        {
            if (timeMode == TimeMode.Live)
            {
                return DateTime.Now;
            }

            return GetHistoricalDateTime();
        }
    }

    public int CurrentYear =>
        CurrentLocalDateTime.Year;

    public int CurrentMonth =>
        CurrentLocalDateTime.Month;

    public int CurrentDay =>
        CurrentLocalDateTime.Day;

    public int CurrentHour =>
        CurrentLocalDateTime.Hour;

    public int CurrentMinute =>
        CurrentLocalDateTime.Minute;

    public int CurrentDayOfYear =>
        CurrentLocalDateTime.DayOfYear;

    // ========================================================================
    // UNITY
    // ========================================================================

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
        if (timeMode == TimeMode.Live)
        {
            UpdateSunPosition();
            return;
        }

        if (playHistoricalTime &&
            Application.isPlaying)
        {
            AdvanceHistoricalTime();
        }

        UpdateSunPosition();
    }

    // ========================================================================
    // PUBLIC TIME CONTROLS
    // ========================================================================

    public void SetLiveMode()
    {
        timeMode = TimeMode.Live;

        UpdateSunPosition();
    }

    public void SetHistoricalMode()
    {
        timeMode = TimeMode.Historical;

        UpdateSunPosition();
    }

    public void SetHistoricalDate(
        int year,
        int month,
        int day)
    {
        try
        {
            DateTime testDate =
                new DateTime(
                    year,
                    month,
                    day,
                    historicalHour,
                    historicalMinute,
                    0
                );

            historicalYear =
                testDate.Year;

            historicalMonth =
                testDate.Month;

            historicalDay =
                testDate.Day;

            timeMode =
                TimeMode.Historical;

            UpdateSunPosition();
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning(
                "RotateSun: Invalid historical date."
            );
        }
    }

    public void SetHistoricalTime(
        int hour,
        int minute)
    {
        historicalHour =
            Mathf.Clamp(hour, 0, 23);

        historicalMinute =
            Mathf.Clamp(minute, 0, 59);

        timeMode =
            TimeMode.Historical;

        UpdateSunPosition();
    }

    public void SetHistoricalDateTime(
        int year,
        int month,
        int day,
        int hour,
        int minute)
    {
        try
        {
            DateTime selectedDate =
                new DateTime(
                    year,
                    month,
                    day,
                    hour,
                    minute,
                    0
                );

            historicalYear =
                selectedDate.Year;

            historicalMonth =
                selectedDate.Month;

            historicalDay =
                selectedDate.Day;

            historicalHour =
                selectedDate.Hour;

            historicalMinute =
                selectedDate.Minute;

            timeMode =
                TimeMode.Historical;

            UpdateSunPosition();
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogWarning(
                "RotateSun: Invalid historical date/time."
            );
        }
    }

    // ========================================================================
    // HISTORICAL TIME
    // ========================================================================

    private DateTime GetHistoricalDateTime()
    {
        try
        {
            return new DateTime(
                historicalYear,
                historicalMonth,
                historicalDay,
                historicalHour,
                historicalMinute,
                0
            );
        }
        catch (ArgumentOutOfRangeException)
        {
            return new DateTime(
                2026,
                1,
                1,
                12,
                0,
                0
            );
        }
    }

    private void AdvanceHistoricalTime()
    {
        DateTime current =
            GetHistoricalDateTime();

        current =
            current.AddSeconds(
                Time.deltaTime *
                timeScaleMultiplier
            );

        historicalYear =
            current.Year;

        historicalMonth =
            current.Month;

        historicalDay =
            current.Day;

        historicalHour =
            current.Hour;

        historicalMinute =
            current.Minute;
    }

    // ========================================================================
    // SOLAR POSITION
    // ========================================================================

    public void UpdateSunPosition()
    {
        DateTime localDateTime =
            CurrentLocalDateTime;

        CalculateSolarPositionForLocalTime(
            localDateTime,
            out currentElevation,
            out currentAzimuth
        );

        isDaytime =
            currentElevation > 0f;

        ApplySunTransform();
        ApplyLighting();
    }

    public void GetSolarPositionAt(
        DateTime localDateTime,
        out float elevation,
        out float azimuth)
    {
        CalculateSolarPositionForLocalTime(
            localDateTime,
            out elevation,
            out azimuth
        );
    }

    public Vector3 GetSunDirectionAt(
        DateTime localDateTime)
    {
        float elevation;
        float azimuth;

        GetSolarPositionAt(
            localDateTime,
            out elevation,
            out azimuth
        );

        float effectiveAzimuthRad =
            (azimuth + northOffset) *
            Mathf.Deg2Rad;

        float elevRad =
            elevation *
            Mathf.Deg2Rad;

        Vector3 sunDirection =
            new Vector3(
                Mathf.Cos(elevRad) *
                Mathf.Sin(effectiveAzimuthRad),

                Mathf.Sin(elevRad),

                Mathf.Cos(elevRad) *
                Mathf.Cos(effectiveAzimuthRad)
            );

        return sunDirection.normalized;
    }

    private void CalculateSolarPositionForLocalTime(
        DateTime localDateTime,
        out float elevation,
        out float azimuth)
    {
        float utcEquivalentHour =
            (float)localDateTime.TimeOfDay.TotalHours
            - (longitude / 15f);

        DateTime utcDate =
            localDateTime.Date;

        while (utcEquivalentHour < 0f)
        {
            utcEquivalentHour += 24f;
            utcDate = utcDate.AddDays(-1);
        }

        while (utcEquivalentHour >= 24f)
        {
            utcEquivalentHour -= 24f;
            utcDate = utcDate.AddDays(1);
        }

        int uHour = Mathf.FloorToInt(utcEquivalentHour);
        float minuteFloat = (utcEquivalentHour - uHour) * 60f;
        int uMinute = Mathf.FloorToInt(minuteFloat);
        float secondFloat = (minuteFloat - uMinute) * 60f;
        int uSecond = Mathf.FloorToInt(secondFloat);

        DateTime utcTime =
            new DateTime(
                utcDate.Year,
                utcDate.Month,
                utcDate.Day,
                uHour,
                uMinute,
                uSecond,
                DateTimeKind.Utc
            );

        CalculateSolarPosition(
            utcTime,
            latitude,
            longitude,
            out elevation,
            out azimuth
        );
    }

    // ========================================================================
    // SUN TRANSFORM
    // ========================================================================

    private void ApplySunTransform()
    {
        float effectiveAzimuthRad =
            (currentAzimuth + northOffset) *
            Mathf.Deg2Rad;

        float elevRad =
            currentElevation *
            Mathf.Deg2Rad;

        Vector3 sunDirectionInSky =
            new Vector3(
                Mathf.Cos(elevRad) *
                Mathf.Sin(effectiveAzimuthRad),

                Mathf.Sin(elevRad),

                Mathf.Cos(elevRad) *
                Mathf.Cos(effectiveAzimuthRad)
            );

        Vector3 lightForward = -sunDirectionInSky;

        if (lightForward != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    lightForward,
                    Vector3.up
                );
        }
    }

    // ========================================================================
    // LIGHTING
    // ========================================================================

    private void ApplyLighting()
    {
        if (!adjustIntensityForNight ||
            sunLight == null)
        {
            return;
        }

        if (currentElevation > 5f)
        {
            sunLight.intensity = dayIntensity;
        }
        else if (currentElevation > 0f)
        {
            sunLight.intensity =
                Mathf.Lerp(
                    0f,
                    dayIntensity,
                    currentElevation / 5f
                );
        }
        else
        {
            sunLight.intensity = 0f;
        }
    }

    // ========================================================================
    // NOAA SOLAR POSITION
    // ========================================================================

    private static void CalculateSolarPosition(
        DateTime utcTime,
        float lat,
        float lon,
        out float elevation,
        out float azimuth)
    {
        int dayOfYear = utcTime.DayOfYear;

        double utcHours =
            utcTime.Hour +
            utcTime.Minute / 60.0 +
            utcTime.Second / 3600.0 +
            utcTime.Millisecond / 3600000.0;

        double gamma =
            2.0 * Math.PI / 365.0 *
            (
                dayOfYear - 1 +
                (utcHours - 12.0) / 24.0
            );

        double eqtime =
            229.18 *
            (
                0.000075 +
                0.001868 * Math.Cos(gamma) -
                0.032077 * Math.Sin(gamma) -
                0.014615 * Math.Cos(2.0 * gamma) -
                0.040849 * Math.Sin(2.0 * gamma)
            );

        double decl =
            0.006918 -
            0.399912 * Math.Cos(gamma) +
            0.070257 * Math.Sin(gamma) -
            0.006758 * Math.Cos(2.0 * gamma) +
            0.000907 * Math.Sin(2.0 * gamma) -
            0.002697 * Math.Cos(3.0 * gamma) +
            0.00148 * Math.Sin(3.0 * gamma);

        double timeOffset = eqtime + 4.0 * lon;
        double tst = utcHours * 60.0 + timeOffset;
        double ha = (tst / 4.0) - 180.0;

        double haRad = ha * (Math.PI / 180.0);
        double latRad = lat * (Math.PI / 180.0);

        double cosZenith =
            Math.Sin(latRad) * Math.Sin(decl) +
            Math.Cos(latRad) * Math.Cos(decl) * Math.Cos(haRad);

        cosZenith = Math.Max(-1.0, Math.Min(1.0, cosZenith));

        double zenith = Math.Acos(cosZenith);

        elevation =
            (float)(90.0 - (zenith * (180.0 / Math.PI)));

        double azRad =
            Math.Atan2(
                Math.Sin(haRad),
                Math.Cos(haRad) * Math.Sin(latRad) -
                Math.Tan(decl) * Math.Cos(latRad)
            ) + Math.PI;

        azimuth = (float)((azRad * (180.0 / Math.PI)) % 360.0);

        if (azimuth < 0f)
        {
            azimuth += 360f;
        }
    }
}
