using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;

public class HideHandMenu : MonoBehaviour
{
    [Header("UI Settings")]
    [Tooltip("The UI Canvas to show/hide based on hand tracking")]
    public Canvas uiCanvas;

    [Header("Look At Settings")]
    [Tooltip("Should the canvas always face the camera/player?")]
    public bool lookAtCamera = true;

    [Tooltip("Only rotate on Y axis (prevents tilting up/down)")]
    public bool lockVerticalRotation = true;

    [Header("Hand Tracking")]
    [Tooltip("Reference to the left hand GameObject. Works with both OVRHand and Building Block Synthetic Hands.")]
    public GameObject leftHandObject;

    private OVRHand ovrHand;
    private IHand syntheticHand;
    private bool wasTracked = false;
    private Transform cameraTransform;

    void Start()
    {
        // Find the left hand automatically if not assigned
        if (leftHandObject == null)
        {
            // Try to find OVRHand first
            OVRHand[] ovrHands = FindObjectsOfType<OVRHand>();
            foreach (OVRHand hand in ovrHands)
            {
                if (hand.gameObject.name.ToLower().Contains("left"))
                {
                    leftHandObject = hand.gameObject;
                    Debug.Log("Found left OVRHand: " + hand.gameObject.name);
                    break;
                }
            }

            // If no OVRHand found, try to find SyntheticHand (Building Blocks)
            if (leftHandObject == null)
            {
                SyntheticHand[] syntheticHands = FindObjectsOfType<SyntheticHand>();
                foreach (SyntheticHand hand in syntheticHands)
                {
                    if (hand.Handedness == Handedness.Left)
                    {
                        leftHandObject = hand.gameObject;
                        Debug.Log("Found left SyntheticHand: " + hand.gameObject.name);
                        break;
                    }
                }
            }
            
            if (leftHandObject == null)
            {
                Debug.LogWarning("Could not find left hand automatically. Please assign it manually in the inspector.");
            }
        }

        // Cache the hand component
        if (leftHandObject != null)
        {
            ovrHand = leftHandObject.GetComponent<OVRHand>();
            if (ovrHand == null)
            {
                syntheticHand = leftHandObject.GetComponent<IHand>();
            }
        }

        // Hide canvas initially if hand is not tracked
        if (uiCanvas != null)
        {
            uiCanvas.gameObject.SetActive(IsHandTracked());
        }

        // Cache the main camera transform for look-at functionality
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("Main camera not found. Look-at functionality will not work.");
        }
    }

    void Update()
    {
        if (uiCanvas == null)
            return;

        // Check if the left hand tracking state has changed
        bool isTracked = IsHandTracked();

        if (isTracked != wasTracked)
        {
            // Update canvas visibility based on tracking state
            uiCanvas.gameObject.SetActive(isTracked);
            wasTracked = isTracked;
        }

        // Make the canvas face the camera if enabled and canvas is active
        if (lookAtCamera && cameraTransform != null && uiCanvas.gameObject.activeSelf)
        {
            Vector3 directionToCamera = cameraTransform.position - uiCanvas.transform.position;
            
            if (lockVerticalRotation)
            {
                // Only rotate on Y axis (horizontal rotation)
                directionToCamera.y = 0;
            }
            
            if (directionToCamera != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
                uiCanvas.transform.rotation = targetRotation;
            }
        }
    }

    /// <summary>
    /// Checks if the hand is currently tracked, works with both OVRHand and SyntheticHand
    /// </summary>
    private bool IsHandTracked()
    {
        if (ovrHand != null)
        {
            return ovrHand.IsTracked;
        }
        else if (syntheticHand != null)
        {
            return syntheticHand.IsTrackedDataValid;
        }
        return false;
    }
}
