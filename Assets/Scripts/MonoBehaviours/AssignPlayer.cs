using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignPlayer : MonoBehaviour
{
    public SunFollower SunFollower;
    public SunFollower MoonFollower;
    public GameObject MainCamera;
    public Vector3 CameraOffset = new Vector3(0,0.8f,0);
    public PlayerManager PlayerManager;
    
    public void Assign(Entity player)
    {
        SunFollower.Following = player.transform;
        MoonFollower.Following = player.transform;
        MainCamera.transform.SetParent(player.transform, false);
        MainCamera.transform.localPosition = CameraOffset;
        PlayerManager.Player = player;
        PlayerManager.playerIdentity = player.GetComponent<Mirror.NetworkIdentity>();
        PlayerManager.singleton = PlayerManager;
    }
}
