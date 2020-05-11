using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MPInGameMenu : MonoBehaviour
{
    public void DisconnectButton_Click()
    {
        // stop host if host mode
        if (NetworkServer.active && NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopHost();
        }
        // stop client if client-only
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
        // stop server if server-only
        else if (NetworkServer.active)
        {
            NetworkManager.singleton.StopServer();
        }
    }
}
