using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;

public class DisplayPlayerDataUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI waterUsedText;
    public TextMeshProUGUI timeToExtinguishText;
    public TextMeshProUGUI timeSinceFireStartText;
    public TextMeshProUGUI deviceNameText;
    
    [Header("Leaderboard UI")]
    public TextMeshProUGUI leaderboardText;

    [Header("References")]
    public CloudSaveDataManager cloudSaveDataManager;

    void Start()
    {
        // Optional: Auto-load player data on start
        // LoadAndDisplayCurrentPlayerData();
    }

    /// <summary>
    /// Loads and displays the current authenticated player's data
    /// </summary>
    public async void LoadAndDisplayCurrentPlayerData()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("Player is not signed in. Cannot load player data.");
            return;
        }

        string playerId = AuthenticationService.Instance.PlayerId;
        
        // Get the current player name from somewhere (you might want to pass this as a parameter)
        string playerName = "Default Player"; // Replace with actual player name
        
        await LoadAndDisplayPlayerData(playerName, playerId);
    }

    /// <summary>
    /// Loads and displays a specific player's data by player name and ID
    /// </summary>
    /// <param name="playerName">The player's name used as key prefix</param>
    /// <param name="playerId">The player's authenticated ID</param>
    public async Task LoadAndDisplayPlayerData(string playerName, string playerId)
    {
        try
        {
            var keys = new HashSet<string> {
                playerName + "_FinalScore",
                playerName + "_WaterUsed",
                playerName + "_TimeToExtinguishFire",
                playerName + "_TimeSinceFireStart",
                "deviceNew"
            };
            
            var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
                keys, 
                new LoadOptions(new PublicReadAccessClassOptions())
            );
            
            // Update UI elements if they are assigned
            if (playerNameText != null)
                playerNameText.text = "Player: " + playerName;
            
            if (playerData.TryGetValue(playerName + "_FinalScore", out var score))
            {
                if (finalScoreText != null)
                    finalScoreText.text = "Final Score: " + score.Value.GetAs<string>();
            }
            
            if (playerData.TryGetValue(playerName + "_WaterUsed", out var water))
            {
                if (waterUsedText != null)
                    waterUsedText.text = "Water Used: " + water.Value.GetAs<string>();
            }
            
            if (playerData.TryGetValue(playerName + "_TimeToExtinguishFire", out var time))
            {
                if (timeToExtinguishText != null)
                    timeToExtinguishText.text = "Time to Extinguish: " + time.Value.GetAs<string>();
            }
            
            if (playerData.TryGetValue(playerName + "_TimeSinceFireStart", out var fireTime))
            {
                if (timeSinceFireStartText != null)
                    timeSinceFireStartText.text = "Time Since Fire Start: " + fireTime.Value.GetAs<string>();
            }
            
            if (playerData.TryGetValue("deviceNew", out var device))
            {
                if (deviceNameText != null)
                    deviceNameText.text = "Device: " + device.Value.GetAs<string>();
            }
            
            Debug.Log($"Successfully loaded and displayed data for player: {playerName}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading player data: {ex.Message}");
            
            // Display error in UI
            if (playerNameText != null)
                playerNameText.text = "Error loading player data";
        }
    }

    /// <summary>
    /// Loads and displays all players' scores in a leaderboard format
    /// </summary>
    public async void LoadAndDisplayLeaderboard()
    {
        if (cloudSaveDataManager == null)
        {
            Debug.LogError("CloudSaveDataManager reference is not set!");
            return;
        }

        string leaderboardData = "=== LEADERBOARD ===\n\n";
        
        string[] allPlayerIDs = new string[] {
            "SWLFCT1V5db4H8AH4cAaXYnW1r67",
            "gtISJtVu72LykdN6ohBZgNMf2Mfy",
            "jz46kXj64jRxIAyxwRooEEhBhlAl",
            "z1MkqBhkOvDwIeIMl6LQ7eLT4ONW"
        };

        foreach (string playerId in allPlayerIDs)
        {
            try
            {
                // Load device name and score data
                var keys = new HashSet<string> { "deviceNew" };
                var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    keys, 
                    new LoadOptions(new PublicReadAccessClassOptions())
                );
                
                string deviceName = "Unknown Device";
                if (playerData.TryGetValue("deviceNew", out var device))
                {
                    deviceName = device.Value.GetAs<string>();
                }
                
                leaderboardData += $"Device: {deviceName}\n";
                leaderboardData += $"Player ID: {playerId.Substring(0, 8)}...\n";
                leaderboardData += "---\n";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not load data for player {playerId}: {ex.Message}");
            }
        }
        
        if (leaderboardText != null)
        {
            leaderboardText.text = leaderboardData;
        }
        
        Debug.Log("Leaderboard data loaded and displayed");
    }

    /// <summary>
    /// Loads and displays detailed leaderboard with scores
    /// </summary>
    /// <param name="playerNames">Array of player names to look up</param>
    public async void LoadAndDisplayDetailedLeaderboard(string[] playerNames)
    {
        if (cloudSaveDataManager == null)
        {
            Debug.LogError("CloudSaveDataManager reference is not set!");
            return;
        }

        string leaderboardData = "=== DETAILED LEADERBOARD ===\n\n";
        
        string[] allPlayerIDs = new string[] {
            "SWLFCT1V5db4H8AH4cAaXYnW1r67",
            "gtISJtVu72LykdN6ohBZgNMf2Mfy",
            "jz46kXj64jRxIAyxwRooEEhBhlAl",
            "z1MkqBhkOvDwIeIMl6LQ7eLT4ONW"
        };

        for (int i = 0; i < allPlayerIDs.Length && i < playerNames.Length; i++)
        {
            string playerId = allPlayerIDs[i];
            string playerName = playerNames[i];
            
            try
            {
                var keys = new HashSet<string> {
                    playerName + "_FinalScore",
                    playerName + "_WaterUsed",
                    playerName + "_TimeToExtinguishFire",
                    "deviceNew"
                };
                
                var playerData = await CloudSaveService.Instance.Data.Player.LoadAsync(
                    keys, 
                    new LoadOptions(new PublicReadAccessClassOptions())
                );
                
                leaderboardData += $"Player: {playerName}\n";
                
                if (playerData.TryGetValue("deviceNew", out var device))
                    leaderboardData += $"Device: {device.Value.GetAs<string>()}\n";
                
                if (playerData.TryGetValue(playerName + "_FinalScore", out var score))
                    leaderboardData += $"Score: {score.Value.GetAs<string>()}\n";
                
                if (playerData.TryGetValue(playerName + "_WaterUsed", out var water))
                    leaderboardData += $"Water Used: {water.Value.GetAs<string>()}\n";
                
                if (playerData.TryGetValue(playerName + "_TimeToExtinguishFire", out var time))
                    leaderboardData += $"Time to Extinguish: {time.Value.GetAs<string>()}\n";
                
                leaderboardData += "\n---\n\n";
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not load data for player {playerName}: {ex.Message}");
                leaderboardData += $"Player: {playerName} - Data unavailable\n\n---\n\n";
            }
        }
        
        if (leaderboardText != null)
        {
            leaderboardText.text = leaderboardData;
        }
        
        Debug.Log("Detailed leaderboard data loaded and displayed");
    }

    /// <summary>
    /// Refreshes the currently displayed player data
    /// </summary>
    public void RefreshPlayerData()
    {
        LoadAndDisplayCurrentPlayerData();
    }

    /// <summary>
    /// Clears all UI text fields
    /// </summary>
    public void ClearDisplay()
    {
        if (playerNameText != null) playerNameText.text = "";
        if (finalScoreText != null) finalScoreText.text = "";
        if (waterUsedText != null) waterUsedText.text = "";
        if (timeToExtinguishText != null) timeToExtinguishText.text = "";
        if (timeSinceFireStartText != null) timeSinceFireStartText.text = "";
        if (deviceNameText != null) deviceNameText.text = "";
        if (leaderboardText != null) leaderboardText.text = "";
    }
}
