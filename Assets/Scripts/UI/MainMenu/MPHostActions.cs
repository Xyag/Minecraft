using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MPHostActions : MonoBehaviour
{
    public void HostButton_Click()
    {
        CustomNetworkManager.singleton.StartServer();
    }
    public void HostAndPlayButton_Click()
    {
        CustomNetworkManager.singleton.StartHost();
    }
}
