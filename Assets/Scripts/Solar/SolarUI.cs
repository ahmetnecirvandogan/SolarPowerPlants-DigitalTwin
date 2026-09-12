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

    private float accumulatedEnergyWh = 0f;
    private float graphTimer = 0f;

    private readonly List<float> powerHistory =
        new List<float>();

    private float previousSimulatedHour = -1f;
    private int previousDayOfYear = -1;

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

        if (rotateSun != null)
        {
            previousSimulatedHour =
                rotateSun.CurrentSimulatedHour;

            previousDayOfYear =
                rotateSun.CurrentDayOfYear;
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

    private void UpdateUI()
    {
        if (locationText != null)
        {
            locationText.text =
                $"LOCATION\n" +
                $"{rotateSun.Latitude:F2}°, " +
                $"{rotateSun.Longitude:F2}°";
        }

        if (elevationText != null)
        {
            elevationText.text =
                $"SOLAR ELEVATION\n" +
                $"{rotateSun.CurrentElevation:F1}°";
        }

        if (azimuthText != null)
        {
            azimuthText.text =
                $"SOLAR AZIMUTH\n" +
                $"{rotateSun.CurrentAzimuth:F1}°";
        }

        if (statusText != null)
        {
            statusText.text =
                $"STATUS\n" +
                $"{(rotateSun.IsDaytime ? "DAYTIME" : "NIGHT")}";
        }

        if (irradianceText != null)
        {
            irradianceText.text =
                $"IRRADIANCE\n" +
                $"{raycastShading.CurrentIrradiance:F1} W/m²";
        }

        if (powerText != null)
        {
            float power =
                raycastShading.CurrentPower;

            if (power >= 1000f)
            {
                powerText.text =
                    $"CURRENT POWER\n" +
                    $"{power / 1000f:F2} kW";
            }
            else
            {
                powerText.text =
                    $"CURRENT POWER\n" +
                    $"{power:F1} W";
            }
        }

        if (efficiencyText != null)
        {
            efficiencyText.text =
                $"PANEL EFFICIENCY\n" +
                $"{raycastShading.panelEfficiency * 100f:F1}%";
        }

        if (shadowText != null)
        {
            shadowText.text =
                $"SHADOW\n" +
                $"{(raycastShading.IsShaded ? "SHADED" : "CLEAR")}";
        }

        if (dailyEnergyText != null)
        {
            if (accumulatedEnergyWh >= 1000f)
            {
                dailyEnergyText.text =
                    $"TODAY'S ENERGY\n" +
                    $"{accumulatedEnergyWh / 1000f:F2} kWh";
            }
            else
            {
                dailyEnergyText.text =
                    $"TODAY'S ENERGY\n" +
                    $"{accumulatedEnergyWh:F1} Wh";
            }
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

        float currentHour =
            rotateSun.CurrentSimulatedHour;

        int currentDay =
            rotateSun.CurrentDayOfYear;

        if (previousDayOfYear != -1 &&
            currentDay != previousDayOfYear)
        {
            accumulatedEnergyWh = 0f;
        }

        float deltaHours = 0f;

        if (previousSimulatedHour >= 0f)
        {
            deltaHours =
                currentHour -
                previousSimulatedHour;

            if (deltaHours < 0f)
            {
                deltaHours += 24f;
            }
        }

        if (deltaHours > 1f)
        {
            deltaHours = 0f;
        }

        float power =
            raycastShading.CurrentPower;

        accumulatedEnergyWh +=
            power * deltaHours;

        previousSimulatedHour =
            currentHour;

        previousDayOfYear =
            currentDay;
    }

    // ========================================================================
    // GRAPH
    // ========================================================================

    private void UpdateGraph()
    {
        graphTimer += Time.deltaTime;

        if (graphTimer < graphUpdateInterval)
        {
            return;
        }

        graphTimer = 0f;

        float currentPower =
            raycastShading.CurrentPower;

        powerHistory.Add(currentPower);

        if (powerHistory.Count > maxGraphPoints)
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

        for (int i = graphLineContainer.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                graphLineContainer.GetChild(i).gameObject
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

        foreach (float power in powerHistory)
        {
            if (power > maxPower)
            {
                maxPower = power;
            }
        }

        for (int i = 0;
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
                (powerHistory[i] / maxPower) *
                height;

            float y2 =
                (powerHistory[i + 1] / maxPower) *
                height;

            CreateGraphLine(
                new Vector2(x1, y1),
                new Vector2(x2, y2)
            );
        }
    }

    private void CreateGraphLine(
        Vector2 start,
        Vector2 end)
    {
        GameObject lineObject =
            new GameObject("GraphLine");

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
            ) * Mathf.Rad2Deg;

        rect.sizeDelta =
            new Vector2(
                length,
                3f
            );

        rect.anchoredPosition =
            start + direction * 0.5f;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );

        image.raycastTarget = false;
    }
}