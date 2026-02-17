using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public class ServerManager : NetworkBehaviour
{

    private static ServerManager instance;
    public static ServerManager Instance
    {
        get { return instance; }
        set { instance = value; }
    }
    void Awake()
    {
        instance = this;
    }

    void OnNetworkStart()
    {
        if (NetworkManager.Singleton.IsServer)
        {            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

    }

    void OnClientConnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.
        if (!NetworkManager.Singleton.IsServer) 
            return;

        Debug.Log("Client joined the server at ID: " + clientID);

    }
    void OnClientDisconnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.
        if (!NetworkManager.Singleton.IsServer) 
            return;

        Debug.Log("Client disconnected the server at ID: " + clientID);
    }


    void Update()
    {
        // Start Client
        if (Input.GetKeyUp(KeyCode.C))
        {
            NetworkManager.Singleton.StartClient();
        }
        // Start Server
        if (Input.GetKeyUp(KeyCode.P))
        {            
            NetworkManager.Singleton.StartServer();
        }
        // Start Host
        if (Input.GetKeyUp(KeyCode.H))
        {            
            NetworkManager.Singleton.StartHost();
        }

    }
}
