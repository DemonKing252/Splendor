using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public class ServerManager : NetworkBehaviour
{
    public List<Tuple<ulong, string>> clientIDs = new List<Tuple<ulong, string>>();
    public NetworkVariable<ulong> playerTurn = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int playerTurnIndex = 0;
    public bool IsMyTurn
        => NetworkManager.Singleton.LocalClientId == playerTurn.Value ? true : false;
        
    
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

            // Make up for the timing issue.   
            //OnClientConnected(NetworkManager.Singleton.LocalClientId);
            
            StartCoroutine(WaitUntilNextFrame());            
        }
        //Debug.Log("Client/Server/Host: " + IsClient + " " + IsServer + " " + IsHost);

        // If the app runs as a Host but not a server *explicitly*
        if (!IsExplicitServer)
        {       
            playerTurn.OnValueChanged += UpdatePlayerTurnStatus;
            UpdatePlayerTurnStatus(0, 0);
        }        

    }

    private IEnumerator WaitUntilNextFrame()
    {
        // Wait one frame so all NetworkObjects finish spawning
        yield return null;

        //if (IsClient)
        //    OnClientConnected(NetworkManager.Singleton.LocalClientId);
        foreach(var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            OnClientConnected(kvp.Key);
        }            
        UpdatePlayerTurnStatus(0, 0);
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NextTurnServerRpc()
    {
        try
        {           
            // Next players turn.
            playerTurnIndex = (playerTurnIndex + 1) % clientIDs.Count;
            playerTurn.Value = clientIDs[playerTurnIndex].Item1;
            Debug.Log("Player: " + playerTurn.Value + " turn.");
        }
        catch(Exception e)
        {
            Debug.Log("Exception on Server: " + e.Message);
        }
    }
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RescrambleCardServerRpc(ulong cardIndex, CardType type)
    {
        // Scramble card at Network Object ID (cardIndex)
        CardManager.Instance.ScrambleCard(cardIndex, type);

        // Sync cards across network
        //CardManager.Instance.SyncBoardClientRpc(CardManager.Instance.networkCards);
    }


    public void UpdatePlayerTurnStatus(ulong oldValue, ulong newValue)
    {

        // This client's IP address matches the IP address of who's turn it is:
        if (NetworkManager.Singleton.LocalClientId == playerTurn.Value)
            CardManager.Instance.playerTurnText.text = "It's your turn!";
        else
        {
            // TODO: Eventually support usernames.
            try {
                var player = clientIDs.Where(c => c.Item1 == playerTurn.Value).First();
                CardManager.Instance.playerTurnText.text = "It's player's " + player.Item2 + " turn!";
            }
            catch (Exception e) {
                playerTurn = null;
            }
            
        } 

    }

    // Called on Server for every client that connects.
    public void OnClientConnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.

        Debug.Log("Client joined the server at ID: " + clientID);
        
        clientIDs.Add(new Tuple<ulong, string>(clientID, Utility.userName));
        CardManager.Instance.SetupBoardClientRpc();
        UpdatePlayerTurnStatus(0, 0);

    }
    // Called on Server for every client that disconnects.
    public void OnClientDisconnected(ulong clientID)
    {
        var delete_me = clientIDs.Where(c => c.Item1 == clientID).First();
        // Only the Host/Client can spawn the player.
        clientIDs.Remove(delete_me);
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
