using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    //gets assigned by scene's worldMananger
    public WorldManager worldManager;
    public NetworkConnection localPlayerConnection;

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        Transform startPos = GetStartPosition();
        GameObject player = startPos != null
            ? Instantiate(playerPrefab, startPos.position, startPos.rotation)
            : Instantiate(playerPrefab);

        NetworkServer.AddPlayerForConnection(conn, player);

        var playerEntity = player.GetComponent<Entity>();
        worldManager.onPlayerConnect(playerEntity, conn);
        conn.Send(new LocalPlayerJoinMessage { gameObject = player });  //lets the cient know which player is theirs.
    }
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        localPlayerConnection = conn;
    }
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        worldManager.onPlayerDisconnect(conn);
        base.OnServerDisconnect(conn);
    }
}
