using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public enum TurnAction
{
    CollectTokens,
    ReserveCardAndWildToken,
    BuyCard,
    None
}

public class ConfirmationMessageBehaviour : MonoBehaviour
{
    private static ConfirmationMessageBehaviour instance;
    public static ConfirmationMessageBehaviour Instance => instance;

    [SerializeField] private TurnAction turnAction;
    public TurnAction TurnAction => turnAction;
    public GameObject clickedGO;

    public delegate void OnCollectTokens();
    public delegate void OnReserveCard();
    public delegate void OnPurchaseCard(GameObject go);

    public event OnCollectTokens onCollectTokens;
    public event OnReserveCard onReserveCard;
    public event OnPurchaseCard onPurchaseCard;

    [SerializeField] public Button confirmBtn;
    [SerializeField] public Button cancelBtn;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        confirmBtn.onClick.AddListener(() => OnConfirmClicked() );
        cancelBtn.onClick.AddListener(() => CancelledClicked() );
    }

    public void ShowConfirmMsg(TurnAction action, GameObject go = null)
    {
        this.clickedGO = go;
        turnAction = action;
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);  
    }

    void Awake()
    {
        instance = this;
        gameObject.SetActive(false);
    }

    public void OnConfirmClicked()
    {
        switch (turnAction)
        {
            case TurnAction.CollectTokens: onCollectTokens?.Invoke(); break;
            case TurnAction.ReserveCardAndWildToken: onReserveCard?.Invoke(); break;
            case TurnAction.BuyCard: onPurchaseCard?.Invoke(clickedGO); break;
            case TurnAction.None: Debug.LogError("Unknown action."); break;
        };

        Hide();
    }
    public void CancelledClicked()
    {
        Hide();
    }
}
