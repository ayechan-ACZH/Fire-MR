using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using TMPro;

public class SaveDataToCloud : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the ExtinguishFire script")]
    public ExtinguishFire extinguishFire;

    [Header("Settings")]
    [Tooltip("Auto-save interval in seconds (0 to disable auto-save)")]
    public float autoSaveInterval = 30f;

    private float autoSaveTimer = 0f;
    [SerializeField]
    private TextMeshProUGUI buttonText;

    async void Start()
    {
        // Ensure Unity Services are initialized
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            await UnityServices.InitializeAsync();
        }

        // Ensure player is signed in
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        // Find ExtinguishFire if not assigned
        if (extinguishFire == null)
        {
            extinguishFire = FindObjectOfType<ExtinguishFire>();
            if (extinguishFire == null)
            {
                Debug.LogError("ExtinguishFire script not found! Please assign it in the inspector.");
            }
        }
    }

    void Update()
    {
        // Auto-save if enabled
        if (autoSaveInterval > 0)
        {
            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer >= autoSaveInterval)
            {
                autoSaveTimer = 0f;
                SaveWaterUsedToCloud();
            }
        }
    }

    /// <summary>
    /// Sanitizes a string to be used as a valid cloud save key
    /// </summary>
    private string SanitizeKeyName(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "Player";
        }

        // Replace spaces and special characters with underscores
        // Unity Cloud Save keys should only contain alphanumeric characters and underscores
        string sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");
        
        // Ensure it doesn't start with a number
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "P_" + sanitized;
        }

        return sanitized;
    }

    /// <summary>
    /// Saves the current water used amount to Unity Cloud Save Player Data
    /// </summary>
    public async void SaveWaterUsedToCloud()
    {
        if (extinguishFire == null)
        {
            Debug.LogError("ExtinguishFire reference is null. Cannot save water used data.");
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player is not signed in. Cannot save to cloud.");
            return;
        }

        try
        {
            // Get the water used amount directly
            float waterUsed = extinguishFire.amtWaterUsed;
            string playerName = SanitizeKeyName(extinguishFire.currentPlayerName);

            // Validate player name
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogError("Player name is invalid. Cannot save to cloud.");
                buttonText.text = "Invalid Player Name";
                return;
            }

            string keyName = playerName + "_WaterUsed";
            Debug.Log($"Attempting to save with key: {keyName}, value: {waterUsed}");

            // Create the data to save - Unity Cloud Save expects specific types
            var data = new Dictionary<string, object>
            {
                { keyName, waterUsed }  // Save as int directly, not string
            };

            // Save to cloud
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            Debug.Log($"Successfully saved water used ({waterUsed}) to cloud for player: {playerName}");
            buttonText.text = "Water Used Saved: " + waterUsed;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving water used to cloud: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
            }
            buttonText.text = "Error Saving Water Used";
        }
    }

    /// <summary>
    /// Saves all game data including water used, time, and score to cloud
    /// </summary>
    /// <param name="finalScore">The final score</param>
    /// <param name="timeToExtinguish">Time taken to extinguish fire</param>
    /// <param name="timeSinceFireStart">Time since fire started</param>
    public async void SaveAllGameDataToCloud(int finalScore, float timeToExtinguish, float timeSinceFireStart)
    {
        if (extinguishFire == null)
        {
            Debug.LogError("ExtinguishFire reference is null. Cannot save game data.");
            return;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player is not signed in. Cannot save to cloud.");
            return;
        }

        try
        {
            float waterUsed = extinguishFire.amtWaterUsed;
            string playerName = SanitizeKeyName(extinguishFire.currentPlayerName);

            // Validate player name
            if (string.IsNullOrEmpty(playerName))
            {
                Debug.LogError("Player name is invalid. Cannot save to cloud.");
                return;
            }

            // Create comprehensive data to save - save as appropriate types
            var data = new Dictionary<string, object>
            {
                { playerName + "_WaterUsed", waterUsed },
                { playerName + "_FinalScore", finalScore },
                { playerName + "_TimeToExtinguishFire", timeToExtinguish },
                { playerName + "_TimeSinceFireStart", timeSinceFireStart }
            };

            // Save to cloud
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);

            Debug.Log($"Successfully saved all game data to cloud for player: {playerName}");
            Debug.Log($"Water Used: {waterUsed}, Final Score: {finalScore}, Time to Extinguish: {timeToExtinguish}, Time Since Fire Start: {timeSinceFireStart}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error saving game data to cloud: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
            }
        }
    }

    /// <summary>
    /// Loads water used data from cloud for the current player
    /// </summary>
    public async Task<int> LoadWaterUsedFromCloud()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("Player is not signed in. Cannot load from cloud.");
            return 0;
        }

        try
        {
            string playerName = SanitizeKeyName(extinguishFire.currentPlayerName);
            string keyName = playerName + "_WaterUsed";
            var keys = new HashSet<string> { keyName };
            
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            
            if (playerData.TryGetValue(keyName, out var waterUsedData))
            {
                // Try to get as int first, then fall back to string parsing
                try
                {
                    int waterUsed = waterUsedData.Value.GetAs<int>();
                    Debug.Log($"Loaded water used from cloud: {waterUsed}");
                    return waterUsed;
                }
                catch
                {
                    // Fall back to string parsing for backwards compatibility
                    string waterUsedStr = waterUsedData.Value.GetAs<string>();
                    if (int.TryParse(waterUsedStr, out int waterUsed))
                    {
                        Debug.Log($"Loaded water used from cloud (as string): {waterUsed}");
                        return waterUsed;
                    }
                }
            }
            
            Debug.Log("No water used data found in cloud for current player.");
            return 0;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading water used from cloud: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.LogError($"Inner exception: {ex.InnerException.Message}");
            }
            return 0;
        }
    }
}
