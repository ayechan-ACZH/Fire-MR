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

public class ExtinguishFire : MonoBehaviour
{
    CloudSaveDataManager cloudSaveDataManager;
    ScoreManager scoreManager;

    public GameObject trainerPanelUI;
    public GameObject firePrefab;

    private GameObject currentFireInstance;

    public ParticleSystem controllerWaterParticles;
    public ParticleSystem handWaterParticles;
    public ParticleSystem fireParticles;
    public GameObject fireAlarm;

    // track the spawned fire alarm instance so we can remove it safely without touching the prefab reference
    private GameObject currentFireAlarmInstance;
    public int timeToExtinguish = 600;

    public AudioSource aiNarrationAudio;
    public AudioSource endAINarrationAudio;
    // 4 ai audio clips for the game
    public AudioClip aiNarrationWelcome1;
    public AudioClip aiNarrationWelcome2;
    public AudioClip aiNarrationWelcome3;

    private bool aiNarrationStarted = false;
    public AudioSource fireExtinguishingAudio;
    ParticleSystem currentWaterParticles;
    bool soundIsPlaying;
    int amtWaterUsed = 0;
    int timeSinceFireStart = 0;

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
        await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());

        // Start the TCP server
        StartServer();
    }

    void ApplyRemoteConfig(ConfigResponse configResponse)
    {
        // Conditionally update settings, depending on the response's origin:
        switch (configResponse.requestOrigin)
        {
            case ConfigOrigin.Default:
                Debug.Log("No settings loaded this session and no local cache file exists; using default values.");
                break;
            case ConfigOrigin.Cached:
                Debug.Log("No settings loaded this session; using cached values from a previous session.");
                break;
            case ConfigOrigin.Remote:
                Debug.Log("New settings loaded this session; updated values accordingly.");
                Debug.Log("handTrackedMode: " + RemoteConfigService.Instance.appConfig.GetBool("handTrackedMode"));
                Debug.Log("currentPlayerName: " + RemoteConfigService.Instance.appConfig.GetString("currentPlayerName"));


                break;
        }

        handTrackedMode = RemoteConfigService.Instance.appConfig.GetBool("handTrackedMode", true);
        currentPlayerName = RemoteConfigService.Instance.appConfig.GetString("currentPlayerName", "Default Player");

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
    }

    // Update is called once per frame
    void Update()
    {
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
              if (!soundIsPlaying)
              {
                  fireExtinguishingAudio.Play();
                  soundIsPlaying = true;
              }

              amtWaterUsed += 1;
              currentWaterParticles.Play();

          }
          else
          {
              if (soundIsPlaying)
              {
                  fireExtinguishingAudio.Stop();
                  soundIsPlaying = false;
              }

              currentWaterParticles.Stop();
          }
        }
        // ifthe right trigger is pressed, instantiate the fire particles at the hand position
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

                StartAINarration();
                aiNarrationStarted = true;
            }
            
            
        }

        // if the fire particles are alive, increment the time since fire start
        if (fireParticles.IsAlive())
        {
            timeSinceFireStart += 1;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("Stopping stopping fire particles via keyboard S key.");
            // Stop emission and clear any existing particles so the visual fire disappears immediately
            if (fireParticles != null)
            {
                fireParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                Debug.Log("fireParticles StopEmittingAndClear called. isPlaying=" + fireParticles.isPlaying + ", isAlive=" + fireParticles.IsAlive());
            }

            Destroy(currentFireInstance);
        }

        // check if the water particles are colliding with the fire particles for more than 3 seconds
        if (currentWaterParticles && fireParticles)
        {
            Debug.Log("1.Checking for collision between water and fire particles.");
            if (currentWaterParticles.IsAlive() && fireParticles.IsAlive())
            {
                // get the bounds of the water particles
                Bounds waterBounds = currentWaterParticles.GetComponent<Renderer>().bounds;

                // get the bounds of the fire particles
                Bounds fireBounds = fireParticles.GetComponent<Renderer>().bounds;
                Debug.Log("2.Fire is alive");
                // check if the water particles are colliding with the fire particles
                if (waterBounds.Intersects(fireBounds))
                {
                    Debug.Log("3.Water particles are colliding with fire particles.");
                    timeToExtinguish -= 1;
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

                        scoreManager.getFinalScore(currentPlayerName, true, 600 + timeToExtinguish, amtWaterUsed, timeSinceFireStart);
                        // shw this device id on the screen
                        serverConfigStatusText.text += "\nDevice ID: " + SystemInfo.deviceUniqueIdentifier;
                        
                    }

                }
                else
                {
                    timeToExtinguish = 600;
                    Debug.Log("Time to extinguish: " + timeToExtinguish);
                }
            }
        }
    }

    public void StartAINarration()
    {
        StartCoroutine(PlayAINarration());
    }

    private IEnumerator PlayAINarration()
    {
        // wait 5 seconds before starting the ai narration (so headset can be put on)
        yield return new WaitForSeconds(8);

        // Play the first audio clip
        aiNarrationAudio.PlayOneShot(aiNarrationWelcome1);
        yield return new WaitWhile(() => aiNarrationAudio.isPlaying);

        // Play the second audio clip
        aiNarrationAudio.PlayOneShot(aiNarrationWelcome2);
        yield return new WaitWhile(() => aiNarrationAudio.isPlaying);

        // now allow the hand controls to be unlocked so fire can be extinguished
        handControlsLocked = false;

        // Play the third audio clip
        aiNarrationAudio.PlayOneShot(aiNarrationWelcome3);
        yield return new WaitWhile(() => aiNarrationAudio.isPlaying);
    }

    public void finishAINarration()
    {
        endAINarrationAudio.Play();
    }

    private async void StartServer()
    {
        tcpListener = new TcpListener(IPAddress.Any, 41196);
        tcpListener.Start();
        Debug.Log("Server started.");
        serverConfigStatusText.text += "\nServer started!!";


        while (true)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            _ = HandleClientAsync(client);  // Fire and forget
        }
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
                        // todo: try uncommenting these
                        //if (!currentWaterParticles.isPlaying)
                        //{
                            currentWaterParticles.Play();
                            Debug.Log("Particles started.");
                            serverConfigStatusText.text += "\nParticles started.";
                        //}
                    }
                    else
                    {
                        //if (currentWaterParticles.isPlaying)
                        //{
                            currentWaterParticles.Stop();
                            Debug.Log("Particles stopped.");
                            serverConfigStatusText.text += "\nParticles stopped.";
                        //}
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
        tcpListener.Stop();
        Debug.Log("Server stopped.");
        serverConfigStatusText.text += "server stopped";

    }
}
