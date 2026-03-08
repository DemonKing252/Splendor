using System;
using UnityEngine;
using UnityEngine.UI;

public class GameInstance : MonoBehaviour
{

    [SerializeField] private Sprite[] gemSprites;
    public Sprite[] GemSprites { get { return gemSprites; } }

    private static GameInstance instance;
    public static GameInstance Instance => instance;
    void Awake()
    {
        instance = this;
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
    }
    void Start()
    {
        // Pick one mode:

        // Optional: set resolution too
    }

}
