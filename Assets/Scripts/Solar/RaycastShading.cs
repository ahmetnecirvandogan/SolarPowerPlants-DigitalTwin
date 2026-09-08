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

    void Update()
    {
        if (sunLight == null) return;
        CalculateSolarOutput();
    }

    void CalculateSolarOutput()
    {
        Vector3 sunDirection = -sunLight.forward;
        
        // Offset the raycast to start just above the panel's surface based on your Inspector value
        Vector3 rayOrigin = transform.position + (transform.up * surfaceOffset);

        // Cast the ray
        bool isShaded = Physics.Raycast(rayOrigin, sunDirection, raycastDistance);

        if (isShaded)
        {
            Debug.DrawRay(rayOrigin, sunDirection * 10f, Color.red);
            Debug.Log("Panel is shaded. Output: 0W");
            return; 
        }

        Debug.DrawRay(rayOrigin, sunDirection * 10f, Color.green);

        // Calculate intensity via Lambert's Cosine Law
        float dotProduct = Vector3.Dot(transform.up, sunDirection);
        float intensity = Mathf.Max(0, dotProduct);
        
        // Calculate final output using Inspector variables
        float rawEnergy = solarIrradiance * panelArea * panelEfficiency * intensity;

        Debug.Log($"Irradiance: {solarIrradiance} | Area: {panelArea}m² | Output: {rawEnergy:F2} W");
    }
}
