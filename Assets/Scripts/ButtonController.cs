using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonController : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private ExtinguishFire extinguishFire;

    [SerializeField] private GameObject myPanel;
    
    private Button button;

    void Start()
    {
        // Get the Button component
        button = GetComponent<Button>();
        
        // If no AudioSource is assigned, try to get one or add it
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        // Add listener to the button
        if (button != null)
        {
            button.onClick.AddListener(PlayButtonSound);
        }
    }

    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }

        extinguishFire.waterUsedText.text = "Fire Extinguished!: " + (600 + extinguishFire.timeToExtinguish) + " seconds used.";
    }

    void OnDestroy()
    {
        // Remove listener when the object is destroyed
        if (button != null)
        {
            button.onClick.RemoveListener(PlayButtonSound);
        }
    }
}
