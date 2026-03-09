using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkToken : INetworkSerializable, IEquatable<NetworkToken>
{
    //public BoardTokenBehaviour token;
    public int TokenCount;

    public bool Equals(NetworkToken other)
    {
        return TokenCount == other.TokenCount;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TokenCount);
    }
}

public struct LocalizedToken
{
    public int TokenCount;
}


[System.Serializable]
public class TokenUITable
{
    public TMP_Text[] tokenTexts;
}

public struct NetworkCard : INetworkSerializable, IEquatable<NetworkCard>
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
        => CardIndex == other.CardIndex &&
        presteigeCount == other.presteigeCount &&
        diamondCount == other.diamondCount &&
        rubyCount == other.rubyCount &&
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

      
    //public NetworkToken[] networkBoardTokens;
    public NetworkList<NetworkToken> networkBoardTokens = new NetworkList<NetworkToken>();
    public BoardTokenBehaviour[] boardTokenBehaviours;

    public LocalizedToken[] inventoryTokens;

    public BoardTokenBehaviour[] permaDiscountTokenBehaviours;
    public LocalizedToken[] permaDiscountTokens;

    public TokenUITable boardUI;
    public TokenUITable inventoryUI;
    public TokenUITable permaDiscountUI;

    private static CardManager instance;
    public static CardManager Instance => instance;
    
    public NetworkList<NetworkCard> networkCards = new NetworkList<NetworkCard>();
    public CardBehaviour[] cardGOs;

    [SerializeField] private Transform reserveCardTransform;
    private CardBehaviour[] reserveGOs;
    [SerializeField] private TMP_Text reservedCardStatusText;


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
    public void ReserveCard(GameObject go)
    {
        bool reserveFull = true;
        int reserveIndex = 0;
        
        int counter = 0;
        foreach(var res in reserveGOs)
        {
            // If the reserve slot is not filled
            if (!res.gameObject.activeSelf)
            {
                reserveFull = false;
                reserveIndex = counter;
                break;
            }
            counter++;
        }

        if (!reserveFull)
        {
            CardBehaviour card = go.GetComponent<CardBehaviour>();
            int CardIndex = (int)card.CardIndex;
            NetworkCard networkCard = networkCards[CardIndex];

            reserveGOs[reserveIndex].SetCard(networkCard.gemStoneType,
                networkCard.presteigeCount,
                networkCard.diamondCount,
                networkCard.rubyCount,
                networkCard.saphireCount,
                networkCard.onyxCount,
                networkCard.emeraldCount,
                true
            );
            reserveGOs[reserveIndex].SetCardType(CardType.Reserve);
            reserveGOs[reserveIndex].CardIndex = (ulong)reserveIndex;

            ServerManager.Instance.RescrambleCardServerRpc(networkCard.CardIndex);

            if (networkBoardTokens[(int)GemStoneType.WildCard].TokenCount > 0)
            {
                int wildTokenCount = networkBoardTokens[(int)GemStoneType.WildCard].TokenCount - 1;
                inventoryTokens[(int)GemStoneType.WildCard].TokenCount++; 
                inventoryUI.tokenTexts[(int)GemStoneType.WildCard].text = "x" + inventoryTokens[(int)GemStoneType.WildCard].TokenCount;
            
                UpdateBoardTokenNetworkServerRpc((int)GemStoneType.WildCard, wildTokenCount);
            }
            Debug.Log("Reserving at network index: " + networkCard.CardIndex.ToString());
            
            ServerManager.Instance.NextTurnServerRpc();

            reservedCardStatusText.gameObject.SetActive(false);
        }
        else
        {
            // TODO: Prompt with UI eventually
            Debug.Log("You can't reserve more then 3 cards!");
        } 
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

        if (!currency_met)
        {
            // Eventually we'll have UI messages for this. 
            Debug.Log("Not enough currency.");
            return;
        }
        else
        {

            int diamondDeduct = Mathf.Max(card.DiamondCount - permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount, 0);
            int rubyDeduct    = Mathf.Max(card.RubyCount - permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount, 0);
            int saphireDeduct = Mathf.Max(card.SaphireCount - permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount, 0);
            int onyxDeduct    = Mathf.Max(card.OnyxCount - permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount, 0);
            int emeraldDeduct = Mathf.Max(card.EmeraldCount - permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount, 0);

            inventoryTokens[(int)GemStoneType.Diamond].TokenCount -= diamondDeduct;
            inventoryTokens[(int)GemStoneType.Ruby].TokenCount    -= rubyDeduct;
            inventoryTokens[(int)GemStoneType.Saphire].TokenCount -= saphireDeduct;
            inventoryTokens[(int)GemStoneType.Onyx].TokenCount    -= onyxDeduct;
            inventoryTokens[(int)GemStoneType.Emerald].TokenCount -= emeraldDeduct;

            for(int idx = 0; idx < inventoryUI.tokenTexts.Length; idx++)
                inventoryUI.tokenTexts[idx].text = "x" + inventoryTokens[idx].TokenCount;

            permaDiscountTokens[(int)card.GemStoneType].TokenCount++;
            permaDiscountUI.tokenTexts[(int)card.GemStoneType].text = "+" + permaDiscountTokens[(int)card.GemStoneType].TokenCount;
            
            NetworkToken[] networkTokens = 
            {
               new NetworkToken{TokenCount=diamondDeduct},
               new NetworkToken{TokenCount=rubyDeduct},
               new NetworkToken{TokenCount=saphireDeduct},
               new NetworkToken{TokenCount=onyxDeduct},
               new NetworkToken{TokenCount=emeraldDeduct},
            };
            ReplenishBoardTokenStackServerRpc(networkTokens);
            ServerManager.Instance.NextTurnServerRpc();

            if (card.CardType == CardType.Board)
            {
                // Replace the card on the network
                ServerManager.Instance.RescrambleCardServerRpc(card.CardIndex);
            }
            else
            {
                // Otherwise just disable it
                card.SetCard(GemStoneType.Diamond, 0, 0, 0, 0, 0, 0, false);
                card.SetCardType(CardType.None);

                bool oneActive = false;
                foreach(var r in reserveGOs)
                {
                    if (r.gameObject.activeSelf)
                        oneActive = true;
                }
                if (oneActive)
                    reservedCardStatusText.gameObject.SetActive(false);
                else                    
                    reservedCardStatusText.gameObject.SetActive(true);
            }
            
        }
    }

    public void OnTokenClicked(GemStoneType type)
    {
        if (!ServerManager.Instance.IsMyTurn)
            return;

        int tokenCount = networkBoardTokens[(int)type].TokenCount;
        
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

        
        if (TokensInHand.Sum() > 1 && TokenUIManager.Instance.TurnAction == TurnAction.Hidden)
            TokenUIManager.Instance.ShowConfirmUI(TurnAction.Collect_Token);
            //confirmMsg.gameObject.SetActive(true);
        
        tokenCount--;
        inventoryTokens[(int)type].TokenCount++;
        

        //networkBoardTokens[(int)type].TokenCount = tokenCount;
        
        // Client -> Server -> Clients and Hosts
        
        UpdateBoardTokenNetworkServerRpc((int)type, tokenCount);
    }
    
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateBoardTokenNetworkServerRpc(int tokenIndex, int value)
    {
        var netToken = networkBoardTokens[tokenIndex];
        netToken.TokenCount = value;
        networkBoardTokens[tokenIndex] = netToken;
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReplenishBoardTokenStackServerRpc(NetworkToken[] networkTokens)
    {
        for(int idx = 0; idx < networkTokens.Count(); idx++)
        {
            var stack = networkBoardTokens[idx];
            var deduct = networkTokens[idx];

            int tokenCount = stack.TokenCount + deduct.TokenCount;
            var newStack = new NetworkToken{TokenCount=tokenCount};

            networkBoardTokens[idx] = newStack;
        }
    }

    public void OnTokenValueChanged(NetworkListEvent<NetworkToken> change)
    {
        var index = change.Index;
        var netToken = change.Value;

        boardUI.tokenTexts[index].text = "x" + netToken.TokenCount.ToString();
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

        //float prestiegeRand = UnityEngine.Random.Range(0f, 100f);
        if (cardIndex >= 0 && cardIndex <= 3)   // 0 Prestige Point (60% chance).
        {
            prestige = 0;
            int gemPrefabCount = UnityEngine.Random.Range(1, 3); // 1 or 2
            switch(gemPrefabCount)
            {
                case 1: RandomizeGemCosts(1, 2, 3); break;
                case 2: RandomizeGemCosts(2, 1, 2); break;
            }
        }
        else if (cardIndex >= 4 && cardIndex <= 7) // 1 Prestige Point (20% chance).
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
        else // 2 Prestige Points (20% chance).
        {
            prestige = 2;
            int gemPrefabCount = UnityEngine.Random.Range(1, 5); 
            switch(gemPrefabCount)
            {
                case 1: RandomizeGemCosts(1, 6, 7); break;
                case 2: RandomizeGemCosts(2, 4, 5); break;
                case 3: RandomizeGemCosts(3, 3, 4); break;
                case 4: RandomizeGemCosts(4, 2, 3); break;
            }
        }
        
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
            var card = networkCards[idx];
            card.CardIndex = (ulong)idx;
            networkCards[idx] = card;

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
    public void SetupBoardClientRpc()
    {
        for(int i = 0; i < networkCards.Count; i++)
        {
            NetworkCard card = networkCards[i];

            cardGOs[i].SetCard(card.gemStoneType, 
                card.presteigeCount,
                card.diamondCount,
                card.rubyCount,
                card.saphireCount,
                card.onyxCount,
                card.emeraldCount
            );
            cardGOs[i].SetCardType(CardType.Board);

        }

        inventoryTokens = new LocalizedToken[6];
            
        for (int idx = 0; idx < inventoryTokens.Length; idx++)
            inventoryTokens[idx].TokenCount = 0;


        for (int idx = 0; idx < boardUI.tokenTexts.Length; idx++)
        {
            boardUI.tokenTexts[idx].text = "x" + networkBoardTokens[idx].TokenCount.ToString();
        }
        foreach(var card in reserveGOs)
            card.SetCard(GemStoneType.Diamond, 0, 0, 0, 0, 0, 0, false);
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
        //networkBoardTokens = new NetworkToken[6];
        
        
        permaDiscountTokens = new LocalizedToken[6];
        cardGOs = new CardBehaviour[cardCount];

        TokensInHand = new int[6]{0,0,0,0,0,0};

        if (IsServer || ServerManager.Instance.IsExplicitServer)
        {
            for(int i = 0; i < 6; i++)
                networkBoardTokens.Add(new NetworkToken());

            for(int idx = 0; idx < networkBoardTokens.Count; idx++)
            {
                var boardToken = networkBoardTokens[idx];
                boardToken.TokenCount = 4;
                networkBoardTokens[idx] = boardToken;
            }

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
            networkBoardTokens.OnListChanged += OnTokenValueChanged;

            for(int i = 0; i < cardCount; i++)
            {
                cardGOs[i] = cardBoardTransform.GetChild(i).GetComponent<CardBehaviour>();
                cardGOs[i].CardIndex = (ulong)i;
            }
            boardTokenBehaviours = new BoardTokenBehaviour[boardTokenTableTransform.childCount];

            for (int i = 0; i < boardTokenTableTransform.childCount; i++)             
                boardTokenBehaviours[i] = boardTokenTableTransform.GetChild(i).GetComponent<BoardTokenBehaviour>();

            for(int i = 0; i < boardTokenBehaviours.Length; i++)
                boardTokenBehaviours[i].onTokenClicked += OnTokenClicked;

            for(int i = 0; i < permaDiscountTokens.Length; i++)
                permaDiscountTokens[i].TokenCount = 0;

            
            reserveGOs = new CardBehaviour[reserveCardTransform.childCount];
            for(int idx = 0; idx < reserveCardTransform.childCount; idx++)
                reserveGOs[idx] = reserveCardTransform.GetChild(idx).GetComponent<CardBehaviour>();


            Debug.Log("Starting Client/Host");
        }
    }
    // --- Helper Functions ---


    // Update is called once per frame
    void Update()
    {
        
    }
}
