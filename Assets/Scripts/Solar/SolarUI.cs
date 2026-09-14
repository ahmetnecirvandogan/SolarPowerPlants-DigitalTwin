using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The RotateSun component controlling the sun.")]
    public RotateSun rotateSun;

    [Tooltip("The RaycastShading component attached to the solar panel.")]
    public RaycastShading raycastShading;

    [Header("Information Text")]
    public TMP_Text locationText;
    public TMP_Text elevationText;
    public TMP_Text azimuthText;
    public TMP_Text statusText;
    public TMP_Text irradianceText;
    public TMP_Text powerText;
    public TMP_Text efficiencyText;
    public TMP_Text shadowText;
    public TMP_Text dailyEnergyText;

    [Header("Production Graph")]
    public RectTransform graphArea;
    public RectTransform graphLineContainer;

    [Min(0.01f)]
    public float graphUpdateInterval = 1f;

    [Min(10)]
    public int maxGraphPoints = 100;

    [Header("Energy Calculation")]
    public bool calculateDailyEnergy = true;

    [Tooltip("Time interval used when estimating daily energy.")]
    [Min(1)]
    public int energyCalculationStepMinutes = 10;

    private float accumulatedEnergyWh = 0f;

    private float graphTimer = 0f;

    private readonly List<float> powerHistory =
        new List<float>();

    private DateTime lastCalculatedDateTime =
        DateTime.MinValue;

    // ========================================================================
    // UNITY
    // ========================================================================

    private void Start()
    {
        if (rotateSun == null)
        {
            Debug.LogWarning(
                "SolarUI: RotateSun reference is missing."
            );
        }

        if (raycastShading == null)
        {
            Debug.LogWarning(
                "SolarUI: RaycastShading reference is missing."
            );
        }

        if (rotateSun != null &&
            raycastShading != null)
        {
            RecalculateDailyEnergy();
        }

        UpdateUI();
    }

    private void Update()
    {
        if (rotateSun == null ||
            raycastShading == null)
        {
            return;
        }

        UpdateDailyEnergy();

        UpdateGraph();

        UpdateUI();
    }

    // ========================================================================
    // UI
    // ========================================================================

    private const string ColPos =
        "<pos=150>";

    private void UpdateUI()
    {
        if (locationText != null)
        {
            locationText.text =
                $"LOCATION\t\t" +
                $"{rotateSun.Latitude:F2}°, " +
                $"{rotateSun.Longitude:F2}°";
        }

        if (elevationText != null)
        {
            elevationText.text =
                $"SOLAR ELEVATION{ColPos}" +
                $"{rotateSun.CurrentElevation:F1}°";
        }

        if (azimuthText != null)
        {
            azimuthText.text =
                $"SOLAR AZIMUTH{ColPos}" +
                $"{rotateSun.CurrentAzimuth:F1}°";
        }

        if (statusText != null)
        {
            statusText.text =
                $"STATUS{ColPos}" +
                $"{(rotateSun.IsDaytime ? "DAY" : "NIGHT")}";
        }

        if (irradianceText != null)
        {
            irradianceText.text =
                $"IRRADIANCE{ColPos}" +
                $"{raycastShading.CurrentIrradiance:F1} W/m²";
        }

        if (powerText != null)
        {
            float power =
                raycastShading.CurrentPower;

            string powerStr =
                power >= 1000f
                    ? $"{power / 1000f:F2} kW"
                    : $"{power:F1} W";

            powerText.text =
                $"CURRENT POWER{ColPos}" +
                $"{powerStr}";
        }

        if (efficiencyText != null)
        {
            efficiencyText.text =
                $"PANEL EFFICIENCY{ColPos}" +
                $"{raycastShading.panelEfficiency * 100f:F1}%";
        }

        if (shadowText != null)
        {
            shadowText.text =
                $"SHADOW{ColPos}" +
                $"{(raycastShading.IsShaded ? "SHADED" : "CLEAR")}";
        }

        if (dailyEnergyText != null)
        {
            string energyStr =
                accumulatedEnergyWh >= 1000f
                    ? $"{accumulatedEnergyWh / 1000f:F2} kWh"
                    : $"{accumulatedEnergyWh:F1} Wh";

            dailyEnergyText.text =
                $"TODAY'S ENERGY{ColPos}" +
                $"{energyStr}";
        }
    }

    // ========================================================================
    // DAILY ENERGY
    // ========================================================================

    private void UpdateDailyEnergy()
    {
        if (!calculateDailyEnergy)
        {
            return;
        }

        DateTime currentDateTime =
            rotateSun.CurrentLocalDateTime;

        // Recalculate only when the selected minute changes.
        if (currentDateTime ==
            lastCalculatedDateTime)
        {
            return;
        }

        RecalculateDailyEnergy();
    }

    private void RecalculateDailyEnergy()
    {
        if (!calculateDailyEnergy ||
            rotateSun == null ||
            raycastShading == null)
        {
            return;
        }

        DateTime selectedDateTime =
            rotateSun.CurrentLocalDateTime;

        // ------------------------------------------------------------
        // Start exactly at midnight.
        // ------------------------------------------------------------

        DateTime midnight =
            selectedDateTime.Date;

        // If it is exactly midnight, energy is exactly zero.
        if (selectedDateTime <= midnight)
        {
            accumulatedEnergyWh = 0f;

            lastCalculatedDateTime =
                selectedDateTime;

            return;
        }

        // ------------------------------------------------------------
        // Integrate solar power from midnight to selected time.
        // ------------------------------------------------------------

        double totalEnergyWh = 0.0;

        DateTime sampleTime =
            midnight;

        DateTime previousSampleTime =
            sampleTime;

        float previousPower =
            CalculateEstimatedPower(
                previousSampleTime
            );

        while (sampleTime <
               selectedDateTime)
        {
            DateTime nextSampleTime =
                sampleTime.AddMinutes(
                    energyCalculationStepMinutes
                );

            if (nextSampleTime >
                selectedDateTime)
            {
                nextSampleTime =
                    selectedDateTime;
            }

            float nextPower =
                CalculateEstimatedPower(
                    nextSampleTime
                );

            double intervalHours =
                (
                    nextSampleTime -
                    previousSampleTime
                ).TotalHours;

            // Trapezoidal integration.
            double averagePower =
                (
                    previousPower +
                    nextPower
                ) / 2.0;

            totalEnergyWh +=
                averagePower *
                intervalHours;

            previousSampleTime =
                nextSampleTime;

            previousPower =
                nextPower;

            sampleTime =
                nextSampleTime;
        }

        accumulatedEnergyWh =
            (float)Math.Max(
                0.0,
                totalEnergyWh
            );

        lastCalculatedDateTime =
            selectedDateTime;
    }

    // ========================================================================
    // ESTIMATE POWER AT AN ARBITRARY TIME
    // ========================================================================

    private float CalculateEstimatedPower(
        DateTime localDateTime)
    {
        Vector3 sunDirection =
            rotateSun.GetSunDirectionAt(
                localDateTime
            );

        float elevation;
        float azimuth;

        rotateSun.GetSolarPositionAt(
            localDateTime,
            out elevation,
            out azimuth
        );

        // Sun is below the horizon.
        if (elevation <= 0f)
        {
            return 0f;
        }

        // ------------------------------------------------------------
        // Check whether another object blocks the panel.
        // ------------------------------------------------------------

        Vector3 rayOrigin =
            raycastShading.transform.position +
            raycastShading.transform.up *
            raycastShading.surfaceOffset;

        bool isShaded =
            Physics.Raycast(
                rayOrigin,
                sunDirection,
                raycastShading.raycastDistance
            );

        if (isShaded)
        {
            return 0f;
        }

        // ------------------------------------------------------------
        // Calculate incidence angle.
        // ------------------------------------------------------------

        float dotProduct =
            Vector3.Dot(
                raycastShading.transform.up,
                sunDirection
            );

        float intensity =
            Mathf.Max(
                0f,
                dotProduct
            );

        if (intensity <= 0f)
        {
            return 0f;
        }

        // ------------------------------------------------------------
        // Calculate irradiance.
        // ------------------------------------------------------------

        float irradiance =
            raycastShading.solarIrradiance *
            intensity;

        // ------------------------------------------------------------
        // Calculate electrical power.
        // ------------------------------------------------------------

        float power =
            irradiance *
            raycastShading.panelArea *
            raycastShading.panelEfficiency;

        return Mathf.Max(
            0f,
            power
        );
    }

    // ========================================================================
    // GRAPH
    // ========================================================================

    private void UpdateGraph()
    {
        graphTimer +=
            Time.deltaTime;

        if (graphTimer <
            graphUpdateInterval)
        {
            return;
        }

        graphTimer = 0f;

        float currentPower =
            raycastShading.CurrentPower;

        powerHistory.Add(
            currentPower
        );

        if (powerHistory.Count >
            maxGraphPoints)
        {
            powerHistory.RemoveAt(0);
        }

        DrawGraph();
    }

    private void DrawGraph()
    {
        if (graphArea == null ||
            graphLineContainer == null)
        {
            return;
        }

        for (
            int i =
                graphLineContainer.childCount - 1;
            i >= 0;
            i--)
        {
            Destroy(
                graphLineContainer
                    .GetChild(i)
                    .gameObject
            );
        }

        if (powerHistory.Count < 2)
        {
            return;
        }

        float width =
            graphArea.rect.width;

        float height =
            graphArea.rect.height;

        float maxPower = 1f;

        foreach (
            float power
            in powerHistory)
        {
            if (power > maxPower)
            {
                maxPower = power;
            }
        }

        for (
            int i = 0;
            i < powerHistory.Count - 1;
            i++)
        {
            float x1 =
                (float)i /
                (powerHistory.Count - 1) *
                width;

            float x2 =
                (float)(i + 1) /
                (powerHistory.Count - 1) *
                width;

            float y1 =
                (powerHistory[i] /
                maxPower) *
                height;

            float y2 =
                (powerHistory[i + 1] /
                maxPower) *
                height;

            CreateGraphLine(
                new Vector2(
                    x1,
                    y1
                ),
                new Vector2(
                    x2,
                    y2
                )
            );
        }
    }

    private void CreateGraphLine(
        Vector2 start,
        Vector2 end)
    {
        GameObject lineObject =
            new GameObject(
                "GraphLine"
            );

        lineObject.transform.SetParent(
            graphLineContainer,
            false
        );

        Image image =
            lineObject.AddComponent<Image>();

        RectTransform rect =
            lineObject.GetComponent<RectTransform>();

        Vector2 direction =
            end - start;

        float length =
            direction.magnitude;

        float angle =
            Mathf.Atan2(
                direction.y,
                direction.x
            ) *
            Mathf.Rad2Deg;

        rect.sizeDelta =
            new Vector2(
                length,
                3f
            );

        rect.anchoredPosition =
            start +
            direction * 0.5f;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        image.raycastTarget =
            false;
    }
}