using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public class ServerManager : NetworkBehaviour
{
    public List<ulong> clientIDs = new List<ulong>();
    public NetworkVariable<ulong> playerTurn = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int playerTurnIndex = 0;
    public bool IsMyTurn => NetworkManager.Singleton.LocalClientId == playerTurn.Value ? true : false;
    
    // Returns true when the application runs as a server, NOT as a Host.
    public bool IsExplicitServer => IsServer && !IsHost;

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
    // On Server Startup
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // start with player 0.
            playerTurn.Value = 0;
            playerTurnIndex = 0;

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        // If the app runs as a Host but not a server *explicitly*
        if (!IsExplicitServer)
        {       
            playerTurn.OnValueChanged += UpdatePlayerTurnStatus;
            UpdatePlayerTurnStatus(0, 0);
        }        

    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NextTurnServerRpc()
    {
        try
        {           
            // Next players turn.
            playerTurnIndex = (playerTurnIndex + 1) % clientIDs.Count;
            playerTurn.Value = clientIDs[playerTurnIndex];
            Debug.Log("Player: " + playerTurn.Value + " turn.");
        }
        catch(Exception e)
        {
            Debug.Log("Exception on Server: " + e.Message);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RescrambleCardServerRpc(ulong cardIndex)
    {
        // Scramble card at Network Object ID (cardIndex)
        CardManager.Instance.ScrambleCard(ref CardManager.Instance.networkCards[(int)cardIndex]);

        // Sync cards across network
        CardManager.Instance.SyncBoardClientRpc(CardManager.Instance.networkCards);
    }


    public void UpdatePlayerTurnStatus(ulong oldValue, ulong newValue)
    {
        Debug.Log("Updating text: " + newValue);

        // This client's IP address matches the IP address of who's turn it is:
        if (NetworkManager.Singleton.LocalClientId == playerTurn.Value)
            CardManager.Instance.playerTurnText.text = "It's your turn!";
        else
            // TODO: Eventually support usernames.
            CardManager.Instance.playerTurnText.text = "It's player's " + NetworkManager.Singleton.LocalClientId + " turn!"; 

    }

    // Called on Server for every client that connects.
    void OnClientConnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.

        Debug.Log("Client joined the server at ID: " + clientID);
        
        clientIDs.Add(clientID);
        CardManager.Instance.SetupBoardClientRpc(CardManager.Instance.networkCards, 4);
        UpdatePlayerTurnStatus(0, 0);

    }
    // Called on Server for every client that disconnects.
    void OnClientDisconnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.
        clientIDs.Remove(clientID);
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
