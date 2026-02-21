using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public struct TokenTable : INetworkSerializable
{
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
    public BoardGemBehaviour[] boardGemBehaviours;
}


public class CardManager : MonoBehaviour
{
    public TokenTable boardTable;
    public TokenTable playerTable;

    public TokenUITable boardUI;
    public TokenUITable inventoryUI;
    private CardManager instance;
    public CardManager Instance => instance;
    public int[] TokensInHand;

    void Awake()
    {
        instance = this;
    }
    public void OnTokenClicked(GemStoneType type)
    {
        // TODO: Fix this bug.
        int tokenCount = boardTable.TokenCounts[(int)type];
        
        TokensInHand[(int)type]++;

        bool safe = true;
        int maxStack = 0;
        for(int i = 0; i < TokensInHand.Length; i++)
        {
            if (TokensInHand[i] > maxStack)
                maxStack = TokensInHand[i];
        }
        if (TokensInHand.Sum() > 3 || maxStack > 2 || (maxStack > 1 && TokensInHand.Sum() > 2))
            safe = false;

        if (tokenCount < 1 || !safe)
        {            
            TokensInHand[(int)type]--;
            return;
        }

        tokenCount--;
        boardTable.TokenCounts[(int)type] = tokenCount;

        boardUI.tokenTexts[(int)type].text = "x" + tokenCount.ToString();
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

            cards[i].SetCard(randGemType, prestige, gemCosts[0], gemCosts[1], gemCosts[2], gemCosts[3], gemCosts[4]);
            
            for(int idx = 0; idx < gemCosts.Length; idx++)
                gemCosts[idx] = 0;
        }
    }

    // TODO: RescrambleBoard at Index I
    public void RescrambleBoard()
    {
        
    }


    [SerializeField]
    private Transform cardBoardTransform;

    public CardBehaviour[] cards;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int cardCount = cardBoardTransform.childCount;
        cards = new CardBehaviour[cardCount];

        if (cardCount != 12)
        {
            Debug.LogError("ERROR: Card count is not supposed to be: " + cardCount);
        }

        for(int i = 0; i < cardCount; i++)
        {
            cards[i] = cardBoardTransform.GetChild(i).GetComponent<CardBehaviour>();
        }

        for(int i = 0; i < boardUI.boardGemBehaviours.Length; i++)
        {
            boardUI.boardGemBehaviours[i].onTokenClicked += OnTokenClicked;
        }

        ScrambleBoard(cardCount);

        boardTable.TokenCounts = new int[(int)GemStoneType.Count] { 4, 4, 4, 4, 4, 4 };
        TokensInHand = new int[6]{0,0,0,0,0,0};
    }
    // --- Helper Functions ---


    // Update is called once per frame
    void Update()
    {
        
    }
}
