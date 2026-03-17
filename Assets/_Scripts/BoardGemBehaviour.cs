using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoardTokenBehaviour : NetworkBehaviour
{
    public delegate void OnTokenClicked(GemStoneType type);
    public event OnTokenClicked onTokenClicked;

    public GemStoneType gemStoneType;

    public override void OnNetworkSpawn()
    {
        if (!ServerManager.Instance.IsExplicitServer)
            GetComponent<Button>().onClick.AddListener(() => { Debug.Log("clicked token"); onTokenClicked?.Invoke(gemStoneType); });     
    }
}
