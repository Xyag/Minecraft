using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SceneData
{
    public enum GameType
    {
        Singleplayer,
        Host,
        Server,
        Join,
    }
    public static string targetWorld = "test";
    public static GameType gameType;
}
