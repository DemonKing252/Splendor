using System;
using UnityEngine;

public class CardManager : MonoBehaviour
{
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

        //TODO: Improve randomness:
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
        
        for (int i = 0; i < cardCount; i++)
        {
            int randomPrest = UnityEngine.Random.Range(0, 3);

            int randDiamond;
            int randRuby;
            int randSaphire;
            int randOnyx;
            int randEmerald;

            if (randomPrest == 0)
            {
                int min_range = 0;
                int max_range = 2;

                randDiamond = UnityEngine.Random.Range(min_range, max_range);
                randRuby = UnityEngine.Random.Range(min_range, max_range);
                randSaphire = UnityEngine.Random.Range(min_range, max_range);
                randOnyx = UnityEngine.Random.Range(min_range, max_range);
                randEmerald = UnityEngine.Random.Range(min_range, max_range);    
            }
            else if (randomPrest == 1)
            {                
                int min_range = 0;
                int max_range = 3;

                randDiamond = UnityEngine.Random.Range(min_range, max_range);
                randRuby = UnityEngine.Random.Range(min_range, max_range);
                randSaphire = UnityEngine.Random.Range(min_range, max_range);
                randOnyx = UnityEngine.Random.Range(min_range, max_range);
                randEmerald = UnityEngine.Random.Range(min_range, max_range);    
            }
            else
            {                
                int min_range = 0;
                int max_range = 4;

                randDiamond = UnityEngine.Random.Range(min_range, max_range);
                randRuby = UnityEngine.Random.Range(min_range, max_range);
                randSaphire = UnityEngine.Random.Range(min_range, max_range);
                randOnyx = UnityEngine.Random.Range(min_range, max_range);
                randEmerald = UnityEngine.Random.Range(min_range, max_range);    
            }

            GemStoneType randGemType = (GemStoneType)UnityEngine.Random.Range(0, (int)GemStoneType.Count); 

            cards[i].SetCard(randGemType, randomPrest, randDiamond, randRuby, randSaphire, randOnyx, randEmerald);
        }
        
        
    }
    // --- Helper Functions ---


    // Update is called once per frame
    void Update()
    {
        
    }
}
