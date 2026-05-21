using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.RemoteConfig;
using Unity.Services.Authentication;
using Unity.Services.Core;
using TMPro;
using Unity.Services.CloudSave;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using UnityEngine.SocialPlatforms.Impl;

public class ExtinguishFire : MonoBehaviour
{
    CloudSaveDataManager cloudSaveDataManager;
    ScoreManager scoreManager;
    DisplayPlayerDataUI displayPlayerDataUI;

    [SerializeField]
    private UIManager uiManager;

    [SerializeField]
    private SaveUserName saveUserName;

    public TextMeshProUGUI waterUsedText;

    public TextMeshProUGUI timeText;

    public TextMeshProUGUI finalScoreText;

    public GameObject trainerPanelUI;
    public GameObject firePrefab;

    private GameObject currentFireInstance;

    public ParticleSystem controllerWaterParticles;
    public ParticleSystem handWaterParticles;
    public ParticleSystem fireParticles;
    public GameObject fireAlarm;

    // track the spawned fire alarm instance so we can remove it safely without touching the prefab reference
    private GameObject currentFireAlarmInstance;
    public float timeToExtinguish = 10f; // seconds (was 600 frames at 60fps)

    public ProgressCircle progressCircle;
    private float initialTimeToExtinguish;

    public AudioSource aiNarrationAudio;
    public AudioSource endAINarrationAudio;

    [SerializeField]
    private AudioClip[] aiNarrationClips;
    // 4 ai audio clips for the game
    public AudioClip aiNarrationWelcome1;
    public AudioClip aiNarrationWelcome2;
    public AudioClip aiNarrationWelcome3;

    private bool aiNarrationStarted = false;

    private bool startTimer = false; // to track when to start the timeSinceFireStart timer
    public AudioSource fireExtinguishingAudio;
    ParticleSystem currentWaterParticles;
    bool soundIsPlaying;
    public float amtWaterUsed = 0f; // litres, based on a 6L UK extinguisher at ~0.15L/s
    public float timeSinceFireStart = 0f; // seconds

    // scoring (higher is better, max = 1000)
    // Full subscore when value is at or below the target.
    public float perfectTimeTargetSeconds = 15f;
    public float perfectWaterTargetLitres = 2.5f;

    // Zero subscore when value is at or above the cutoff.
    public float zeroTimeScoreSeconds = 60f;
    public float zeroWaterScoreLitres = 6f;

    [Range(0f, 1f)] public float timeWeight = 0.7f;  // normalized with waterWeight at runtime
    [Range(0f, 1f)] public float waterWeight = 0.3f;

    public TextMeshProUGUI serverConfigStatusText;

    public bool handTrackedMode = true; // false means controller trigger

    public string currentPlayerName = "Default Player";

    public bool leftHandSqueezeTrigger = true; // false means left hand squeeze wont trigger water particles

    public struct userAttributes { }

    public struct appAttributes { }

    // for hand squeeze tracking
    public GameObject palmCenterCollider;
    public GameObject middleFingerCollider;
    float distanceX;
    float distanceY;
    float distanceZ;
    public float distanceBetweenFingerAndPalm;
    public bool handControlsLocked = true;

    private TcpListener tcpListener;
    private bool isServerRunning;

