using System;
using UnityEngine;
using UnityEngine.UI;

public static class Utility
{
    public static string userName = "";
    public static string server_status_msg = "";
    public static Color server_status_msg_color = Color.white;
}

public class GameInstance : MonoBehaviour
{
    [SerializeField] private Sprite[] gemSprites;
    public Sprite[] GemSprites => gemSprites;

    [SerializeField] private Color[] gemVertexColors;
    public Color[] GemVertexColors => gemVertexColors; 
    

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
