using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPHostActions : MonoBehaviour
{
    public void HostButton_Click()
    {
        SceneData.gameType = SceneData.GameType.Server;
    }
    public void HostAndPlayButton_Click()
    {
        SceneData.gameType = SceneData.GameType.Host;
    }
    public void StartGame()
    {
        if (SceneData.gameType == SceneData.GameType.Host)
        {
            CustomNetworkManager.singleton.maxConnections = 16;
            CustomNetworkManager.singleton.StartHost();
        }
        else if (SceneData.gameType == SceneData.GameType.Singleplayer)
        {
            CustomNetworkManager.singleton.maxConnections = 1;
            CustomNetworkManager.singleton.StartHost();
        }
        else if (SceneData.gameType == SceneData.GameType.Server)
        {
            CustomNetworkManager.singleton.maxConnections = 16;
            CustomNetworkManager.singleton.StartServer();
        }
    }
}
