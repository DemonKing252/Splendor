using System.Collections;
using TMPro;
using UnityEngine;

public class WarningMessage : MonoBehaviour
{
    private static WarningMessage instance;
    public static WarningMessage Instance => instance;
    
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private Transform warningPanelTransform;
    private Coroutine runningCoroutine;


    void Start()
    {
        warningPanelTransform.gameObject.SetActive(false);
    }
    void Awake()
    {
        instance = this;
    }
    public void SetWarningText(string text, Color color, float fadeInDuration = 0.25f, float appearDuration = 3f)
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
            warningPanelTransform.localScale = Vector3.zero;
            warningPanelTransform.gameObject.SetActive(false);
        }

        runningCoroutine = StartCoroutine(WarningTextProc(text, color, fadeInDuration, appearDuration));
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
