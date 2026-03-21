using UnityEngine;
using UnityEngine.UI;

public class ToggleUI : MonoBehaviour
{
    [SerializeField] private Sprite clickedSprite;
    [SerializeField] private Sprite disabledSprite;
    private Image image;

    void Awake()
    {
        image = GetComponent<Image>();
    }
    public void OnToggleClicked(bool clicked)
    {
        image.sprite = clicked ? clickedSprite : disabledSprite;
    }
}
