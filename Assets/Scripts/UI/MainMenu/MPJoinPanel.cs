using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class MPJoinPanel : MonoBehaviour
{
    public InputField AddressField;
    public Button ConnectButton;
    public GameObject CancelButton;

    public void ConnectButton_Click()
    {
        
        CustomNetworkManager.singleton.networkAddress = AddressField.text;
        CustomNetworkManager.singleton.StartClient();
        ConnectButton.enabled = false;
        CancelButton.SetActive(true);
    }
    public void CancelButton_Click()
    {
        CustomNetworkManager.singleton.StopClient();
        ConnectButton.enabled = true;
        CancelButton.SetActive(false);
    }
}
