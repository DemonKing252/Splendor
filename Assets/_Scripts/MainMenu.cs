using UnityEngine;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Runtime.InteropServices;
using static Unity.Netcode.Transports.UTP.UnityTransport;

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

    [SerializeField] private Toggle localHostToggle;


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
    private IEnumerator TryConnect()
    {
        float t = 0f;
        SetMenuStatus("Trying to connect...", Color.white);
        while (t < 3f) // Wait 3 seconds for a connection then time out.
        {
            if (connectionSuccess)
                yield break;
            yield return null;
            t += Time.deltaTime;
        }
        SetMenuStatus("Unknown Host!", Color.yellow);
        NetworkManager.Singleton.Shutdown();
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

        startServerBtn.onClick.AddListener(() => {
            if (ValidateIP(ipAddress.text) && ValidatePort(portNo.text)) 
                NetworkManager.Singleton.StartServer();
        });

        hostGameBtn.onClick.AddListener(() => {
            if (!localHostToggle.isOn)
                RelayManager.Instance.CreateRelay();
            else
            {                
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 5491);
                NetworkManager.Singleton.StartHost();
            }
        });

        startMatchMakingBtn.onClick.AddListener(() => { 
            if (!localHostToggle.isOn)
            {
                if (relayID.text == "")
                {
                    SetMenuStatus("You cannot have a blank Relay ID!", Color.yellow);
                    relayID.text = "LHGGCJ";
                }
                
                RelayManager.Instance.JoinRelay(relayID.text);                
            }
            else
            {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 5491);
                NetworkManager.Singleton.StartClient();
                StartCoroutine(TryConnect());
            }
        });
        userName.onValueChanged.AddListener((string user) => Utility.userName = userName.text);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
