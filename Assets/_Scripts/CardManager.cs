using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.InputSystem;

public struct NetworkToken : INetworkSerializable, IEquatable<NetworkToken>
{
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
    public CardType cardType;
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
        emeraldCount == other.emeraldCount &&
        cardType == other.cardType;

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
        serializer.SerializeValue(ref cardType);
    }
}

public class CardManager : NetworkBehaviour
{
    [SerializeField] private Transform cardBoardTransform;
    [SerializeField] private Transform boardTokenTableTransform;

      
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
    
    public NetworkList<NetworkCard> devNetworkCards = new NetworkList<NetworkCard>();
    public CardBehaviour[] cardGOs;

    [SerializeField] private Transform reserveCardTransform;
    private CardBehaviour[] reserveGOs;
    [SerializeField] private TMP_Text reservedCardStatusText;

    [SerializeField] private Transform nobleTransform;
    private CardBehaviour[] nobleGOs;
    public NetworkList<NetworkCard> nobleNetworkCards = new NetworkList<NetworkCard>();

    public TMP_Text prestigePointsText;
    private int prestigePoints = 0;
    public int PrestigePoints { 
        get => prestigePoints; 
        set { 
            prestigePoints = value; 
            prestigePointsText.text = "Prestige Points: " + value.ToString() + "/15";
        }
    }


    public int[] TokensInHand;

    void Awake()
    {
        instance = this;
    }
    

