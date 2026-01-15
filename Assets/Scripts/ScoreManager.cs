using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading.Tasks;


public class ScoreManager : MonoBehaviour
{
    CloudSaveDataManager cloudSaveDataManager;

    // time to extinguish fire
    // amount of water used
    // time taken to discover fire
    // time since fire started
    // fire extinguished bool

    public TextMeshProUGUI serverConfigStatusText;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    async Task Awake()
    {
        cloudSaveDataManager = GameObject.FindGameObjectWithTag("RHController").GetComponent<CloudSaveDataManager>();
    }

    public void getFinalScore(
        string currentPlayerName,
        bool fireExtinguished,
        int timeToExtinguishFire,
        int waterUsed,
        int timeSinceFireStart
        // int timeTakenToDiscoverFire
    )

    
    {
        int score = timeSinceFireStart - waterUsed - timeToExtinguishFire;

        // time to extinguish fire goes negative - this is the amount of wasted extinguisher liquid
        serverConfigStatusText.text += "Time to extinguish fire: " + timeToExtinguishFire + "\n";
        serverConfigStatusText.text += "Water used: " + waterUsed + "\n";
        serverConfigStatusText.text += "Time since fire started " + timeSinceFireStart + "\n";
        
        // right now the lower the new score the better
        serverConfigStatusText.text += "New score: " + score + "\n";

        // send score to server
        // Sanitize player name to avoid invalid characters in keys
        string sanitizedPlayerName = SanitizeKeyName(currentPlayerName);
        Debug.Log($"[ScoreManager] Saving scores for player: '{currentPlayerName}' (sanitized: '{sanitizedPlayerName}')");
        
        cloudSaveDataManager.SavePublicData(sanitizedPlayerName + "_TimeToExtinguishFire", timeToExtinguishFire.ToString());
        cloudSaveDataManager.SavePublicData(sanitizedPlayerName + "_WaterUsed", waterUsed.ToString());
        cloudSaveDataManager.SavePublicData(sanitizedPlayerName + "_TimeSinceFireStart", timeSinceFireStart.ToString());
        cloudSaveDataManager.SavePublicData(sanitizedPlayerName + "_FinalScore", score.ToString());
    }
    
    // Sanitize key names by removing invalid characters
    private string SanitizeKeyName(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "UnknownPlayer";
            
        // Replace invalid characters with underscores
        char[] invalidChars = new char[] { '/', '\\', '#', '?', '&', '=', ' ' };
        string sanitized = input;
        foreach (char c in invalidChars)
        {
            sanitized = sanitized.Replace(c, '_');
        }
        
        // Limit length to 200 to leave room for suffix
        if (sanitized.Length > 200)
            sanitized = sanitized.Substring(0, 200);
            
        return sanitized;
        
    }
}
