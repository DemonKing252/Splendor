using System;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public enum TurnAction
{
    Collect_Token,
    Buy_OR_Reserve,
    Hidden
}

public class TokenUIManager : MonoBehaviour
{
    private static TokenUIManager instance;
    public static TokenUIManager Instance => instance;

    private TurnAction turnAction = TurnAction.Hidden;
    public TurnAction TurnAction => turnAction;

    private GameObject activeUICanvas = null;
    private GameObject activeCardGO = null;

    [Header("Token Collection")]
    [SerializeField] private GameObject tokenCollectionUI;
    [SerializeField] public Button confirmBtn;
    [SerializeField] public Button closeBtn;

    [Header("Buy and Reserve")]
    [SerializeField] private GameObject buyAndReserveUI;
    [SerializeField] public Button buyBtn;
    [SerializeField] public Button reserveBtn;
    [SerializeField] public Button cancelBtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        confirmBtn.onClick.AddListener(() => {
            CardManager.Instance.CollectTokens();
            HideConfirmUI();
        });

        // TODO: Support refunding the player in the future
        closeBtn.onClick.AddListener(() => {
            CardManager.Instance.PlaceTokensBack();
            HideConfirmUI();
        });
        buyBtn.onClick.AddListener(() =>
        {
            CardManager.Instance.PurchaseCard(activeCardGO); 
            HideConfirmUI();
        });
        reserveBtn.onClick.AddListener(() =>
        {
            CardManager.Instance.ReserveCard(activeCardGO);
            HideConfirmUI();
        });
        cancelBtn.onClick.AddListener(() => 
            HideConfirmUI() 
        );

    }

    public void ShowConfirmUI(TurnAction action, GameObject go = null)
    {
        if (!ServerManager.Instance.IsMyTurn || TurnAction != TurnAction.Hidden)
            return;

        
        switch(action)
        {
            case TurnAction.Collect_Token:
                activeUICanvas = tokenCollectionUI;
                this.activeCardGO = go;
                turnAction = action;
            break;
            case TurnAction.Buy_OR_Reserve:
                CardBehaviour card = go.GetComponent<CardBehaviour>();

                // Reserve is full and we can't afford the card
                if (card.CardType == CardType.Development && !CardManager.Instance.CurrentyMet(card) && CardManager.Instance.ReserveFull().Item1)
                    return;

                // We can't afford the Noble
                if (card.CardType == CardType.Noble && !CardManager.Instance.CurrentyMet(card))
                    return;

                // We can't afford the Reserved card
                if (card.CardType == CardType.Reserve && !CardManager.Instance.CurrentyMet(card))
                    return;

                activeUICanvas = buyAndReserveUI;
                if (card.CardType == CardType.Reserve || 
                    card.CardType == CardType.Noble) {
                    reserveBtn.gameObject.SetActive(false);
                }
                else if (card.CardType == CardType.Development)
                    reserveBtn.gameObject.SetActive(true);
                
                buyBtn.gameObject.SetActive(CardManager.Instance.CurrentyMet(card));
                this.activeCardGO = go;
                turnAction = action;
            break;
        
        }

        activeUICanvas.SetActive(true);
    }
    public void HideConfirmUI()
    {
        activeUICanvas.SetActive(false);
        activeUICanvas = null;

        tokenCollectionUI.SetActive(false);
        buyAndReserveUI.SetActive(false);

        turnAction = TurnAction.Hidden;
    }

    void Awake()
    {
        instance = this;
    }

    
}
