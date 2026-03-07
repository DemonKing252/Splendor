using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NUnit.Framework.Constraints;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public struct NetworkToken : INetworkSerializable
{
    public BoardTokenBehaviour token;
    public int TokenCount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TokenCount);
    }

}
[System.Serializable]
public class TokenUITable
{
    public TMP_Text[] tokenTexts;
}

public struct NetworkCard : INetworkSerializable, System.IEquatable<NetworkCard>
{
    public ulong CardIndex;
    public GemStoneType gemStoneType;
    /*
        diamondCount,
        rubyCount,
        saphireCount,
        onyxCount,
        emeraldCount
    */
    public int presteigeCount;
    public int diamondCount;
    public int rubyCount;
    public int saphireCount;
    public int onyxCount;
    public int emeraldCount;

    public bool Equals(NetworkCard other)
        => gemStoneType == other.gemStoneType &&
           presteigeCount == other.presteigeCount &&
           diamondCount == other.diamondCount &&
           saphireCount == other.saphireCount &&
           onyxCount == other.onyxCount &&
           emeraldCount == other.emeraldCount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref gemStoneType);
        serializer.SerializeValue(ref presteigeCount);
        serializer.SerializeValue(ref diamondCount);
        serializer.SerializeValue(ref rubyCount);
        serializer.SerializeValue(ref saphireCount);
        serializer.SerializeValue(ref onyxCount);
        serializer.SerializeValue(ref emeraldCount);
        serializer.SerializeValue(ref CardIndex);
    }
}

public class CardManager : NetworkBehaviour
{
    [SerializeField] private Transform cardBoardTransform;
    [SerializeField] private Transform boardTokenTableTransform;
    [SerializeField] public TMP_Text playerTurnText;
    [SerializeField] public ConfirmationMessageBehaviour confirmMsg;
        
    public NetworkToken[] boardTokens;
    public NetworkToken[] inventoryTokens;
    public NetworkToken[] permaDiscountTokens;

    public TokenUITable boardUI;
    public TokenUITable inventoryUI;
    public TokenUITable permaDiscountUI;

    private static CardManager instance;
    public static CardManager Instance => instance;

    
    //public NetworkCard[] networkCards;
    public NetworkList<NetworkCard> networkCards = new NetworkList<NetworkCard>();
    
    public CardBehaviour[] cardGOs;

    public int[] TokensInHand;

    void Awake()
    {
        instance = this;
    }

