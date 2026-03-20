using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Runtime.InteropServices;

public class MainMenu : NetworkBehaviour
{
    private static MainMenu instance;
    public static MainMenu Instance => instance;
    public bool IsExplicitServer => IsServer && !IsHost;
    [SerializeField] private TMP_InputField userName;
    [SerializeField] private TMP_InputField ipAddress;
    [SerializeField] private TMP_InputField portNo;
    [SerializeField] private TMP_InputField relayID;


    [SerializeField] private Button startServerBtn;
    [SerializeField] private Button hostGameBtn;
    [SerializeField] private Button startMatchMakingBtn;
    [SerializeField] private TMP_Text serverStatusText;
    void Awake()
    {
        instance = this;
    }

    private bool connectionSuccess = false;
    public override void OnNetworkSpawn()
    {
        if (IsClient && !IsHost)
            connectionSuccess = true;
        if (IsHost || IsServer)            
                NetworkManager.SceneManager.LoadScene("Main", LoadSceneMode.Single);        
    }
    public void SetMenuStatus(string msg, Color col)
    {        
        serverStatusText.text = msg;
        serverStatusText.color = col;
    }
    public bool ValidateIP(string msg)
    {
        if (!System.Net.IPAddress.TryParse(msg, out _))
        {
            Debug.Log("Not valid");
            SetMenuStatus("IP Address is not valid!", new Color(1f, 0.5f, 0f));
            ipAddress.text = "127.0.0.1";
            return false;
        }        
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress.text;
        return true;
    }
    public bool ValidatePort(string msg)
    {
        if (!ushort.TryParse(msg, out ushort port))
        {
            SetMenuStatus("Port Numher is not Valid!", new Color(1f, 0.5f, 0f));
            portNo.text = "25565";
            return false;
        }
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = ushort.Parse(portNo.text);
        return true;        
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        SetMenuStatus(Utility.server_status_msg, Utility.server_status_msg_color);
        Utility.userName = userName.text;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ipAddress.text;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = ushort.Parse(portNo.text);

        startServerBtn.onClick.AddListener(() => {
            if (ValidateIP(ipAddress.text) && ValidatePort(portNo.text)) 
                NetworkManager.Singleton.StartServer();
        });

        hostGameBtn.onClick.AddListener(() => RelayManager.Instance.CreateRelay() );
        startMatchMakingBtn.onClick.AddListener(() => RelayManager.Instance.JoinRelay(relayID.text) );
        userName.onValueChanged.AddListener((string user) => Utility.userName = userName.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
