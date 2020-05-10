using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignPlayer : MonoBehaviour
{
    public SunFollower SunFollower;
    public SunFollower MoonFollower;
    public GameObject MainCamera;
    public PlayerManager PlayerManager;
    
    public void Assign(Entity player)
    {
        SunFollower.Following = player.transform;
        MoonFollower.Following = player.transform;
        MainCamera.transform.SetParent(player.transform);
        PlayerManager.Player = player;
        PlayerManager.playerIdentity = player.GetComponent<Mirror.NetworkIdentity>();
        PlayerManager.singleton = PlayerManager;
    }
}
