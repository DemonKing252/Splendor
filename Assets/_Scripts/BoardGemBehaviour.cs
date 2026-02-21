using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class BoardGemBehaviour : MonoBehaviour
{
    public delegate void OnTokenClicked(GemStoneType type);
    public event OnTokenClicked onTokenClicked;

    public GemStoneType gemStoneType;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() => { onTokenClicked?.Invoke(gemStoneType); });
    }
}
