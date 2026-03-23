using System;
using System.Collections;
using System.Linq;
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
    [SerializeField] private GameObject activeCardGO = null;

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
    public void ShowTokenCollectorConfirmButton(bool show)
    {
        confirmBtn.gameObject.SetActive(show);
    }

    public void ShowUI(TurnAction action, GameObject go = null)
    {
        Debug.Log("Got here 0");
        if (!ServerManager.Instance.IsMyTurn)
        {
            DialogueManager.Instance.SetWarningText("Wait for your turn!", Color.yellow);
        }
        Debug.Log("Got here 1");
        // Don't show the UI if is already active, this can cause unexpected problems when collecting tokens while buying a card at the same time.
        if (activeUICanvas != null)
            if (activeUICanvas.activeSelf && CardManager.Instance.TokensInHand.Sum() > 0)
                return;
        Debug.Log("Got here 2");

        // No exceptions to this rule if its not our turn, we cannot click the card
        if (!ServerManager.Instance.IsMyTurn) // If we select a different GO, select it.
            return;
        Debug.Log("Got here 3");

        if (TurnAction != TurnAction.Hidden && go == activeCardGO) // If we select a different GO, select it.
            return;
        Debug.Log("Got here 4");
        
        switch(action)
        {
            case TurnAction.Collect_Token:
                activeUICanvas = tokenCollectionUI;
                this.activeCardGO = go;
                turnAction = action;
                activeUICanvas.SetActive(true);
            break;
            case TurnAction.Buy_OR_Reserve:
                CardBehaviour card = go.GetComponent<CardBehaviour>();
                if (go != null)
                    Debug.Log("Setting active card to: " + go.name);

                // Reserve is full and we can't afford the card
                if (card.CardType == CardType.Development && !CardManager.Instance.CurrentyMet(card) && CardManager.Instance.ReserveFull().Item1)
                {
                    DialogueManager.Instance.SetWarningText("Cannot reserve more then 3 cards!", Color.yellow);
                    return;
                }

                // We can't afford the Noble
                if (card.CardType == CardType.Noble && !CardManager.Instance.CurrentyMet(card))
                {
                    DialogueManager.Instance.SetWarningText("Cannot afford this noble card!", Color.yellow);
                    return;
                }

                // We can't afford the Reserved card
                if (card.CardType == CardType.Reserve && !CardManager.Instance.CurrentyMet(card))
                {
                    DialogueManager.Instance.SetWarningText("Cannot afford this reserve card!", Color.yellow);
                    return;
                }

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
                activeUICanvas.SetActive(true);
            break;
        
        }
    }
    public void HideConfirmUI()
    {
        if (activeUICanvas != null)
        {            
            activeUICanvas.SetActive(false);
            activeUICanvas = null;
        }

        tokenCollectionUI.SetActive(false);
        buyAndReserveUI.SetActive(false);

        turnAction = TurnAction.Hidden;
    }

    void Awake()
    {
        instance = this;
    }

    
}
