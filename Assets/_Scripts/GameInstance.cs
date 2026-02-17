using System;
using UnityEngine;
using UnityEngine.UI;

public class GameInstance : MonoBehaviour
{

    [SerializeField] private Sprite[] gemSprites;
    public Sprite[] GemSprites { get { return gemSprites; } }

    private static GameInstance instance;
    public static GameInstance Instance
    {
        get { return instance;}
    }
    void Awake()
    {
        instance = this;
    }

}
