using UnityEngine;

public class RaycastShading : MonoBehaviour
{
    [Header("Environment Settings")]
    public Transform sunLight;

    [Tooltip("The base solar radiation in W/m² (Standard Test Condition is 1000)")]
    public float solarIrradiance = 1000f;

    public float raycastDistance = 1000f;

    [Header("Panel Specifications")]
    [Tooltip("Manual surface area in square meters")]
    public float panelArea = 2.0f;

    [Tooltip("Conversion efficiency of the panel (e.g., 0.20 for 20%)")]
    public float panelEfficiency = 0.20f;

    [Header("Physics Settings")]
    [Tooltip("Distance to offset the raycast above the panel to prevent self-collision")]
    public float surfaceOffset = 0.06f;

    // ------------------------------------------------------------------------
    // Runtime Values
    // These values can be accessed by the UI and other scripts.
    // ------------------------------------------------------------------------

    /// <summary>
    /// Current power output of the panel in Watts.
    /// </summary>
    public float CurrentPower { get; private set; }

    /// <summary>
    /// Current normalized sunlight intensity reaching the panel (0-1).
    /// </summary>
    public float CurrentIntensity { get; private set; }

    /// <summary>
    /// Whether the panel is currently shaded.
    /// </summary>
    public bool IsShaded { get; private set; }

    /// <summary>
    /// Current irradiance reaching the panel in W/m².
    /// </summary>
    public float CurrentIrradiance { get; private set; }

    void Update()
    {
        if (sunLight == null)
        {
            CurrentPower = 0f;
            CurrentIntensity = 0f;
            CurrentIrradiance = 0f;
            IsShaded = true;
            return;
        }

        CalculateSolarOutput();
    }

    void CalculateSolarOutput()
    {
        Vector3 sunDirection = -sunLight.forward;

        // Offset the raycast to start just above the panel's surface
        // to prevent the panel itself from blocking the ray.
        Vector3 rayOrigin = transform.position + (transform.up * surfaceOffset);

        // Cast a ray toward the sun.
        bool isShaded = Physics.Raycast(
            rayOrigin,
            sunDirection,
            raycastDistance
        );

        IsShaded = isShaded;

        if (isShaded)
        {
            CurrentPower = 0f;
            CurrentIntensity = 0f;
            CurrentIrradiance = 0f;

            Debug.DrawRay(
                rayOrigin,
                sunDirection * 10f,
                Color.red
            );

            Debug.Log("Panel is shaded. Output: 0W");

            return;
        }

        Debug.DrawRay(
            rayOrigin,
            sunDirection * 10f,
            Color.green
        );

        // Calculate sunlight intensity using Lambert's Cosine Law.
        float dotProduct = Vector3.Dot(
            transform.up,
            sunDirection
        );

        float intensity = Mathf.Max(0f, dotProduct);

        // Store normalized intensity.
        CurrentIntensity = intensity;

        // Calculate actual irradiance reaching the panel.
        float receivedIrradiance = solarIrradiance * intensity;

        CurrentIrradiance = receivedIrradiance;

        // Calculate final electrical power output.
        float rawEnergy =
            receivedIrradiance *
            panelArea *
            panelEfficiency;

        CurrentPower = rawEnergy;

        Debug.Log(
            $"Irradiance: {CurrentIrradiance:F2} W/m² | " +
            $"Area: {panelArea}m² | " +
            $"Output: {CurrentPower:F2} W"
        );
    }
}