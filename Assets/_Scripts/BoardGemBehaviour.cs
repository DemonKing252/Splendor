using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class BoardTokenBehaviour : NetworkBehaviour
{
    public delegate void OnTokenClicked(GemStoneType type);
    public event OnTokenClicked onTokenClicked;

    public GemStoneType gemStoneType;

    void Start()
    {
        if (!IsServer)
            GetComponent<Button>().onClick.AddListener(() => { onTokenClicked?.Invoke(gemStoneType); });     
    }
}
