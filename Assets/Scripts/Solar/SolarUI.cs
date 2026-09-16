using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SolarUI : MonoBehaviour
{
    [Header("References")]
    public RotateSun rotateSun;

    [Tooltip("All solar panels in the farm. Automatically populated if left empty.")]
    public RaycastShading[] solarPanels;

    [Header("Time Panel")]
    public GameObject timePanel;
    public TMP_Text dateTimeText;
    public TMP_Text timeModeText;

    [Header("Plant Information Text")]
    public TMP_Text locationText;
    public TMP_Text elevationText;
    public TMP_Text azimuthText;
    public TMP_Text statusText;
    public TMP_Text irradianceText;
    public TMP_Text powerText;
    public TMP_Text efficiencyText;
    public TMP_Text shadowText;
    public TMP_Text dailyEnergyText;
    public TMP_Text panelCountText;

    [Header("Production Graph")]
    public RectTransform graphArea;
    public RectTransform graphLineContainer;
    [Min(0.01f)] public float graphUpdateInterval = 1f;
    [Min(10)] public int maxGraphPoints = 100;

    [Header("Energy Calculation")]
    public bool calculateDailyEnergy = true;
    [Min(1)] public int energyCalculationStepMinutes = 15;

    // Plant Aggregates
    public float TotalCurrentPower { get; private set; }
    public float AverageIrradiance { get; private set; }
    public int ShadedPanelCount { get; private set; }
    public int TotalPanelCount => solarPanels != null ? solarPanels.Length : 0;

    private float accumulatedEnergyWh = 0f;
    private float graphTimer = 0f;
    private readonly List<float> powerHistory = new List<float>();
    private DateTime lastCalculatedDateTime = DateTime.MinValue;

    [Header("Time Dropdown Drawer")]
    [Tooltip("The clickable button on the Time Panel to toggle the settings drawer.")]
    public Button timePanelButton;

    [Tooltip("The dropdown container panel that expands/collapses.")]
    public GameObject timeSettingsDrawer;

    [Tooltip("Mode selector: 0 = Live, 1 = Historical")]
    public TMP_Dropdown timeModeDropdown;

    [Header("Historical Date & Time Controls")]
    [Tooltip("Parent object containing historical sliders/inputs (hidden in Live mode)")]
    public GameObject historicalControlsContainer;

    [Tooltip("Interactive slider from 0 to 1439 minutes (00:00 to 23:59)")]
    public Slider timeOfDaySlider;
    public TMP_Text timeOfDaySliderText;

    [Tooltip("Year input field or slider")]
    public TMP_InputField yearInputField;

    [Tooltip("Month selector (Jan - Dec)")]
    public TMP_Dropdown monthDropdown;

    [Tooltip("Day slider (1 - 31)")]
    public Slider daySlider;
    public TMP_Text daySliderText;

    [Header("Time Simulation Controls")]
    public Button playPauseButton;
    public TMP_Text playPauseButtonText;
    public Slider timeSpeedSlider;
    public TMP_Text timeSpeedText;

    private const string ColPos = "<pos=130>";

    private void Awake()
    {
        AutoRecoverReferences();
    }

    private void Start()
    {
        AutoRecoverReferences();
        SetupTimeControls();
        RecalculateDailyEnergy();
        UpdateUI();
    }


    private void Update()
    {
        if (rotateSun == null || solarPanels == null || solarPanels.Length == 0)
        {
            AutoRecoverReferences();
        }

        // Keep sliders updated if simulation is playing or in live mode
        if (rotateSun != null && (rotateSun.IsLiveMode || rotateSun.PlayHistoricalTime))
        {
            SyncControlsWithCurrentTime();
        }

        AggregatePlantData();
        UpdateDailyEnergy();
        UpdateGraph();
        UpdateUI();
        Debug.Log($"[SolarUI] Connected Drawer: {(timeSettingsDrawer != null ? timeSettingsDrawer.name : "STILL MISSING / NULL")}");
    }


    private void AutoRecoverReferences()
    {
        if (rotateSun == null)
        {
            rotateSun = FindObjectOfType<RotateSun>();
        }

        if (solarPanels == null || solarPanels.Length == 0)
        {
            solarPanels = FindObjectsOfType<RaycastShading>();
        }
        // 1. Auto-find Time Drawer Panel
        if (timeSettingsDrawer == null)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            foreach (Transform t in allTransforms)
            {
                string n = t.name.ToLower();
                if (n.Contains("timesetting") || n.Contains("settingsdrawer") || n.Contains("timedrawer") || n.Contains("time_settings") || n.Contains("settingspanel"))
                {
                    timeSettingsDrawer = t.gameObject;
                    break;
                }
            }
        }

        // 2. Auto-find & Setup Button on TimePanel
        if (timePanelButton == null && timePanel != null)
        {
            timePanelButton = timePanel.GetComponent<Button>() ?? timePanel.AddComponent<Button>();
        }

        if (timePanelButton != null)
        {
            timePanelButton.onClick.RemoveAllListeners();
            timePanelButton.onClick.AddListener(ToggleTimeSettingsDrawer);
        }

        // 3. Close the drawer initially
        if (timeSettingsDrawer != null)
        {
            timeSettingsDrawer.SetActive(false);
        }

        // 4. Auto-find dropdowns & sliders
        if (timeModeDropdown == null)
            timeModeDropdown = GetComponentInChildren<TMP_Dropdown>(true);

        if (timeOfDaySlider == null)
        {
            Slider[] sliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in sliders)
            {
                if (s.name.ToLower().Contains("time")) timeOfDaySlider = s;
                else if (s.name.ToLower().Contains("day")) daySlider = s;
            }
        }



        TMP_Text[] allTexts = FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text t in allTexts)
        {
            string n = t.name.ToLower();
            if (dateTimeText == null && (n.Contains("datetime") || n.Contains("time_text") || n.Contains("clock")))
                dateTimeText = t;
            else if (locationText == null && n.Contains("location"))
                locationText = t;
            else if (elevationText == null && n.Contains("elevation"))
                elevationText = t;
            else if (azimuthText == null && n.Contains("azimuth"))
                azimuthText = t;
            else if (statusText == null && n.Contains("status"))
                statusText = t;
            else if (irradianceText == null && n.Contains("irradiance"))
                irradianceText = t;
            else if (powerText == null && n.Contains("power"))
                powerText = t;
            else if (efficiencyText == null && n.Contains("efficiency"))
                efficiencyText = t;
            else if (shadowText == null && n.Contains("shadow"))
                shadowText = t;
            else if (dailyEnergyText == null && (n.Contains("energy") || n.Contains("daily")))
                dailyEnergyText = t;
            else if (panelCountText == null && n.Contains("panel_count"))
                panelCountText = t;
        }
    }

    private void AggregatePlantData()
    {
        if (solarPanels == null || solarPanels.Length == 0) return;

        float totalPower = 0f;
        float totalIrradiance = 0f;
        int shadedCount = 0;

        for (int i = 0; i < solarPanels.Length; i++)
        {
            RaycastShading panel = solarPanels[i];
            if (panel == null) continue;

            totalPower += panel.CurrentPower;
            totalIrradiance += panel.CurrentIrradiance;
            if (panel.IsShaded) shadedCount++;
        }

        TotalCurrentPower = totalPower;
        AverageIrradiance = totalIrradiance / solarPanels.Length;
        ShadedPanelCount = shadedCount;
    }

    private void UpdateUI()
    {
        // Date & Time
        if (dateTimeText != null)
        {
            DateTime dt = (rotateSun != null && rotateSun.IsHistoricalMode)
                ? rotateSun.CurrentLocalDateTime
                : DateTime.Now;

            dateTimeText.text = $"Date: {dt:dd-MM-yyyy}\nTime: {dt:HH:mm}";
        }

        if (timeModeText != null && rotateSun != null)
        {
            timeModeText.text = rotateSun.IsLiveMode ? "LIVE" : "HISTORICAL";
        }

        // Sun & Location
        if (rotateSun != null)
        {
            if (locationText != null)
                locationText.text = $"LOCATION\t\t{rotateSun.Latitude:F2}°, {rotateSun.Longitude:F2}°";

            if (elevationText != null)
                elevationText.text = $"SOLAR ELEVATION{ColPos}{rotateSun.CurrentElevation:F1}°";

            if (azimuthText != null)
                azimuthText.text = $"SOLAR AZIMUTH{ColPos}{rotateSun.CurrentAzimuth:F1}°";

            if (statusText != null)
                statusText.text = $"STATUS{ColPos}{(rotateSun.IsDaytime ? "DAY" : "NIGHT")}";
        }

        // Farm Production Metrics
        if (irradianceText != null)
        {
            irradianceText.text = $"AVG IRRADIANCE{ColPos}{AverageIrradiance:F1} W/m²";
        }

        if (powerText != null)
        {
            powerText.text = $"TOTAL POWER{ColPos}{FormatPower(TotalCurrentPower)}";
        }

        if (efficiencyText != null && solarPanels != null && solarPanels.Length > 0)
        {
            efficiencyText.text = $"AVG EFFICIENCY{ColPos}{solarPanels[0].panelEfficiency * 100f:F1}%";
        }

        // Multi-panel Shadow status
        if (shadowText != null)
        {
            int total = TotalPanelCount;
            if (total > 0)
            {
                if (ShadedPanelCount == 0)
                    shadowText.text = $"SHADOW{ColPos}ALL CLEAR (0/{total})";
                else if (ShadedPanelCount == total)
                    shadowText.text = $"SHADOW{ColPos}FULLY SHADED ({total}/{total})";
                else
                {
                    float pct = ((float)ShadedPanelCount / total) * 100f;
                    shadowText.text = $"SHADOW{ColPos}{ShadedPanelCount}/{total} ({pct:F1}%)";
                }
            }
        }


        if (panelCountText != null)
        {
            panelCountText.text = $"PANELS{ColPos}{TotalPanelCount}";
        }

        if (dailyEnergyText != null)
        {
            dailyEnergyText.text = $"TODAY'S ENERGY{ColPos}{FormatEnergy(accumulatedEnergyWh)}";
        }
    }

    private string FormatPower(float watts)
    {
        if (watts >= 1_000_000f)
            return $"{watts / 1_000_000f:F2} MW";
        if (watts >= 1000f)
            return $"{watts / 1000f:F2} kW";
        return $"{watts:F1} W";
    }

    private string FormatEnergy(float wattHours)
    {
        if (wattHours >= 1_000_000f)
            return $"{wattHours / 1_000_000f:F2} MWh";
        if (wattHours >= 1000f)
            return $"{wattHours / 1000f:F2} kWh";
        return $"{wattHours:F1} Wh";
    }

    // ========================================================================
    // DAILY ENERGY CALCULATION
    // ========================================================================

    private void UpdateDailyEnergy()
    {
        if (!calculateDailyEnergy || rotateSun == null) return;

        DateTime currentDateTime = rotateSun.CurrentLocalDateTime;
        if (currentDateTime == lastCalculatedDateTime) return;

        RecalculateDailyEnergy();
    }

    private void RecalculateDailyEnergy()
    {
        if (!calculateDailyEnergy || rotateSun == null || solarPanels == null || solarPanels.Length == 0)
            return;

        DateTime selectedDateTime = rotateSun.CurrentLocalDateTime;
        DateTime midnight = selectedDateTime.Date;

        if (selectedDateTime <= midnight)
        {
            accumulatedEnergyWh = 0f;
            lastCalculatedDateTime = selectedDateTime;
            return;
        }

        double totalEnergyWh = 0.0;
        DateTime sampleTime = midnight;
        DateTime previousSampleTime = sampleTime;

        float previousPower = EstimatePlantPowerAt(previousSampleTime);

        while (sampleTime < selectedDateTime)
        {
            DateTime nextSampleTime = sampleTime.AddMinutes(energyCalculationStepMinutes);
            if (nextSampleTime > selectedDateTime)
                nextSampleTime = selectedDateTime;

            float nextPower = EstimatePlantPowerAt(nextSampleTime);
            double intervalHours = (nextSampleTime - previousSampleTime).TotalHours;

            double averagePower = (previousPower + nextPower) / 2.0;
            totalEnergyWh += averagePower * intervalHours;

            previousSampleTime = nextSampleTime;
            previousPower = nextPower;
            sampleTime = nextSampleTime;
        }

        accumulatedEnergyWh = (float)Math.Max(0.0, totalEnergyWh);
        lastCalculatedDateTime = selectedDateTime;
    }

    private float EstimatePlantPowerAt(DateTime localDateTime)
    {
        if (rotateSun == null || solarPanels == null) return 0f;

        rotateSun.GetSolarPositionAt(localDateTime, out float elevation, out _);
        if (elevation <= 0f) return 0f;

        Vector3 sunDirection = rotateSun.GetSunDirectionAt(localDateTime);
        float totalEstimatedPower = 0f;

        // Sample panels (for large farms, sampling or full iteration)
        for (int i = 0; i < solarPanels.Length; i++)
        {
            RaycastShading panel = solarPanels[i];
            if (panel == null) continue;

            Vector3 normal = panel.SurfaceNormal;
            float dotProduct = Vector3.Dot(normal, sunDirection);
            if (dotProduct <= 0f) continue;

            Vector3 rayOrigin = panel.transform.position + (normal * panel.surfaceOffset);
            if (Physics.Raycast(rayOrigin, sunDirection, panel.raycastDistance, panel.obstacleLayerMask))
                continue;

            float irradiance = panel.solarIrradiance * dotProduct;
            totalEstimatedPower += irradiance * panel.panelArea * panel.panelEfficiency;
        }

        return totalEstimatedPower;
    }

    // ========================================================================
    // GRAPH
    // ========================================================================

    private void UpdateGraph()
    {
        graphTimer += Time.deltaTime;
        if (graphTimer < graphUpdateInterval) return;
        graphTimer = 0f;

        powerHistory.Add(TotalCurrentPower);
        if (powerHistory.Count > maxGraphPoints)
            powerHistory.RemoveAt(0);

        DrawGraph();
    }

    private void DrawGraph()
    {
        if (graphArea == null || graphLineContainer == null) return;

        for (int i = graphLineContainer.childCount - 1; i >= 0; i--)
            Destroy(graphLineContainer.GetChild(i).gameObject);

        if (powerHistory.Count < 2) return;

        float width = graphArea.rect.width;
        float height = graphArea.rect.height;
        float maxPower = 1f;

        foreach (float p in powerHistory)
        {
            if (p > maxPower) maxPower = p;
        }

        for (int i = 0; i < powerHistory.Count - 1; i++)
        {
            float x1 = (float)i / (powerHistory.Count - 1) * width;
            float x2 = (float)(i + 1) / (powerHistory.Count - 1) * width;
            float y1 = (powerHistory[i] / maxPower) * height;
            float y2 = (powerHistory[i + 1] / maxPower) * height;

            CreateGraphLine(new Vector2(x1, y1), new Vector2(x2, y2));
        }
    }

    private void CreateGraphLine(Vector2 start, Vector2 end)
    {
        GameObject lineObject = new GameObject("GraphLine");
        lineObject.transform.SetParent(graphLineContainer, false);

        Image image = lineObject.AddComponent<Image>();
        RectTransform rect = lineObject.GetComponent<RectTransform>();

        Vector2 direction = end - start;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        rect.sizeDelta = new Vector2(length, 3f);
        rect.anchoredPosition = start + direction * 0.5f;
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        image.raycastTarget = false;
    }

        // ========================================================================
    // TIME CONTROLS INITIALIZATION & LISTENERS
    // ========================================================================

    private void SetupTimeControls()
    {
        // 1. Toggle Button for Drawer
        if (timePanelButton != null)
        {
            timePanelButton.onClick.RemoveAllListeners();
            timePanelButton.onClick.AddListener(ToggleTimeSettingsDrawer);
        }
        // Start with the drawer closed by default
        if (timeSettingsDrawer != null)
        {
            timeSettingsDrawer.SetActive(false);
        }


        // 2. Mode Dropdown (Live vs Historical)
        if (timeModeDropdown != null)
        {
            if (timeModeDropdown.options.Count == 0)
            {
                timeModeDropdown.ClearOptions();
                timeModeDropdown.AddOptions(new List<string> { "Live Mode", "Historical Mode" });
            }

            timeModeDropdown.value = (rotateSun != null && rotateSun.IsHistoricalMode) ? 1 : 0;
            timeModeDropdown.onValueChanged.RemoveAllListeners();
            timeModeDropdown.onValueChanged.AddListener(OnTimeModeChanged);
        }

        // 3. Month Dropdown
        if (monthDropdown != null)
        {
            if (monthDropdown.options.Count == 0)
            {
                monthDropdown.ClearOptions();
                monthDropdown.AddOptions(new List<string>
                {
                    "01 - Jan", "02 - Feb", "03 - Mar", "04 - Apr",
                    "05 - May", "06 - Jun", "07 - Jul", "08 - Aug",
                    "09 - Sep", "10 - Oct", "11 - Nov", "12 - Dec"
                });
            }

            if (rotateSun != null)
                monthDropdown.value = rotateSun.CurrentMonth - 1;

            monthDropdown.onValueChanged.RemoveAllListeners();
            monthDropdown.onValueChanged.AddListener(OnMonthChanged);
        }

        // 4. Year Input Field
        if (yearInputField != null && rotateSun != null)
        {
            yearInputField.text = rotateSun.CurrentYear.ToString();
            yearInputField.onEndEdit.RemoveAllListeners();
            yearInputField.onEndEdit.AddListener(OnYearChanged);
        }

        // 5. Day Slider (1 - 31)
        if (daySlider != null && rotateSun != null)
        {
            daySlider.minValue = 1;
            daySlider.maxValue = DateTime.DaysInMonth(rotateSun.CurrentYear, rotateSun.CurrentMonth);
            daySlider.wholeNumbers = true;
            daySlider.value = rotateSun.CurrentDay;
            daySlider.onValueChanged.RemoveAllListeners();
            daySlider.onValueChanged.AddListener(OnDayChanged);
            UpdateDaySliderText(rotateSun.CurrentDay);
        }

        // 6. Time-of-Day Scrubber Slider (0 to 1439 minutes)
        if (timeOfDaySlider != null && rotateSun != null)
        {
            timeOfDaySlider.minValue = 0;
            timeOfDaySlider.maxValue = 1439; // 23 hours * 60 + 59 min
            timeOfDaySlider.wholeNumbers = true;
            int totalMin = rotateSun.CurrentHour * 60 + rotateSun.CurrentMinute;
            timeOfDaySlider.value = totalMin;
            timeOfDaySlider.onValueChanged.RemoveAllListeners();
            timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayChanged);
            UpdateTimeOfDaySliderText(totalMin);
        }

        // 7. Play / Pause Button
        if (playPauseButton != null)
        {
            playPauseButton.onClick.RemoveAllListeners();
            playPauseButton.onClick.AddListener(OnPlayPauseClicked);
            UpdatePlayPauseButtonText();
        }

        // 8. Time Speed Slider
        if (timeSpeedSlider != null && rotateSun != null)
        {
            timeSpeedSlider.minValue = 1f;
            timeSpeedSlider.maxValue = 3600f;
            timeSpeedSlider.value = rotateSun.TimeScaleMultiplier;
            timeSpeedSlider.onValueChanged.RemoveAllListeners();
            timeSpeedSlider.onValueChanged.AddListener(OnTimeSpeedChanged);
            UpdateTimeSpeedText(timeSpeedSlider.value);
        }

        UpdateDrawerVisibility();
    }

    public void ToggleTimeSettingsDrawer()
    {
        if (timeSettingsDrawer != null)
        {
            bool newState = !timeSettingsDrawer.activeSelf;
            timeSettingsDrawer.SetActive(newState);
            Debug.Log($"[SolarUI] Time settings drawer toggled: {newState}");
        }
        else
        {
            Debug.LogWarning("[SolarUI] TimeSettingsDrawer is not assigned! Please drag your settings panel into the 'Time Settings Drawer' slot on SolarUI.");
        }
    }


    private void UpdateDrawerVisibility()
    {
        bool isHistorical = rotateSun != null && rotateSun.IsHistoricalMode;
        if (historicalControlsContainer != null)
        {
            historicalControlsContainer.SetActive(isHistorical);
        }
    }

    // ========================================================================
    // UI EVENT HANDLERS
    // ========================================================================

    private void OnTimeModeChanged(int index)
    {
        if (rotateSun == null) return;

        if (index == 0) // Live
        {
            rotateSun.SetLiveMode();
        }
        else // Historical
        {
            rotateSun.SetHistoricalMode();
        }

        UpdateDrawerVisibility();
        SyncControlsWithCurrentTime();
        RecalculateDailyEnergy();
    }

    private void OnTimeOfDayChanged(float value)
    {
        if (rotateSun == null || !rotateSun.IsHistoricalMode) return;

        int totalMinutes = Mathf.RoundToInt(value);
        int hour = totalMinutes / 60;
        int minute = totalMinutes % 60;

        rotateSun.SetHistoricalTime(hour, minute);
        UpdateTimeOfDaySliderText(totalMinutes);
    }

    private void OnMonthChanged(int index)
    {
        if (rotateSun == null || !rotateSun.IsHistoricalMode) return;

        int newMonth = index + 1;
        int maxDays = DateTime.DaysInMonth(rotateSun.CurrentYear, newMonth);
        int day = Mathf.Clamp(rotateSun.CurrentDay, 1, maxDays);

        if (daySlider != null)
        {
            daySlider.maxValue = maxDays;
            daySlider.SetValueWithoutNotify(day);
            UpdateDaySliderText(day);
        }

        rotateSun.SetHistoricalDate(rotateSun.CurrentYear, newMonth, day);
        RecalculateDailyEnergy();
    }

    private void OnYearChanged(string text)
    {
        if (rotateSun == null || !rotateSun.IsHistoricalMode) return;

        if (int.TryParse(text, out int year))
        {
            year = Mathf.Clamp(year, 1990, 2100);
            int maxDays = DateTime.DaysInMonth(year, rotateSun.CurrentMonth);
            int day = Mathf.Clamp(rotateSun.CurrentDay, 1, maxDays);

            if (daySlider != null)
            {
                daySlider.maxValue = maxDays;
                daySlider.SetValueWithoutNotify(day);
                UpdateDaySliderText(day);
            }

            rotateSun.SetHistoricalDate(year, rotateSun.CurrentMonth, day);
            RecalculateDailyEnergy();
        }
    }

    private void OnDayChanged(float value)
    {
        if (rotateSun == null || !rotateSun.IsHistoricalMode) return;

        int day = Mathf.RoundToInt(value);
        rotateSun.SetHistoricalDate(rotateSun.CurrentYear, rotateSun.CurrentMonth, day);
        UpdateDaySliderText(day);
        RecalculateDailyEnergy();
    }

    private void OnPlayPauseClicked()
    {
        if (rotateSun == null) return;
        rotateSun.PlayHistoricalTime = !rotateSun.PlayHistoricalTime;
        UpdatePlayPauseButtonText();
    }

    private void OnTimeSpeedChanged(float value)
    {
        if (rotateSun == null) return;
        rotateSun.TimeScaleMultiplier = value;
        UpdateTimeSpeedText(value);
    }

    // ========================================================================
    // SYNC SLIDERS DURING SIMULATION
    // ========================================================================

    private void SyncControlsWithCurrentTime()
    {
        if (rotateSun == null) return;

        int totalMin = rotateSun.CurrentHour * 60 + rotateSun.CurrentMinute;
        if (timeOfDaySlider != null && !Mathf.Approximately(timeOfDaySlider.value, totalMin))
        {
            timeOfDaySlider.SetValueWithoutNotify(totalMin);
            UpdateTimeOfDaySliderText(totalMin);
        }

        if (daySlider != null && !Mathf.Approximately(daySlider.value, rotateSun.CurrentDay))
        {
            daySlider.SetValueWithoutNotify(rotateSun.CurrentDay);
            UpdateDaySliderText(rotateSun.CurrentDay);
        }

        if (monthDropdown != null && monthDropdown.value != (rotateSun.CurrentMonth - 1))
        {
            monthDropdown.SetValueWithoutNotify(rotateSun.CurrentMonth - 1);
        }

        if (yearInputField != null && yearInputField.text != rotateSun.CurrentYear.ToString())
        {
            yearInputField.SetTextWithoutNotify(rotateSun.CurrentYear.ToString());
        }

        UpdatePlayPauseButtonText();
    }

    private void UpdateTimeOfDaySliderText(int totalMinutes)
    {
        if (timeOfDaySliderText != null)
        {
            int h = totalMinutes / 60;
            int m = totalMinutes % 60;
            timeOfDaySliderText.text = $"{h:00}:{m:00}";
        }
    }

    private void UpdateDaySliderText(int day)
    {
        if (daySliderText != null)
        {
            daySliderText.text = $"Day: {day:00}";
        }
    }

    private void UpdateTimeSpeedText(float speed)
    {
        if (timeSpeedText != null)
        {
            timeSpeedText.text = $"{speed:F0}x";
        }
    }

    private void UpdatePlayPauseButtonText()
    {
        if (playPauseButtonText != null && rotateSun != null)
        {
            playPauseButtonText.text = rotateSun.PlayHistoricalTime ? "⏸ Pause" : "▶ Play";
        }
    }

}