    public void CollectTokens()
    {
        Debug.Log("Clicking the button");
        for (int idx = 0; idx < inventoryTokens.Length; idx++)
        {
            inventoryUI.tokenTexts[idx].text = "x" + inventoryTokens[idx].TokenCount.ToString();
        }

        
        for(int i = 0; i < TokensInHand.Length; i++)
            TokensInHand[i] = 0;

        ServerManager.Instance.NextTurnServerRpc();
    }
    public void PurchaseCard(GameObject go)
    {
        CardBehaviour card = go.GetComponent<CardBehaviour>();

        bool currency_met = 
            (inventoryTokens[(int)GemStoneType.Diamond].TokenCount + permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount >= card.DiamondCount ) &&
            (inventoryTokens[(int)GemStoneType.Ruby].TokenCount + permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount    >= card.RubyCount    ) &&
            (inventoryTokens[(int)GemStoneType.Saphire].TokenCount + permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount >= card.SaphireCount ) &&
            (inventoryTokens[(int)GemStoneType.Onyx].TokenCount + permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount    >= card.OnyxCount    ) &&
            (inventoryTokens[(int)GemStoneType.Emerald].TokenCount + permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount >= card.EmeraldCount );


        Debug.Log("Card Costs - Diamond: " + card.DiamondCount + " - Ruby: " + card.RubyCount + " - Saphire: " + card.SaphireCount + " - Onyx: " + card.OnyxCount + " - Emerald: " + card.EmeraldCount);
        
        Debug.Log("Inventory - Diamond: " + inventoryTokens[(int)GemStoneType.Diamond].TokenCount + " - Ruby: " + 
        inventoryTokens[(int)GemStoneType.Ruby].TokenCount + " - Saphire: " + inventoryTokens[(int)GemStoneType.Saphire].TokenCount + 
        " - Onyx: " + inventoryTokens[(int)GemStoneType.Onyx].TokenCount + " - Emerald: " + inventoryTokens[(int)GemStoneType.Emerald].TokenCount);

        if (!currency_met)
        {
            // Eventually we'll have UI messages for this.
            Debug.LogWarning("Not enough currency!");
        }
        else
        {
            Debug.Log("Curreny met, we can purchase this card");
            permaDiscountTokens[(int)card.GemStoneType].TokenCount++;

            inventoryTokens[(int)GemStoneType.Diamond].TokenCount -= Mathf.Max(card.DiamondCount - permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount, 0);
            inventoryTokens[(int)GemStoneType.Ruby].TokenCount -= Mathf.Max(card.RubyCount - permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount, 0);
            inventoryTokens[(int)GemStoneType.Saphire].TokenCount -= Mathf.Max(card.SaphireCount - permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount, 0);
            inventoryTokens[(int)GemStoneType.Onyx].TokenCount -= Mathf.Max(card.OnyxCount - permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount, 0);
            inventoryTokens[(int)GemStoneType.Emerald].TokenCount -= Mathf.Max(card.EmeraldCount - permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount, 0);

            for(int idx = 0; idx < inventoryUI.tokenTexts.Length; idx++)
                inventoryUI.tokenTexts[idx].text = "x" + inventoryTokens[idx].TokenCount;

            permaDiscountUI.tokenTexts[(int)card.GemStoneType].text = "+" + permaDiscountTokens[(int)card.GemStoneType].TokenCount;
            
            ServerManager.Instance.NextTurnServerRpc();
            ServerManager.Instance.RescrambleCardServerRpc(card.CardIndex);
        }
    }