    async Task InitializeRemoteConfigAsync()
    {
        // initialize handlers for unity game services
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    async Task Awake()
    {
        cloudSaveDataManager = GameObject.FindGameObjectWithTag("RHController").GetComponent<CloudSaveDataManager>();
        scoreManager = GameObject.FindGameObjectWithTag("RHController").GetComponent<ScoreManager>();
        displayPlayerDataUI = FindObjectOfType<DisplayPlayerDataUI>();

        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        // cloudSaveDataManager.LoadDataFromCloud();
        // cloudSaveDataManager.SavePlayerFileToCloud("virejfilee.csv", "test,1,3,test");

        // sends device name to cloud in public player data so it can be read by tablet app
        string userDevice = SystemInfo.deviceName;
        Debug.Log("Device Name: " + userDevice);
        cloudSaveDataManager.SavePublicData("deviceNew", userDevice);
        cloudSaveDataManager.LoadPublicDataByAllPlayerIds("deviceNew");

        // initialize Unity's authentication and core services
        await InitializeRemoteConfigAsync();

        // Add a listener to apply settings when successfully retrieved:
        RemoteConfigService.Instance.FetchCompleted += ApplyRemoteConfig;
        
        try
        {
            Debug.Log("Fetching remote config...");
            await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
            Debug.Log("Remote config fetch completed");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch remote config: {e.Message}");
        }

        // Start the TCP server
        StartServer();
    }



    void ApplyRemoteConfig(ConfigResponse configResponse)
    {
        Debug.Log($"ApplyRemoteConfig called with origin: {configResponse.requestOrigin}");
        //currentPlayerName = saveUserName.currentPlayerName;
        
        // Conditionally update settings, depending on the response's origin:
        switch (configResponse.requestOrigin)
        {
            case ConfigOrigin.Default:
                Debug.LogWarning("No settings loaded this session and no local cache file exists; using default values.");
                break;
            case ConfigOrigin.Cached:
                Debug.LogWarning("No settings loaded this session; using cached values from a previous session.");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("New settings loaded this session; updated values accordingly.");
                Debug.Log("handTrackedMode: " + RemoteConfigService.Instance.appConfig.GetBool("handTrackedMode"));
                Debug.Log("currentPlayerName: " + RemoteConfigService.Instance.appConfig.GetString("currentPlayerName"));
                break;
        }

        // Apply config values regardless of origin
        handTrackedMode = RemoteConfigService.Instance.appConfig.GetBool("handTrackedMode", true);
        currentPlayerName = RemoteConfigService.Instance.appConfig.GetString("currentPlayerName", "Default Player");
        
        Debug.Log($"Applied config - handTrackedMode: {handTrackedMode}, currentPlayerName: {currentPlayerName}");

        // set the current particle system based on the handWaterParticlesOn boolean
        if (handTrackedMode)
        {
            currentWaterParticles = handWaterParticles;
            controllerWaterParticles.Stop();
        }
        else
        {
            currentWaterParticles = controllerWaterParticles;
            handWaterParticles.Stop();
        }
    }

    private string GetLocalIPv4Address()
    {
        foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus == OperationalStatus.Up)
            {
                foreach (UnicastIPAddressInformation unicastIPAddressInformation in networkInterface.GetIPProperties().UnicastAddresses)
                {
                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return unicastIPAddressInformation.Address.ToString();
                    }
                }
            }
        }
        return "No network adapters with an IPv4 address in the system!";
    }

    // Start is called before the first frame update
    void Start()
    {
        initialTimeToExtinguish = timeToExtinguish;
        NormalizeScoreWeights();
        controllerWaterParticles.Stop();
        handWaterParticles.Stop();

        if (handTrackedMode)
        {
            currentWaterParticles = handWaterParticles;
        }
        else
        {
            currentWaterParticles = controllerWaterParticles;
        }

        soundIsPlaying = false;

        string localIPAddress = GetLocalIPv4Address();
        Debug.Log("Local IPv4 Address: " + localIPAddress);
        serverConfigStatusText.text += "\nLocal IPv4 Address: " + localIPAddress;

        //Play Ai Narration at the start of the game
        // if(!aiNarrationStarted){
        //         Debug.Log("Playing AI Narration");
            
        //         // make the trainer panel ui disappear
        //         trainerPanelUI.SetActive(false);

        //         StartAINarration(0);
        //         aiNarrationStarted = true;
        // }
    }

    private void NormalizeScoreWeights()
    {
        float total = timeWeight + waterWeight;

        if (total <= 0.0001f)
        {
            timeWeight = 0.7f;
            waterWeight = 0.3f;
            return;
        }

        timeWeight /= total;
        waterWeight /= total;
    }

    private float CalculateThresholdScore(float value, float fullScoreThreshold, float zeroScoreThreshold)
    {
        if (value <= fullScoreThreshold)
        {
            return 1f;
        }

        if (value >= zeroScoreThreshold)
        {
            return 0f;
        }

        float range = Mathf.Max(0.0001f, zeroScoreThreshold - fullScoreThreshold);
        float t = (value - fullScoreThreshold) / range;
        return 1f - t;
    }

    private bool HasWaterRemaining()
    {
        return amtWaterUsed < zeroWaterScoreLitres;
    }

    private void StopWaterSpray()
    {
        if (soundIsPlaying)
        {
            fireExtinguishingAudio.Stop();
            soundIsPlaying = false;
        }

        if (currentWaterParticles != null)
        {
            currentWaterParticles.Stop();
        }
    }

    // Update is called once per frame
    async Task Update()
    {
        if (!HasWaterRemaining())
        {
            amtWaterUsed = zeroWaterScoreLitres;
            StopWaterSpray();
        }

        if (leftHandSqueezeTrigger) {
            // for hand squeeze tracking
            Vector3 delta = middleFingerCollider.transform.position - palmCenterCollider.transform.position;
            distanceX = delta.x;
            distanceY = delta.y;
            distanceZ = delta.z;
            distanceBetweenFingerAndPalm = delta.magnitude * 100;
        }

        if (!handControlsLocked) 
        {
          // if the left trigger is pressed, play the water particles from the hand and increment the water used and play the sound
          if ((OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger) > 0.5f)||
              (leftHandSqueezeTrigger && (distanceBetweenFingerAndPalm < 4)))
          {
              if (!HasWaterRemaining())
              {
                  StopWaterSpray();
              }
              else
              {
                  if (!soundIsPlaying)
                  {
                      fireExtinguishingAudio.Play();
                      soundIsPlaying = true;
                  }

                  amtWaterUsed = Mathf.Min(amtWaterUsed + (0.15f * Time.deltaTime), zeroWaterScoreLitres);
                  currentWaterParticles.Play();

                  if (!HasWaterRemaining())
                  {
                      StopWaterSpray();
                  }

                  waterUsedText.text = amtWaterUsed.ToString("F1");
                  Debug.Log("Water used" + (amtWaterUsed));
              }

          }
          else
          {
              StopWaterSpray();
          }
        }
        // ifthe right trigger is pressed, instantiate the fire particles at the hand position
        
        if(Input.GetKeyDown(KeyCode.T)){
            progressCircle.SetProgress(0.5f);
            Debug.Log("Progress circle set to 50% for testing.");
        }
        
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch) || Input.GetKeyDown(KeyCode.F))
        {
            // get the transform position in a varable and set the y value to 0
            Vector3 spawnPos = transform.position;
            // spawnPos.y = 0; // this is if we want to spawn the fire particles at the floor level

            // If we previously spawned a fire, destroy that instance first (safe runtime destroy).
            if (currentFireInstance != null)
            {
                Destroy(currentFireInstance);
                currentFireInstance = null;
            }

            // Instantiate a fresh fire after cleaning up the previous spawned instance
            GameObject spawnedFire = Instantiate(firePrefab, spawnPos, Quaternion.identity);

            currentFireInstance = spawnedFire;

            // get the ProgressCircle from the spawned instance (not the prefab asset)
            progressCircle = spawnedFire.GetComponent<ProgressCircle>();
            if (progressCircle != null)
                progressCircle.SetProgress(0f);

            // set the fire particles to the instantiated fire particles child
            fireParticles = spawnedFire.transform.GetChild(0).GetComponent<ParticleSystem>();

            //fireParticles = spawnedFire.GetComponent<ParticleSystem>();
            Debug.Log("Fire particles: "+fireParticles);
        }

        // if the b button is pressed, add a fire alarm game object to the position of the hand
        if (OVRInput.GetDown(OVRInput.Button.Two))
        {
            
            Vector3 spawnPos = transform.position;

            // make the rotation of the fire alarm the same as the hand
            Quaternion spawnRot = transform.rotation;

            // If we previously spawned an alarm, destroy that instance first (safe runtime destroy).
            if (currentFireAlarmInstance != null)
            {
                Destroy(currentFireAlarmInstance);
                currentFireAlarmInstance = null;
            }

            // Instantiate a fresh fire alarm after cleaning up the previous spawned instance
            if (fireAlarm != null)
            {
                currentFireAlarmInstance = Instantiate(fireAlarm, spawnPos, spawnRot);
                await Task.Delay(1000);
                currentFireAlarmInstance.GetComponent<BoxCollider>().enabled = true; //enable collider after 5 seconds so it doesn't interfere with hand spawning the alarm
            }
        }

        // if the left thumbstick is pressed, play the ai narration audio or the space bar is pressed
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick) || Input.GetKeyDown(KeyCode.D))
        {

            //play the ai narration only once
            if(!aiNarrationStarted){
                Debug.Log("Playing AI Narration");
            
                // make the trainer panel ui disappear
                trainerPanelUI.SetActive(false);

                StartAINarration(0);
                aiNarrationStarted = true;
            }
            
            
        }

        // if the fire particles are alive, increment the time since fire start
        if (currentFireInstance != null && fireParticles.IsAlive() && startTimer == true)
        {
            timeSinceFireStart += Time.deltaTime;
            timeText.text = timeSinceFireStart.ToString("F1");

        }

        // check if the water particles are colliding with the fire particles for more than 3 seconds
        if (currentWaterParticles && fireParticles)
        {
            
            if (currentWaterParticles.IsAlive() && fireParticles.IsAlive())
            {
                // Calculate distance and angle between water and fire
                Vector3 toFire = fireParticles.transform.position - currentWaterParticles.transform.position;
                float distance = toFire.magnitude;
                float angle = Vector3.Angle(currentWaterParticles.transform.forward, toFire);
                
                // check if water is aimed at fire (within distance and angle threshold)
                if (distance < 10.0f && angle < 45f)
                {
                    Debug.Log("3.Water is aimed at fire - distance: " + distance + ", angle: " + angle);
                    timeToExtinguish -= Time.deltaTime;
                    if (progressCircle != null)
                        progressCircle.SetProgress((initialTimeToExtinguish - timeToExtinguish) / initialTimeToExtinguish);
                    serverConfigStatusText.text = "Time to extinguish: " + timeToExtinguish;
                    serverConfigStatusText.text += "\nTime since fire start: " + timeSinceFireStart;
                    serverConfigStatusText.text += "\nWater used: " + amtWaterUsed;

                    Debug.Log("Time to extinguish: " + timeToExtinguish);

                    
                    
                    // if the time to extinguish is less than or equal to 0, stop the fire particles and get the final score
                    if (timeToExtinguish <= 0)
                    {

                        Debug.Log("4.Fire extinguished!");
                        fireParticles.Stop();

                        Destroy(currentFireInstance);

                        finishAINarration();

                        currentPlayerName = saveUserName.currentPlayerName;

                        // higher is better, max 1000
                        float timeScore = CalculateThresholdScore(
                            timeSinceFireStart,
                            perfectTimeTargetSeconds,
                            zeroTimeScoreSeconds
                        );

                        float waterScore = CalculateThresholdScore(
                            amtWaterUsed,
                            perfectWaterTargetLitres,
                            zeroWaterScoreLitres
                        );

                        float weightedScore = Mathf.Clamp01((timeScore * timeWeight) + (waterScore * waterWeight));
                        float finalScore = weightedScore * 1000f;

                        finalScoreText.text = finalScore.ToString("F1");

                        scoreManager.getFinalScore(currentPlayerName, true, amtWaterUsed, timeSinceFireStart, finalScore);

                        


                        
                        // Load and display player data on UI after saving
                        if (displayPlayerDataUI != null)
                        {
                            string playerId = AuthenticationService.Instance.PlayerId;
                            _ = displayPlayerDataUI.LoadAndDisplayPlayerData(currentPlayerName, playerId);
                        }
                        else
                        {
                            Debug.LogWarning("DisplayPlayerDataUI component not found in scene!");
                            serverConfigStatusText.text += "\nError: Can't find displayPlayerDataUI";
                        }
                        
                        // shw this device id on the screen
                        serverConfigStatusText.text += "\nDevice ID: " + SystemInfo.deviceUniqueIdentifier;
                        
                    }

                }
                else
                {
                    timeToExtinguish = initialTimeToExtinguish;
                    if (progressCircle != null)
                        progressCircle.SetProgress(0f); // timeToExtinguish reset to initial value → progress back to 0
                    Debug.Log("Time to extinguish: " + timeToExtinguish);
                }
            }
        }
    }

    public void StartAINarration(int index)
    {
        StartCoroutine(PlayAINarration(index));
    }

    private IEnumerator PlayAINarration(int i)
    {
        
        {
            if(aiNarrationAudio.isPlaying)
                {
                    aiNarrationAudio.Stop();
                }
            aiNarrationAudio.PlayOneShot(aiNarrationClips[i]);
            yield return new WaitWhile(() => aiNarrationAudio.isPlaying);
            
            if(i == aiNarrationClips.Length - 1)
            {
                // now allow the hand controls to be unlocked so fire can be extinguished
                handControlsLocked = false;
                startTimer = true; // start the timer for timeSinceFireStart once the narration is done
            }
            
        }
      
    }


    public void finishAINarration()
    {
        endAINarrationAudio.Play();
        uiManager.finishSlide();
        
    }

    private async void StartServer()
    {
        try
        {
            tcpListener = new TcpListener(IPAddress.Any, 41196);
            tcpListener.Start();
            isServerRunning = true;
            Debug.Log("Server started.");
            serverConfigStatusText.text += "\nServer started!!";

            while (isServerRunning)
            {
                TcpClient client = await tcpListener.AcceptTcpClientAsync();
                _ = HandleClientAsync(client);  // Fire and forget
            }
        }
        catch (SocketException)
        {
            if (isServerRunning)
            {
                Debug.LogWarning("Server stopped due to socket error.");
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected when stopping the listener during scene reload/quit.
        }
    }

    private void StopServer()
    {
        if (!isServerRunning)
        {
            return;
        }

        isServerRunning = false;

        if (tcpListener != null)
        {
            tcpListener.Stop();
            tcpListener = null;
        }

        Debug.Log("Server stopped.");
        serverConfigStatusText.text += "server stopped";
    }

    private async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[2048];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;  // Client disconnected

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("Received: " + message);
                serverConfigStatusText.text = "\nReceived: " + message;

                if (int.TryParse(message, out int value))
                {
                    if (value < 50000)
                    {
                            currentWaterParticles.Play();
                            Debug.Log("Particles started.");
                            serverConfigStatusText.text += "\nParticles started.";      
                    }
                    else
                    {
                            currentWaterParticles.Stop();
                            Debug.Log("Particles stopped.");
                            serverConfigStatusText.text += "\nParticles stopped.";
                    }
                }
                else
                {
                    Debug.LogError("Received invalid number: " + message);
                    serverConfigStatusText.text += "\nReceived invalid number: " + message;
                }

                byte[] response = Encoding.UTF8.GetBytes("Echo: " + message);
                await stream.WriteAsync(response, 0, response.Length);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception: " + ex.Message);
            serverConfigStatusText.text += "Exception: " + ex.Message;
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    private void OnDestroy()
    {
        StopServer();

    }
}
