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
        if (!ServerManager.Instance.IsMyTurn)
            return;
        
        this.activeCardGO = go;
        turnAction = action;
        activeUICanvas = action switch
        {
            TurnAction.Buy_OR_Reserve => buyAndReserveUI,
            TurnAction.Collect_Token => tokenCollectionUI,
            _ => throw new Exception("Unknown Turn Action")
        };
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
