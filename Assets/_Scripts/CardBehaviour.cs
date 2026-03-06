using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Unity.Netcode;

[System.Serializable]
public enum GemStoneType
{
    Diamond,
    Ruby,
    Saphire,
    Onyx,
    Emerald,
    WildCard,
    Count
}

public class CardBehaviour : MonoBehaviour
{
    [SerializeField] private Button buttonBehaviour;
    [SerializeField] private TMP_Text presteigeText;
    [SerializeField] private Image cardSprite;
    [SerializeField] private GemStoneType gemStoneType;
    [SerializeField] private int presteigeCount = 0;
    [SerializeField] private int diamondCount = 0;
    [SerializeField] private int rubyCount = 0;
    [SerializeField] private int saphireCount = 0;
    [SerializeField] private int onyxCount = 0;
    [SerializeField] private int emeraldCount = 0;

    public GemStoneType GemStoneType => gemStoneType;
    public int PresteigeCount => presteigeCount;
    public int DiamondCount => diamondCount;
    public int RubyCount => rubyCount;
    public int SaphireCount => saphireCount;
    public int OnyxCount => onyxCount;
    public int EmeraldCount => emeraldCount;
    public ulong CardIndex = 0;

    public GameObject[] gems;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        buttonBehaviour = GetComponent<Button>();
        buttonBehaviour.onClick.AddListener(() =>
        {
            ConfirmationMessageBehaviour.Instance.ShowConfirmMsg(TurnAction.BuyCard, this.gameObject);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetCard(GemStoneType gemStoneType, int presteigeCount, 
    int diamondCount, int rubyCount, int saphireCount, int onyxCount, int emeraldCount)
    {
        this.gemStoneType = gemStoneType;
        int gemStoneIndex = (int)gemStoneType;
        cardSprite.sprite = GameInstance.Instance.GemSprites[gemStoneIndex];

        this.presteigeCount = presteigeCount;
        presteigeText.text = presteigeCount != 0 ? presteigeCount.ToString() : string.Empty;

        this.diamondCount = diamondCount;
        this.rubyCount = rubyCount;
        this.saphireCount = saphireCount;
        this.onyxCount = onyxCount;
        this.emeraldCount = emeraldCount;

        int[] _gemCounts =
        {
            diamondCount,
            onyxCount,
            rubyCount,
            emeraldCount,
            saphireCount
        };
        /*
            diamondCount,
            rubyCount,
            saphireCount,
            onyxCount,
            emeraldCount
        */

        int index = 0;
        foreach(int value in _gemCounts)
        {
            gems[index].GetComponentInChildren<TMP_Text>().text = "x" + value.ToString();
            gems[index].SetActive(value > 0 ? true : false);
            index++;
        }

    }
}
