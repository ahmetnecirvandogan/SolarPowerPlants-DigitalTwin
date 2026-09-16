using System;
using UnityEngine;

public class RaycastShading : MonoBehaviour
{
    public enum NormalAxis
    {
        Forward,
        Up,
        Back,
        Down,
        Right,
        Left
    }

    [Header("Environment Settings")]
    [Tooltip("Reference to the directional sun light or sun transform.")]
    public Transform sunLight;

    [Tooltip("Base solar radiation in W/m² (STC standard is 1000 W/m²)")]
    public float solarIrradiance = 1000f;

    public float raycastDistance = 1000f;
    public LayerMask obstacleLayerMask = ~0;

    [Header("Panel Specifications")]
    [Tooltip("Which local axis points perpendicular to the solar cell face.")]
    public NormalAxis panelNormalAxis = NormalAxis.Forward;

    [Tooltip("Surface area in square meters per panel")]
    public float panelArea = 2.0f;

    [Tooltip("Conversion efficiency (e.g. 0.20 for 20%)")]
    public float panelEfficiency = 0.20f;

    [Header("Physics Settings")]
    [Tooltip("Offset above panel to avoid self-collision")]
    public float surfaceOffset = 0.05f;

    [Header("Partial Shading Settings")]
    [Tooltip("Panel dimensions in meters for sampling grid")]
    public float panelWidth = 1.0f;
    public float panelHeight = 2.0f;

    [Range(1, 9)]
    [Tooltip("1 = center only (fastest), 5 = 4 corners + center, 6 = 2x3 string grid")]
    public int samplePoints = 5;

    // Output: 0.0 = completely clear, 1.0 = completely shaded
    public float ShadedPercentage { get; private set; }


    // Runtime values
    public float CurrentPower { get; private set; }
    public float CurrentIntensity { get; private set; }
    public bool IsShaded { get; private set; }
    public float CurrentIrradiance { get; private set; }

    public Vector3 SurfaceNormal
    {
        get
        {
            switch (panelNormalAxis)
            {
                case NormalAxis.Forward: return transform.forward;
                case NormalAxis.Up: return transform.up;
                case NormalAxis.Back: return -transform.forward;
                case NormalAxis.Down: return -transform.up;
                case NormalAxis.Right: return transform.right;
                case NormalAxis.Left: return -transform.right;
                default: return transform.forward;
            }
        }
    }


    private void Awake()
    {
        if (sunLight == null)
        {
            RotateSun sun = FindObjectOfType<RotateSun>();
            if (sun != null) sunLight = sun.transform;
        }
    }

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

public void CalculateSolarOutput()
{
    Vector3 sunDirection = -sunLight.forward;
    Vector3 normal = SurfaceNormal;

    float dotProduct = Vector3.Dot(normal, sunDirection);
    if (dotProduct <= 0f)
    {
        CurrentIntensity = 0f;
        CurrentIrradiance = 0f;
        CurrentPower = 0f;
        IsShaded = false;
        ShadedPercentage = 0f;
        return;
    }

    // Determine sample ray origins across the panel surface
    Vector3[] sampleOrigins = GetSampleOrigins(normal);
    int blockedCount = 0;

    for (int i = 0; i < sampleOrigins.Length; i++)
    {
        if (Physics.Raycast(sampleOrigins[i], sunDirection, raycastDistance, obstacleLayerMask))
        {
            blockedCount++;
        }
    }

    ShadedPercentage = (float)blockedCount / sampleOrigins.Length;
    IsShaded = ShadedPercentage > 0.01f;

    // Linear unshaded power factor (1.0 = full sun, 0.5 = half sun, 0.0 = full shadow)
    float unshadedFactor = 1.0f - ShadedPercentage;

    CurrentIntensity = dotProduct * unshadedFactor;
    CurrentIrradiance = solarIrradiance * CurrentIntensity;
    CurrentPower = solarIrradiance * dotProduct * unshadedFactor * panelArea * panelEfficiency;
}

private Vector3[] GetSampleOrigins(Vector3 normal)
{
    Vector3 center = transform.position + (normal * surfaceOffset);
    if (samplePoints <= 1) return new Vector3[] { center };

    // Get lateral directions along the panel plane
    Vector3 right = transform.right * (panelWidth * 0.4f);
    Vector3 up = Vector3.Cross(normal, transform.right).normalized * (panelHeight * 0.4f);

    if (samplePoints == 5)
    {
        return new Vector3[]
        {
            center,                  // Center
            center + right + up,     // Top-Right
            center - right + up,     // Top-Left
            center + right - up,     // Bottom-Right
            center - right - up      // Bottom-Left
        };
    }

    // Default to center
    return new Vector3[] { center };
}


    private void OnDrawGizmosSelected()
    {
        Vector3 normal = SurfaceNormal;
        Vector3[] origins = GetSampleOrigins(normal);

        // Draw normal vector in Cyan at center
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + normal * surfaceOffset, normal * 1.5f);

        if (sunLight == null) return;

        Vector3 sunDir = -sunLight.forward;

        // Draw each sample ray: Yellow if clear, Red if blocked
        for (int i = 0; i < origins.Length; i++)
        {
            bool hit = Physics.Raycast(origins[i], sunDir, raycastDistance, obstacleLayerMask);
            Gizmos.color = hit ? Color.red : Color.yellow;
            Gizmos.DrawRay(origins[i], sunDir * (hit ? 2f : 5f));
            Gizmos.DrawSphere(origins[i], 0.03f); // Small sphere on panel surface
        }
    }

}
