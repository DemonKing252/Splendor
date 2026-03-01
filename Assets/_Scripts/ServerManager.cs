using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public class ServerManager : NetworkBehaviour
{
    public List<ulong> clientIDs = new List<ulong>();
    public NetworkVariable<ulong> playerTurn = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int playerIndexTurn = 0;
    public bool IsMyTurn => NetworkManager.Singleton.LocalClientId == playerTurn.Value ? true : false;

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
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            // start with player 0.
            playerTurn.Value = 0;
            playerIndexTurn = 0;
        }
        else if (IsClient || IsHost)
        {
            Debug.Log("Initializing client [Owner | Current Turn]: " + NetworkManager.Singleton.LocalClientId + " " + playerTurn.Value);
            playerTurn.OnValueChanged += UpdatePlayerTurnStatus;
            UpdatePlayerTurnStatus(0, 0);
        }

    }
    public void NextPlayerTurn()
    {
        
    }
    public void UpdatePlayerTurnStatus(ulong oldValue, ulong newValue)
    {
        if (NetworkManager.Singleton.LocalClientId == playerTurn.Value)
            CardManager.Instance.playerTurnText.text = "It's your turn!";
        else
            // TODO: Eventually support usernames.
            CardManager.Instance.playerTurnText.text = "It's players: " + NetworkManager.Singleton.LocalClientId + " turn!"; 

    }

    // Called on Server for every client that connects.
    void OnClientConnected(ulong clientID)
    {
        // Only the Host/Client can spawn the player.

        Debug.Log("Client joined the server at ID: " + clientID);
        
        clientIDs.Add(clientID);
        CardManager.Instance.SyncBoardClientRpc(CardManager.Instance.networkCards, 4);
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
