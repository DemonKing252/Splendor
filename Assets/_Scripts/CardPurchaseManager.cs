using Unity.Netcode;
using UnityEngine;

public class CardPurchaseManager : NetworkManager
{    
    public CardBehaviour activeCard = null;
    private static CardPurchaseManager instance;
    public static CardPurchaseManager Instance => instance;
    void Awake()
    {
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