    public void OnTokenClicked(GemStoneType type)
    {
        if (!ServerManager.Instance.IsMyTurn)
            return;

        int tokenCount = boardTokens[(int)type].TokenCount;
        
        TokensInHand[(int)type]++;

        bool safe = true;
        int maxStack = 0;
        for(int i = 0; i < TokensInHand.Length; i++)
        {
            if (TokensInHand[i] > maxStack)
                maxStack = TokensInHand[i];
        }
        // Rules for Splendor:
        // 1. Sum of tokens cannot exceed 3.
        // 2. No more then 2 tokens per stack.
        // 3. No more tokens if there's already 2 collected in any given stack.
        if (TokensInHand.Sum() > 3 || maxStack > 2 || (maxStack > 1 && TokensInHand.Sum() > 2))
            safe = false;

        if (tokenCount < 1 || !safe)
        {            
            TokensInHand[(int)type]--;
            return;
        } 

        
        if (TokensInHand.Sum() > 1 && confirmMsg.gameObject.activeSelf == false)
            ConfirmationMessageBehaviour.Instance.ShowConfirmMsg(TurnAction.CollectTokens);
            //confirmMsg.gameObject.SetActive(true);
        
        tokenCount--;
        inventoryTokens[(int)type].TokenCount++;
        

        boardTokens[(int)type].TokenCount = tokenCount;
        
        // Client -> Server -> Clients and Hosts
        UpdateBoardTokenNetworkServerRpc(boardTokens);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateBoardTokenNetworkServerRpc(NetworkToken[] netTokens)
    {
        UpdateBoardTokenNetworkClientRpc(netTokens);
    } 

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateBoardTokenNetworkClientRpc(NetworkToken[] netTokens)
    {
        for (int idx = 0; idx < netTokens.Length; idx++)
        {
            //Debug.Log("Updating Token count to be: " + netTokens[idx].TokenCount.ToString());
            boardTokens[idx].TokenCount = netTokens[idx].TokenCount;
            boardUI.tokenTexts[idx].text = "x" + netTokens[idx].TokenCount.ToString();
        }
    }


    public int[] RandomUniqueIndexes(int count, int maxInclusive)
    {
        List<int> values = new List<int>();

        int[] indexes = new int[count];
        indexes[0] = UnityEngine.Random.Range(0, maxInclusive+1);
        values.Add(indexes[0]);

        for(int i = 1; i < count; i++)
        {
            do
            {
                indexes[i] = UnityEngine.Random.Range(0, maxInclusive+1);
            } while (values.Contains(indexes[i]));
            values.Add(indexes[i]);
        }

        return indexes;
    }

    public string IntArrayToList(int[] arr)
    {
        string str = null;
        str += "{";
        foreach(int value in arr)
        {
            str += value + ", ";
        }
        str += "}";
        return str;
    }

    public void ScrambleCard(ulong cardIndex)
    {
        int prestige = 0;

        int[] gemCosts = new int[5]
        {
            0, 0, 0, 0, 0
        };
        
        Action<int, int, int> RandomizeGemCosts = (count, minInclusive, maxInclusive) =>
        {
            int[] randIndexes = RandomUniqueIndexes(count, 4);
            
            for(int idx = 0; idx < count; idx++)
                gemCosts[randIndexes[idx]] = UnityEngine.Random.Range(minInclusive, maxInclusive + 1);
        };

        float prestiegeRand = UnityEngine.Random.Range(0f, 100f);
        if (prestiegeRand <= 40f)   // 0 Prestige Point.
        {
            prestige = 0;
            int gemPrefabCount = UnityEngine.Random.Range(1, 3); // 1 or 2
            switch(gemPrefabCount)
            {
                case 1: RandomizeGemCosts(1, 2, 3); break;
                case 2: RandomizeGemCosts(2, 1, 2); break;
            }
        }
        else if (prestiegeRand <= 70f) // 1 Prestige Point.
        {
            prestige = 1;
            int gemPrefabCount = UnityEngine.Random.Range(1, 4); // 1/2/3
        
            switch(gemPrefabCount)
            {
                case 1: RandomizeGemCosts(1, 3, 4); break;
                case 2: RandomizeGemCosts(2, 2, 3); break;
                case 3: RandomizeGemCosts(3, 1, 2); break;
            }
        }
        else // 2 Prestige Points.
        {
            prestige = 2;
            int gemPrefabCount = UnityEngine.Random.Range(1, 5); // 1/2/3/4
            switch(gemPrefabCount)
            {
                case 1: RandomizeGemCosts(1, 6, 7); break;
                case 2: RandomizeGemCosts(2, 4, 5); break;
                case 3: RandomizeGemCosts(3, 3, 4); break;
                case 4: RandomizeGemCosts(4, 2, 3); break;
            }
        }
        //Debug.Log(i + " - Gem Stone Values: " + IntArrayToList(gemCosts));
        
        GemStoneType randGemType = (GemStoneType)UnityEngine.Random.Range(0, 5); 
        /*
        diamondCount,
        rubyCount,
        saphireCount,
        onyxCount,
        emeraldCount
        */
        NetworkCard netCard = networkCards[(int)cardIndex];

        netCard.presteigeCount = prestige;
        netCard.diamondCount = gemCosts[0];
        netCard.rubyCount = gemCosts[1];
        netCard.saphireCount = gemCosts[2];
        netCard.onyxCount = gemCosts[3];
        netCard.emeraldCount = gemCosts[4]; 
        netCard.gemStoneType = randGemType;

        networkCards[(int)cardIndex] = netCard;
        
        for(int idx = 0; idx < gemCosts.Length; idx++)
            gemCosts[idx] = 0;
    }

    public void ScrambleBoard(int cardCount)
    {
        /*
            ** All Inclusive Numbers **

            Prestige = 40% for 0, 30% for 1, 30% for 2
            Within Prest 0: Another random number between 1 and 2:
            If 1 -> one gem type costing 2-3
            If 2 -> two gem types costing 1-2

            Within Prestige 1: Another random number between 1 and 3:
            If 1 -> one gem type costing 3-4 
            If 2 -> two gem types costing 2-3
            If 3 -> three gem types costing 1-2

            Within Prestige 2: Another random number between 1 and 3:
            If 1 -> one gem type costing 6-7 
            If 2 -> two gem types costing 4-5
            If 3 -> two gem types costing 3-4 
            If 4 -> four gem types costing 2-3
        */

        

        for (int idx = 0; idx < cardCount; idx++)
        {
            //networkCards.Value[idx].CardIndex = (ulong)idx;
            
            ScrambleCard((ulong)idx);
        }

    }
    public void OnNetworkCardChanged(NetworkListEvent<NetworkCard> change)
    {    
        NetworkCard card = change.Value;
        int cardIndex = change.Index;
    
        cardGOs[cardIndex].SetCard(card.gemStoneType, 
            card.presteigeCount, 
            card.diamondCount, 
            card.rubyCount, 
            card.saphireCount, 
            card.onyxCount, 
            card.emeraldCount
        );
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    public void SetupBoardClientRpc(int TokenStock)
    {
        for(int i = 0; i < networkCards.Count; i++)
        {
            NetworkCard card = networkCards[i];
            Debug.Log("Netork Card - type: " + card.gemStoneType + " - " + card.presteigeCount + " - " + card.diamondCount);

            cardGOs[i].SetCard(card.gemStoneType, 
                card.presteigeCount, 
                card.diamondCount, 
                card.rubyCount, 
                card.saphireCount, 
                card.onyxCount, 
                card.emeraldCount
            );

            networkCards[i] = card;
        }
        for (int idx = 0; idx < boardTokens.Length; idx++)
            boardTokens[idx].TokenCount = TokenStock;

        inventoryTokens = new NetworkToken[6];
            
        for (int idx = 0; idx < boardTokens.Length; idx++)
            inventoryTokens[idx].TokenCount = 0;


        for (int idx = 0; idx < boardUI.tokenTexts.Length; idx++)
            boardUI.tokenTexts[idx].text = "x" + TokenStock.ToString();
    }
    void Start()
    {
        if (!ServerManager.Instance.IsExplicitServer)
        {
            ConfirmationMessageBehaviour.Instance.onCollectTokens += CollectTokens;            
            ConfirmationMessageBehaviour.Instance.onPurchaseCard += PurchaseCard;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {

        int cardCount = cardBoardTransform.childCount;

        if (cardCount != 12)
        {
            Debug.LogError("ERROR: Card count is not supposed to be: " + cardCount);
        }

        // Server sets up the board
        
        // Server Sets up Board -> Client recieves the board map & sets up network Ids -> S
        boardTokens = new NetworkToken[6];
        permaDiscountTokens = new NetworkToken[6];
        cardGOs = new CardBehaviour[cardCount];

        for(int idx = 0; idx < boardTokens.Length; idx++)
        {
            boardTokens[idx].TokenCount = 0;
        }

        TokensInHand = new int[6]{0,0,0,0,0,0};

        if (IsServer)
        {
            for (int i = 0; i < cardCount; i++)
            {
                networkCards.Add(new NetworkCard());
            }

            ScrambleBoard(cardCount);
            Debug.Log("Starting Server...");            
        }
        // Client/Host leave the board blank until the server authorizes the scramble (check ServerRpc)
        if (IsHost || IsClient)
        {    
            networkCards.OnListChanged += OnNetworkCardChanged;

            for(int i = 0; i < cardCount; i++)
            {
                cardGOs[i] = cardBoardTransform.GetChild(i).GetComponent<CardBehaviour>();
                cardGOs[i].CardIndex = (ulong)i;
            }

            for (int i = 0; i < boardTokenTableTransform.childCount; i++)             
                boardTokens[i].token = boardTokenTableTransform.GetChild(i).GetComponent<BoardTokenBehaviour>();

            for(int i = 0; i < boardTokens.Length; i++)
                boardTokens[i].token.onTokenClicked += OnTokenClicked;

            for(int i = 0; i < permaDiscountTokens.Length; i++)
                permaDiscountTokens[i].TokenCount = 0;


            Debug.Log("Starting Client/Host");
        }
    }
    // --- Helper Functions ---


    // Update is called once per frame
    void Update()
    {
        
    }
}
