﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    #region Definitions

    [HideInInspector]
    public WebCamTexture Cam;   //Rear Camera
    [HideInInspector]
    public bool arReady;
    [HideInInspector]
    public bool GameOver;
    public Button FireButton;
    public Bullet Bullet;
    public Text CountText;
    public Text CenterText;
    public Text ButtonText;
    public Text ScoreText;
    [HideInInspector]
    public int Level;
    public int Bullets;
    public Dictionary<string, Spawner> Spawner;
    public Dictionary<System.Type, int> ActiveCounter;
    [HideInInspector]
    public bool RoundWon;

    [Range(0,6)]
    private int Health;
    private int Score;
    private TVScript TVScreen;
    private bool ExitTap;

    #endregion


    private void Awake()
    {
#if UNITY_ANDROID

        //Check if we support both devices
        //Gyroscope
        if (!SystemInfo.supportsGyroscope)
        {
            Debug.Log("This device does not have Gyroscope");
            ExitTap = true;
        }

        //Enable gyro
        Input.gyro.enabled = true;

        //Back Camera
        for (int i = 0; i < WebCamTexture.devices.Length; i++)
        {
            if (!WebCamTexture.devices[i].isFrontFacing)
            {

                Cam = new WebCamTexture(WebCamTexture.devices[i].name, Screen.width, Screen.height);
                break;
            }
        }

#endif

#if UNITY_EDITOR_WIN
        Cam = new WebCamTexture();
#endif

        //If we did not find a back camera,exit
        if (Cam == null)
        {
            Debug.Log("This device does not have a rear camera");
            ExitTap = true;
        }

        /* Creating a dictionary of all spawners by setting the name of
         * their assigned ObjectToSpawn as a key and the spawner itself
         * as the value. All spawners must be children of the game object
         * "Spawners". Also, deactivating them, to keep them from spawning
         * objects before the game is ready.*/
        Spawner = new Dictionary<string, Spawner>();
        Spawner[] spawnerParent = GameObject.Find("Spawners").GetComponentsInChildren<Spawner>();
        if (spawnerParent == null)
        {
            Debug.Log("Could not find Object \"Spawners\". All spawners must be children of \"Spawners\".");
        }
        else
        {
            foreach (Spawner spawner in spawnerParent)
            {
                string type = spawner.ObjectToSpawn.name;
                Spawner.Add(type, spawner);
            }
        }

        ActiveCounter = new Dictionary<System.Type, int>
        {
            { typeof(Bullet), 0 }   //initializing the counter for active bullets here, because the game-controller needs it for checking the "Game Over" state.
        };

        Health = 3;
        GameOver = false;
        arReady = true;
    }

    private void Start()
    {
        if (TVScreen == null) TVScreen = GameObject.Find("TVScreen").GetComponent<TVScript>();
        ButtonText.text = "";
        CenterText.text = "";
        FireButton.onClick.AddListener(OnButtonDown);
        AdvanceLevel();

        System.GC.Collect();
    }

    private void Update()
    {

        //Round Won
        if (Spawner["Podpod"].SpawnCount == Spawner["Podpod"].SpawnLimit && CountTotalActiveEnemies() == 0 && !GameOver)
        {
            RoundWon = true;
            CenterText.text = "Round Clear!";
            ClearLevel();
        }

        //Defeat
        if (ActiveCounter[typeof(Bullet)] == 0 && Bullets == 0 && CountTotalActiveEnemies() > 0 && !GameOver)
        {
            CenterText.text = "You survived " + (Level - 1) + " rounds";
            ClearLevel();
        }
    }



    private void OnButtonDown()
    {
        if (ExitTap == false)
        {
            //Fire Bullet
            if (!GameOver)
            {
                if (Bullets > 0)
                {
                    Instantiate(Bullet, Camera.main.transform.position, Camera.main.transform.rotation);
                }
            }

            //Tap To Continue
            if (GameOver)
            {
                //Defeat, tap to restart
                if (Bullets == 0 || Health == 0)
                {
                    Cam.Stop();
                    SceneManager.LoadScene("FreakyTVGame");
                }
                //Next Round tap
                else
                {
                    GameOver = false;
                    CenterText.text = "";
                    ButtonText.text = "";
                    Bullets += 6;
                    AdvanceLevel();
                }
            }
        }
        else Application.Quit();
    }

    /// <summary>
    /// Prepares for the next level.
    /// </summary>
    private void AdvanceLevel()
    {
        Level++;
        CountText.text = Bullets.ToString();
        foreach (var spawner in Spawner)
        {
            spawner.Value.SpawnCount = 0;
            spawner.Value.StartCoroutine("Spawn");

            // Spawn Patterns
            switch (spawner.Key.ToString())
            {
                case "Podpod":
                    spawner.Value.SpawnLimit = 4 * Level + 8;
                    break;
                case "Capsule":
                    spawner.Value.SpawnLimit = Level + 2;
                    break;
            }
        }
        RoundWon = false;
    }

    /// <summary>
    /// Adds points to the score and updates the GUI.
    /// </summary>
    /// <param name="points">The amount of pointts to add.</param>
    public void AddScore(int points)
    {
        Score += points;
        ScoreText.text = "Score: " + Score;
    }

    /// <summary>
    /// Calculates the total amount of currently spawned active enemies.
    /// </summary>
    /// <returns>Total count of active Enemy type objects.</returns>
    private int CountTotalActiveEnemies()
    {
        int x = 0;
        foreach (System.Type k in ActiveCounter.Keys)
        {
            if (k.IsSubclassOf(typeof(Enemy))) x += ActiveCounter[k];
        }
        return x;
    }

    /// <summary>
    /// Deactivates spawners, destroys all spawned objects.
    /// Mainly used when a round ends.
    /// </summary>
    private void ClearLevel()
    {
        GameOver = true;

        foreach (Spawner spawner in Spawner.Values)
            spawner.StopCoroutine("Spawn");

        Object[] objects = FindObjectsOfType<SpawnableObject>();
        foreach (Object o in objects)
        {
            Destroy(GameObject.Find(o.name));
        }

        ButtonText.text = "Tap to continue";
    }

    /// <summary>
    /// Player's health decreases by the designated amount.
    /// If heatlh reaches zero, the game is over. Negative value heals them.
    /// </summary>
    public void DamagePlayer(int damage)
    {
        Health -= damage;
        
        if (Health <= 0)
        {
            GameOver = true;
        }
    }
}