using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ProgressCircle : MonoBehaviour
{
     [Header("UI References")]
    public Image loadingCircle; // Assign an Image with 'Filled' type
    public Text percentageText; // Optional text display

    [Header("Loading Settings")]
    [Range(0f, 1f)]
    public float progress = 0f;
    public float loadingSpeed = 1f;

    [Header("Behaviour")]
    public bool autoSimulate = false; // set true only for standalone testing
    public float dampingSpeed = 3f; // how fast the circle visually catches up to the target

    private float displayedProgress = 0f; // the smoothed value actually shown

    void Start()
    {
        displayedProgress = progress;
        UpdateUI(displayedProgress);
    }

    void Update()
    {
        if (autoSimulate)
        {
            progress += Time.deltaTime * loadingSpeed;
            progress = Mathf.Clamp01(progress);
        }

        // smoothly move displayed value toward target — MoveTowards guarantees it always reaches progress exactly
        displayedProgress = Mathf.MoveTowards(displayedProgress, progress, Time.deltaTime * dampingSpeed);
        UpdateUI(displayedProgress);
    }

    void UpdateUI(float value)
    {
        if (loadingCircle != null)
        {
            loadingCircle.fillAmount = value;
        }

        if (percentageText != null)
        {
            percentageText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
    }

    // Call this externally if using real loading progress
    public void SetProgress(float value)
    {
        progress = Mathf.Clamp01(value);
        // displayedProgress will smoothly lerp toward progress in Update()
    }
}
