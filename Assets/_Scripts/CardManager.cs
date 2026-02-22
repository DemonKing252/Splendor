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

public struct TokenTable : INetworkSerializable
{
    public BoardTokenBehaviour[] tokens;
    public int[] TokenCounts;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        for (int i = 0; i < TokenCounts.Length; i++)
        {
            serializer.SerializeValue(ref TokenCounts[i]);
        }
    }

}
[System.Serializable]
public class TokenUITable
{
    public TMP_Text[] tokenTexts;
}

public struct NetworkCard : INetworkSerializable
{
    public ulong NetworkID;
    public CardBehaviour cardGO;
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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref gemStoneType);
        serializer.SerializeValue(ref presteigeCount);
        serializer.SerializeValue(ref diamondCount);
        serializer.SerializeValue(ref rubyCount);
        serializer.SerializeValue(ref saphireCount);
        serializer.SerializeValue(ref onyxCount);
        serializer.SerializeValue(ref emeraldCount);
        serializer.SerializeValue(ref NetworkID);
    }
}




public class CardManager : NetworkBehaviour
{
    public NetworkCard[] networkCards;
    [SerializeField] private Transform cardBoardTransform;
    [SerializeField] private Transform boardTokenTableTransform;

    public TokenTable boardTokenTable;
    public TokenTable playerTokenTable;

    public TokenUITable boardUI;
    //public TokenUITable inventoryUI;
    private static CardManager instance;
    public static CardManager Instance => instance;
    public int[] TokensInHand;

    void Awake()
    {
        instance = this;
    }


    public void OnTokenClicked(GemStoneType type)
    {
        Debug.Log("Updating token...");
        // TODO: Fix this bug.
        int tokenCount = boardTokenTable.TokenCounts[(int)type];
        
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


        tokenCount--;
        boardTokenTable.TokenCounts[(int)type] = tokenCount;
        
        int[] tokens = new int[6]
        {
            boardTokenTable.TokenCounts[0],            
            boardTokenTable.TokenCounts[1],
            boardTokenTable.TokenCounts[2],
            boardTokenTable.TokenCounts[3],
            boardTokenTable.TokenCounts[4],
            boardTokenTable.TokenCounts[5]
        };

        UpdateBoardTokenNetworkClientRpc(tokens);

        //boardUI.tokenTexts[(int)type].text = "x" + tokenCount.ToString();
    }

    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    public void UpdateBoardTokenNetworkClientRpc(int[] tokenCounts)
    {
        for (int idx = 0; idx < tokenCounts.Length; idx++)
        {
            boardTokenTable.TokenCounts[idx] = tokenCounts[idx];
            boardUI.tokenTexts[idx].text = "x" + tokenCounts[idx].ToString();
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

        int prestige = 0;
        
        /*
            diamondCount,
            rubyCount,
            saphireCount,
            onyxCount,
            emeraldCount
        */

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

        for (int i = 0; i < cardCount; i++)
        {
            
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

            Debug.Log(i + " - Gem Stone Values: " + IntArrayToList(gemCosts));
            
            GemStoneType randGemType = (GemStoneType)UnityEngine.Random.Range(0, 5); 

            /*
            diamondCount,
            rubyCount,
            saphireCount,
            onyxCount,
            emeraldCount
            */
            networkCards[i].presteigeCount = prestige;
            networkCards[i].diamondCount = gemCosts[0];
            networkCards[i].rubyCount = gemCosts[1];
            networkCards[i].saphireCount = gemCosts[2];
            networkCards[i].onyxCount = gemCosts[3];
            networkCards[i].emeraldCount = gemCosts[4]; 

            //networkCards[i].cardGO.SetCard(randGemType, prestige, gemCosts[0], gemCosts[1], gemCosts[2], gemCosts[3], gemCosts[4]);
            
            for(int idx = 0; idx < gemCosts.Length; idx++)
                gemCosts[idx] = 0;
        }
    }

    //[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    
    [Rpc(SendTo.ClientsAndHost, InvokePermission = RpcInvokePermission.Everyone)]
    public void SyncBoardClientRpc(NetworkCard[] netCards, int TokenStock)
    {
        Debug.Log("Setting up board [Client = " + (IsClient || IsHost).ToString() + "]");
        for(int i = 0; i < netCards.Length; i++)
        {
            NetworkCard card = netCards[i];

            //Debug.Log("Card Net ID: " + networkCards[i].cardGO.GetComponent<NetworkObject>().NetworkObjectId);
            ulong netid = networkCards[i].cardGO.GetComponent<NetworkObject>().NetworkObjectId;

            Debug.Log("Net ID: " + netid + ", P: " + card.presteigeCount + " " + card.diamondCount);

            networkCards[i].cardGO.SetCard(card.gemStoneType, 
                card.presteigeCount, 
                card.diamondCount, 
                card.rubyCount, 
                card.saphireCount, 
                card.onyxCount, 
                card.emeraldCount
            );

            //networkCards[i].NetworkID = networkCards[i].cardGO.GetComponent<NetworkObject>().NetworkObjectId;
        }
        for (int idx = 0; idx < boardTokenTable.TokenCounts.Length; idx++)
        {
            boardTokenTable.TokenCounts[idx] = TokenStock;
        }
        for (int idx = 0; idx < boardTokenTable.TokenCounts.Length; idx++)
        {
            boardUI.tokenTexts[idx].text = "x" + TokenStock.ToString();
        }
    }
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnNetworkSpawn()
    {
        int cardCount = cardBoardTransform.childCount;
        networkCards = new NetworkCard[12];

        if (cardCount != 12)
        {
            Debug.LogError("ERROR: Card count is not supposed to be: " + cardCount);
        }

        // Server sets up the board
        
        // Server Sets up Board -> Client recieves the board map & sets up network Ids -> S
        boardTokenTable.TokenCounts = new int[(int)GemStoneType.Count] { 4, 4, 4, 4, 4, 4 };
        boardTokenTable.tokens = new BoardTokenBehaviour[12];
        TokensInHand = new int[6]{0,0,0,0,0,0};

        if (IsServer)
        {
            ScrambleBoard(cardCount);
            Debug.Log("Starting Server...");            
        }
        // Client/Host leave the board blank until the server authorizes the scramble (check ServerRpc)
        if (IsHost || IsClient)
        {
            
            for(int i = 0; i < cardCount; i++)
            {
                networkCards[i].cardGO = cardBoardTransform.GetChild(i).GetComponent<CardBehaviour>();
            }

            for (int i = 0; i < boardTokenTableTransform.childCount; i++)
            {                
                boardTokenTable.tokens[i] = boardTokenTableTransform.GetChild(i).GetComponent<BoardTokenBehaviour>();
            }

            for(int i = 0; i < boardTokenTable.TokenCounts.Length; i++)
            {
                boardTokenTable.tokens[i].onTokenClicked += OnTokenClicked;
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