    public void CollectTokens()
    {
        for (int idx = 0; idx < inventoryTokens.Length; idx++)
        {
            inventoryUI.tokenTexts[idx].text = "x" + inventoryTokens[idx].TokenCount.ToString();
        }

        
        for(int i = 0; i < TokensInHand.Length; i++)
            TokensInHand[i] = 0;

        ServerManager.Instance.NextTurnServerRpc();
    }
    public Tuple<bool, int> ReserveFull()
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
        return new Tuple<bool, int>(reserveFull, reserveIndex);
    }

    public void ReserveCard(GameObject go)
    {
        // Cannot reserve Noble cards and cards that are already reserved.
        if (go.GetComponent<CardBehaviour>().CardType != CardType.Development)
        {
            DialogueManager.Instance.SetWarningText("You can't reserve a card more then once!", Color.yellow);
            return;
        }

        
        int reserveIndex = ReserveFull().Item2;
        if (!ReserveFull().Item1)
        {
            CardBehaviour card = go.GetComponent<CardBehaviour>();
            int CardIndex = (int)card.CardIndex;
            NetworkCard networkCard = devNetworkCards[CardIndex];

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

            ServerManager.Instance.RescrambleCardServerRpc(networkCard.CardIndex, card.CardType);

            if (networkBoardTokens[(int)GemStoneType.WildCard].TokenCount > 0)
            {
                int wildTokenCount = networkBoardTokens[(int)GemStoneType.WildCard].TokenCount - 1;
                inventoryTokens[(int)GemStoneType.WildCard].TokenCount++; 
                inventoryUI.tokenTexts[(int)GemStoneType.WildCard].text = "x" + inventoryTokens[(int)GemStoneType.WildCard].TokenCount;
            
                UpdateBoardTokenNetworkServerRpc((int)GemStoneType.WildCard, wildTokenCount);
            }
            
            ServerManager.Instance.NextTurnServerRpc();

            reservedCardStatusText.gameObject.SetActive(false);
        }
        else
        {
            DialogueManager.Instance.SetWarningText("You can't reserve more then 3 cards!", Color.yellow);
        } 
    }
    /*
        Diamond,
        Ruby,
        Saphire,
        Onyx,
        Emerald,
    */
    public bool CurrentyMet(CardBehaviour card)
    {
                
        if (card.CardType != CardType.Noble)
        {
            int[] costs = new int[5]
            {
                card.DiamondCount,
                card.RubyCount,
                card.SaphireCount,
                card.OnyxCount,
                card.EmeraldCount
            };
            int wildCardCount = inventoryTokens[(int)GemStoneType.WildCard].TokenCount;

            for(int idx = 0; idx < 5; idx++)
            {
                if (inventoryTokens[idx].TokenCount + permaDiscountTokens[idx].TokenCount < costs[idx])
                {
                    wildCardCount -= costs[idx] - (inventoryTokens[idx].TokenCount + permaDiscountTokens[idx].TokenCount);
                }
            }

            return wildCardCount >= 0 || ((inventoryTokens[(int)GemStoneType.Diamond].TokenCount + permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount >= card.DiamondCount ) &&
                (inventoryTokens[(int)GemStoneType.Ruby].TokenCount + permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount >= card.RubyCount    ) &&
                (inventoryTokens[(int)GemStoneType.Saphire].TokenCount + permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount >= card.SaphireCount ) &&
                (inventoryTokens[(int)GemStoneType.Onyx].TokenCount + permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount >= card.OnyxCount    ) &&
                (inventoryTokens[(int)GemStoneType.Emerald].TokenCount + permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount >= card.EmeraldCount ));
        }
        else
        {
            return (permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount >= card.DiamondCount ) &&
                (permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount    >= card.RubyCount    ) &&
                (permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount >= card.SaphireCount ) &&
                (permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount    >= card.OnyxCount    ) &&
                (permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount >= card.EmeraldCount );
        }
            
    }
    public void PurchaseCard(GameObject go)
    {
        CardBehaviour card = go.GetComponent<CardBehaviour>();
            
        if (!CurrentyMet(card))
        {
            if (card.CardType == CardType.Development)
                DialogueManager.Instance.SetWarningText("Cannot afford this development card!", Color.yellow);

            return;
        }
        else
        {
            if (card.CardType != CardType.Noble)
            {
                int[] costs = new int[5]
                {
                    card.DiamondCount,
                    card.RubyCount,
                    card.SaphireCount,
                    card.OnyxCount,
                    card.EmeraldCount
                };
                int[] wildCardsUsed = new int[5] {0, 0, 0, 0, 0};

                for(int idx = 0; idx < 5; idx++)
                {
                    if (inventoryTokens[idx].TokenCount + permaDiscountTokens[idx].TokenCount < costs[idx])
                    {
                        wildCardsUsed[idx] += costs[idx] - (inventoryTokens[idx].TokenCount + permaDiscountTokens[idx].TokenCount);
                    }
                }

                int diamondDeduct = Mathf.Max(card.DiamondCount - permaDiscountTokens[(int)GemStoneType.Diamond].TokenCount - wildCardsUsed[0], 0);
                int rubyDeduct    = Mathf.Max(card.RubyCount - permaDiscountTokens[(int)GemStoneType.Ruby].TokenCount - wildCardsUsed[1], 0);
                int saphireDeduct = Mathf.Max(card.SaphireCount - permaDiscountTokens[(int)GemStoneType.Saphire].TokenCount - wildCardsUsed[2], 0);
                int onyxDeduct    = Mathf.Max(card.OnyxCount - permaDiscountTokens[(int)GemStoneType.Onyx].TokenCount - wildCardsUsed[3], 0);
                int emeraldDeduct = Mathf.Max(card.EmeraldCount - permaDiscountTokens[(int)GemStoneType.Emerald].TokenCount - wildCardsUsed[4], 0);

                inventoryTokens[(int)GemStoneType.Diamond].TokenCount -= diamondDeduct;
                inventoryTokens[(int)GemStoneType.Ruby].TokenCount    -= rubyDeduct;
                inventoryTokens[(int)GemStoneType.Saphire].TokenCount -= saphireDeduct;
                inventoryTokens[(int)GemStoneType.Onyx].TokenCount    -= onyxDeduct;
                inventoryTokens[(int)GemStoneType.Emerald].TokenCount -= emeraldDeduct;
                inventoryTokens[(int)GemStoneType.WildCard].TokenCount -= wildCardsUsed.Sum();

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
                   new NetworkToken{TokenCount=wildCardsUsed.Sum()},
                };
                ReplenishBoardTokenStackServerRpc(networkTokens);
            }
            
            PrestigePoints += card.PresteigeCount;
            
            ServerManager.Instance.NextTurnServerRpc();

            if (card.CardType == CardType.Development || card.CardType == CardType.Noble)
            {
                // Replace the card on the network
                ServerManager.Instance.RescrambleCardServerRpc(card.CardIndex, card.CardType);
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
    public void PlaceTokensBack()
    {
        for(int idx = 0; idx < TokensInHand.Length - 1; idx++)
        {
            int tokenCount = networkBoardTokens[idx].TokenCount + TokensInHand[idx];
            UpdateBoardTokenNetworkServerRpc(idx, tokenCount);
            inventoryTokens[idx].TokenCount -= TokensInHand[idx];
            TokensInHand[idx] = 0;
        }
    }

    public void OnTokenClicked(GemStoneType type)
    {
        if (!ServerManager.Instance.IsMyTurn) //  || TokenUIManager.Instance.TurnAction != TurnAction.Hidden
        {
            DialogueManager.Instance.SetWarningText("Wait for your turn!", Color.yellow);
            return;
        }
            
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
        int reservedTokenSlots = 0;
        for(int idx = 0; idx < TokensInHand.Length; idx++)
        {
            if (TokensInHand[idx] > 0)
                reservedTokenSlots++;
        }

        
        if (maxStack > 0)
        {
            TokenUIManager.Instance.ShowUI(TurnAction.Collect_Token);
            if (maxStack > 1 || reservedTokenSlots > 2)
            {
                TokenUIManager.Instance.ShowTokenCollectorConfirmButton(true);
            }
            else
            {                
                TokenUIManager.Instance.ShowTokenCollectorConfirmButton(false);
            }
        }
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

    public void ScrambleCard(ulong cardIndex, CardType type)
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

        if (type == CardType.Noble)
            prestige = 3;
        
        GemStoneType randGemType = (GemStoneType)UnityEngine.Random.Range(0, 5); 
        /*
        diamondCount,
        rubyCount,
        saphireCount,
        onyxCount,
        emeraldCount
        */
        NetworkCard card = type == CardType.Development ? devNetworkCards[(int)cardIndex] : nobleNetworkCards[(int)cardIndex];

        card.presteigeCount = prestige;
        card.diamondCount = gemCosts[0];
        card.rubyCount = gemCosts[1];
        card.saphireCount = gemCosts[2];
        card.onyxCount = gemCosts[3];
        card.emeraldCount = gemCosts[4]; 
        card.gemStoneType = randGemType;

        if (type == CardType.Development)
            devNetworkCards[(int)cardIndex] = card;
        else if (type == CardType.Noble)
            nobleNetworkCards[(int)cardIndex] = card;
        
        for(int idx = 0; idx < gemCosts.Length; idx++)
            gemCosts[idx] = 0;

            
        //Debug.Log("Scrambled card: " + card.diamondCount);
    }

    public void ScrambleBoard(int devCount, int nobleCount)
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

        

        for (int idx = 0; idx < devCount; idx++)
        {
            var card = devNetworkCards[idx];
            card.CardIndex = (ulong)idx;
            card.cardType = CardType.Development;

            devNetworkCards[idx] = card;
            //Debug.Log("Srambled card: " + devNetworkCards[idx].gemStoneType); 

            ScrambleCard((ulong)idx, CardType.Development);
        }

        for (int idx = 0; idx < nobleCount; idx++)
        {
            var card = devNetworkCards[idx];
            card.CardIndex = (ulong)idx;
            card.cardType = CardType.Noble;
            devNetworkCards[idx] = card;
                 
            ScrambleCard((ulong)idx, CardType.Noble);
        }        

    }
    public void OnDevNetworkCardChanged(NetworkListEvent<NetworkCard> change)
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
    public void OnNobleNetworkCardChanged(NetworkListEvent<NetworkCard> change)
    {
        NetworkCard card = change.Value;
        int cardIndex = change.Index;
    
        nobleGOs[cardIndex].SetCard(card.gemStoneType, 
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
        Debug.Log("Setting up Board on client side...");
        for(int i = 0; i < devNetworkCards.Count; i++)
        {
            NetworkCard card = devNetworkCards[i];

            cardGOs[i].SetCard(card.gemStoneType, 
                card.presteigeCount,
                card.diamondCount,
                card.rubyCount,
                card.saphireCount,
                card.onyxCount,
                card.emeraldCount
            );
            cardGOs[i].SetCardType(CardType.Development);

        }
        for(int i = 0; i < nobleNetworkCards.Count; i++)
        {
            NetworkCard card = nobleNetworkCards[i];

            nobleGOs[i].SetCard(card.gemStoneType, 
                card.presteigeCount,
                card.diamondCount,
                card.rubyCount,
                card.saphireCount,
                card.onyxCount,
                card.emeraldCount
            );
            nobleGOs[i].SetCardType(CardType.Noble);

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
                devNetworkCards.Add(new NetworkCard());
            }

            for (int i = 0; i < nobleTransform.childCount; i++)
            {
                nobleNetworkCards.Add(new NetworkCard());
            }

            ScrambleBoard(cardCount, nobleTransform.childCount);
        }
        // Client/Host leave the board blank until the server authorizes the scramble (check ServerRpc)
        if (IsHost || IsClient)
        {  
            PrestigePoints = 0;
            devNetworkCards.OnListChanged += OnDevNetworkCardChanged;
            nobleNetworkCards.OnListChanged += OnNobleNetworkCardChanged;
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

            nobleGOs = new CardBehaviour[nobleTransform.childCount];
            for (int idx = 0; idx < nobleTransform.childCount; idx++)
            {
                nobleGOs[idx] = nobleTransform.GetChild(idx).GetComponent<CardBehaviour>();
                nobleGOs[idx].CardIndex = (ulong)idx;
                
            }



            Debug.Log("Starting Client/Host");
        }
    }
    // --- Helper Functions ---


    // Update is called once per frame
    void Update()
    {
        
    }
}
