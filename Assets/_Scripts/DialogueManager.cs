using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ImgType
{
    Finger,
    HourGlass
}

public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;
    public static DialogueManager Instance => instance;
    
    [Header("Warnings")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Transform warningPanelTransform;
    private Coroutine warningCoroutine;

    [Header("TurnStatus")]
    [SerializeField] private TMP_Text turnStatusText;
    [SerializeField] private Transform turnStatusPanelTransform;
    private Coroutine turnCoroutine;
    [SerializeField] private Transform statusImageTransform;
    [SerializeField] private Sprite pointFingerSprite;
    [SerializeField] private Sprite hourGlassSprite;

    void Start()
    {
        warningPanelTransform.gameObject.SetActive(false);
        turnStatusPanelTransform.gameObject.SetActive(false);
    }
    void Awake()
    {
        instance = this;
    }
    public void SetWarningText(string text, Color color, float fadeInDuration = 0.25f, float appearDuration = 3f)
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
            warningPanelTransform.localScale = Vector3.zero;
            warningPanelTransform.gameObject.SetActive(false);
        }

        warningCoroutine = StartCoroutine(WarningTextProc(text, color, fadeInDuration, appearDuration));
    }
    public void SetTurnStatusText(string text, Color color, ImgType imgType)
    {
        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
        }

        turnCoroutine = StartCoroutine(TurnStatusProc(text, color, imgType));
    }

    private IEnumerator TurnStatusProc(string text, Color color, ImgType imgType)
    {
        statusImageTransform.GetComponent<Image>().sprite = imgType == ImgType.HourGlass ? hourGlassSprite : pointFingerSprite;
        if (imgType == ImgType.HourGlass)
            statusImageTransform.GetComponent<RectTransform>().sizeDelta = new Vector2(70f, 70f);
        else
            statusImageTransform.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);


        turnStatusPanelTransform.gameObject.SetActive(true);
        turnStatusPanelTransform.localScale = Vector3.zero;

        turnStatusText.color = color;
        turnStatusText.text = text;
        float time = 0f;
        while (time < 0.25f)
        {
            turnStatusPanelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time / 0.25f);

            time += Time.deltaTime;
            yield return null;
        }
        
        turnStatusPanelTransform.localScale = Vector3.one;
    }

    private IEnumerator WarningTextProc(string text, Color color, float fadeInOutDur, float appearDuration)
    {
        warningPanelTransform.gameObject.SetActive(true);
        warningPanelTransform.localScale = Vector3.zero;

        warningText.color = color;
        warningText.text = text;
        float time = 0f;
        while (time < fadeInOutDur)
        {
            warningPanelTransform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, time / fadeInOutDur);

            time += Time.deltaTime;
            yield return null;
        }
        
        time = 0f;
        warningPanelTransform.localScale = Vector3.one;
        yield return new WaitForSeconds(appearDuration);
        while (time < fadeInOutDur)
        {
            warningPanelTransform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, time / fadeInOutDur);

            time += Time.deltaTime;
            yield return null;
        }
        warningPanelTransform.localScale = Vector3.zero;

        warningText.text = "";
        warningPanelTransform.gameObject.SetActive(false);
    }
}
