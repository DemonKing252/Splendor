using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class MainMenu : NetworkBehaviour
{
    public bool IsExplicitServer => IsServer && !IsHost;
    [SerializeField] private TMP_InputField userName;
    [SerializeField] private TMP_InputField ipAddress;
    [SerializeField] private TMP_InputField portNo;


    [SerializeField] private Button startServerBtn;
    [SerializeField] private Button hostGameBtn;
    [SerializeField] private Button startMatchMakingBtn;

    public override void OnNetworkSpawn()
    {
        if (!IsExplicitServer)
        {
            NetworkManager.SceneManager.LoadScene("Main", LoadSceneMode.Single);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress.text;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = ushort.Parse(portNo.text);
        startServerBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });
        hostGameBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        startMatchMakingBtn.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });
        ipAddress.onValueChanged.AddListener((string msg) => 
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = msg;
        });
        portNo.onValueChanged.AddListener((string msg) => 
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = ushort.Parse(msg);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
