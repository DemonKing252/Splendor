using System.Data.SqlTypes;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UIElements;

public class RelayManager : MonoBehaviour
{
    private static RelayManager instance;
    public static RelayManager Instance => instance;
    void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    async void Start()
    {
        await UnityServices.InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
            return;

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(15);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            RelayServerData relayServer = AllocationUtils.ToRelayServerData(alloc, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServer);


            NetworkManager.Singleton.StartHost();
            
            Debug.Log("Join Code: " + joinCode);
        }
        catch(RelayServiceException e)
        {
            Debug.Log("ERROR when creating relay: " + e.Message);
        }
    }
    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation alloc = await RelayService.Instance.JoinAllocationAsync(joinCode);            
            
            RelayServerData relayServer = AllocationUtils.ToRelayServerData(alloc, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServer);

            NetworkManager.Singleton.StartClient();

            Debug.Log("Join Code: " + joinCode);
        }
        catch(RelayServiceException e)
        {
            Debug.Log("ERROR when joining relay: " + e.Message);
            MainMenu.Instance.SetMenuStatus("Unknown host!", Color.yellow);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
