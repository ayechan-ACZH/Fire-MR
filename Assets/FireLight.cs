using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireLight : MonoBehaviour
{
    [Header("Flicker Settings")]
    [Tooltip("The light component to flicker")]
    private Light fireLight;
    
    [Tooltip("Minimum light intensity")]
    public float minIntensity = 0.8f;
    
    [Tooltip("Maximum light intensity")]
    public float maxIntensity = 1.5f;
    
    [Tooltip("How fast the light flickers")]
    public float flickerSpeed = 0.1f;
    
    [Tooltip("Randomness of the flicker")]
    public float flickerAmount = 0.2f;
    
    private float targetIntensity;
    private float currentIntensity;
    
    void Start()
    {
        // Get the Light component attached to this GameObject
        fireLight = GetComponent<Light>();
        
        if (fireLight == null)
        {
            Debug.LogError("FireLight script requires a Light component on the same GameObject!");
            enabled = false;
            return;
        }
        
        currentIntensity = fireLight.intensity;
        targetIntensity = currentIntensity;
    }
    
    void Update()
    {
        if (fireLight == null) return;
        
        // Randomly change target intensity occasionally
        if (Random.value < flickerAmount)
        {
            targetIntensity = Random.Range(minIntensity, maxIntensity);
        }
        
        // Smoothly interpolate to target intensity
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, flickerSpeed);
        fireLight.intensity = currentIntensity;
    }
}
