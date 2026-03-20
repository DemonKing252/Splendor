using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct NetworkClient : INetworkSerializable, IEquatable<NetworkClient>
{
    public ulong ClientId;
    public FixedString32Bytes UserName;

    public bool Equals(NetworkClient other)
    {
        return ClientId == other.ClientId &&
        UserName == other.UserName;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref UserName);
    }
}


public class ServerManager : NetworkBehaviour
{
    public NetworkList<NetworkClient> clients = new NetworkList<NetworkClient>();
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
            
            StartCoroutine(WaitUntilNextFrame());
        }
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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

        foreach(var kvp in NetworkManager.Singleton.ConnectedClients)
            OnClientConnected(kvp.Key);

        UpdatePlayerTurnStatus(0, 0);
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NextTurnServerRpc()
    {
        NextTurn();
    }
    private void NextTurn()
    {
        try
        {           
            // Next players turn.
            playerTurnIndex = (playerTurnIndex + 1) % clients.Count;
            playerTurn.Value = clients[playerTurnIndex].ClientId;
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
    }


    public void UpdatePlayerTurnStatus(ulong oldValue, ulong newValue)
    {

        // This client's IP address matches the IP address of who's turn it is:
        if (NetworkManager.Singleton.LocalClientId == newValue)
            CardManager.Instance.playerTurnText.text = "It's your turn!";
        else
        {
            string username = null;
            foreach(var c in clients)
            {
                if (c.ClientId == newValue)
                {
                    username = c.UserName.ToSafeString();
                    break;
                }
            }
            CardManager.Instance.playerTurnText.text = "It's player's " + username + " turn!";
        } 
    }

    // Called on Server for every client that connects.
    public void OnClientConnected(ulong clientID)
    {
        Debug.Log("Client joined the server at ID: " + clientID);
        clients.Add(new NetworkClient{ClientId=clientID,UserName=""});
        CardManager.Instance.SetupBoardClientRpc();
        
        UpdatePlayerTurnStatus(0, 0);
        RequestUserNameClientRpc(ToClient(clientID));
    }

    public ClientRpcParams ToClient(ulong clientID)
    {
        return new ClientRpcParams{Send=new ClientRpcSendParams{TargetClientIds=new ulong[] { clientID}}};
    }
    
    [ClientRpc(RequireOwnership=false)]
    public void RequestUserNameClientRpc(ClientRpcParams rpcParams = default)
    {
        SendUsernameServerRpc(NetworkManager.Singleton.LocalClientId, Utility.userName);        
    }
    
    [ServerRpc(RequireOwnership=false)]
    public void SendUsernameServerRpc(ulong clientId, string username)
    {
        for(int i = 0; i < clients.Count; i++)
        {
            if (clients[i].ClientId == clientId)
            {                
                NetworkClient client = clients[i];
                client.UserName = username.ToSafeString();
                clients[i] = client;

            }
        }
    }

    // Called on Server for every client that disconnects.
    public void OnClientDisconnected(ulong clientID)
    {

        bool isLocalDisconnect = clientID == NetworkManager.Singleton.LocalClientId;
        bool isShuttingDown = !NetworkManager.Singleton.IsListening;
        // [Host] -> { [Client][Server] }
        // [Remote Client] -> {[Client]}
        // If the this is the HOST-SERVER instance not the HOST-CLIENT instance, don't touch the variables.

        if (IsServer && !(isLocalDisconnect || isShuttingDown))
        {
            
            Debug.Log("Client disconnected the server at ID: " + clientID);
            // Only the Host/Client can spawn the player.
            for(int i = 0; i < clients.Count; i++)
            {
                if (clients[i].ClientId == clientID)
                {
                    // Force a next turn.
                    if (clientID == playerTurn.Value)
                    {
                        NextTurn();
                    }
                    clients.RemoveAt(i);     
                    break;                               
                }
            }
        }
        // If Explicit Client
        if (!IsHost && IsClient)
        {
            Debug.Log("Calling client shutdown");
            Utility.server_status_msg_color = Color.yellow;
            Utility.server_status_msg = "Lost connection to Host!";

            Debug.Log("Lost connection to host, returning to main menu");
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
        }
        

    }
}
