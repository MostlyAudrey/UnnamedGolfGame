﻿using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;

[RequireComponent(typeof(NetworkManager))]
public class MainGameController : MonoBehaviour
{
    NetworkManager NETWORK_MANAGER;
    //Hash table with keys being <NetworkIdentity, Networked Entity >
    public Hashtable players_hash = new Hashtable();
    public GameObject local_player;
    public string name;
    public Color color;
    public string last_scene;
    public bool p_clicked = false;
    public bool show_debug = false;

    private string path;
    private string myLog;
    Queue myLogQueue = new Queue();

    // Start is called before the first frame update
    void Start()
    {
        path = Application.dataPath + "/Log.txt";
        File.WriteAllText(path, "");
        NETWORK_MANAGER = GetComponent<NetworkManager>();
        last_scene = NetworkManager.networkSceneName;
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkServer.active || NetworkClient.active)
        {
            if (players_hash.Count != NETWORK_MANAGER.numPlayers)
            {
                players_hash.Clear();
                foreach (GameObject networked_entity in GameObject.FindGameObjectsWithTag("Client"))
                {
                    players_hash.Add(networked_entity.GetComponent<NetworkIdentity>(), networked_entity);
                    if (networked_entity.GetComponent<NetworkIdentity>().hasAuthority)
                    {
                        local_player = networked_entity;
                        set_name_and_color(name, color);
                    }
                };
            }
            if (local_player == false)
                refresh_player_list();

            if (last_scene != NetworkManager.networkSceneName)
            {
                last_scene = NetworkManager.networkSceneName;
            }

            if (p_clicked)
            {
                if (Input.GetKeyUp(KeyCode.P))
                {
                    p_clicked = false;
                    show_debug = !show_debug;
                }
            }
            else if (Input.GetKeyDown(KeyCode.P))
                p_clicked = true;
        }
    }

    public void refresh_player_list()
    {
        players_hash.Clear();
        foreach (GameObject networked_entity in GameObject.FindGameObjectsWithTag("Client"))
        {
            players_hash.Add(networked_entity.GetComponent<NetworkIdentity>(), networked_entity);
            if (networked_entity.GetComponent<NetworkIdentity>().hasAuthority)
            {
                local_player = networked_entity;
                set_name_and_color(name, color);
            }
        };
    }

    public void ServerChangeScene(string scene)
    {
        this.GetComponent<NetworkManager>().ServerChangeScene(scene);
    }

    public ushort get_port() { return this.GetComponent<TelepathyTransport>().port; }

    public void set_name_and_color(string name, Color color)
    {
        if (name != "")
        {
            this.name = name;
            this.color = color;
        }
        if (local_player)
            local_player.GetComponent<PlayerScript>().Cmd_set_name_and_color(name, color);
    }
    public void start_host() { NETWORK_MANAGER.StartHost(); }
    public void stop_host() { NETWORK_MANAGER.StopHost(); }
    public void start_client(string address) { NETWORK_MANAGER.StartClient(); NETWORK_MANAGER.networkAddress = address; }
    public void stop_client() { NETWORK_MANAGER.StopClient(); }

    
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        myLog = logString;
        string newString = "\n [" + type + "] : " + myLog;
        myLogQueue.Enqueue(newString);
        if (type == LogType.Exception)
        {
            newString = "\n" + stackTrace;
            myLogQueue.Enqueue(newString);
        }
        myLog = string.Empty;
        foreach (string mylog in myLogQueue)
        {
            myLog += mylog;
        }
        if (myLogQueue.Count > 50)
        {
            File.AppendAllText(path, myLog);
            myLogQueue.Clear();
        }

    }

    void OnGUI() { if (show_debug) GUILayout.Label(myLog); else GUILayout.Label("");  }

    void OnApplicationQuit()
    {
        string path = Application.dataPath + "/Log.txt";
        if (!File.Exists(path))
        {
            File.WriteAllText(path, myLog);
        }
    }

}
