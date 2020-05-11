using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Mirror;

[RequireComponent(typeof(WorldManager))]
public class WorldLoader : NetworkBehaviour
{
    //[HideInInspector]
    public Entity player;   //assigned by worldmanager
    public World world; //assigned by worldmanager
    public int LoadDist = 5;
    public int UnloadDist = 7;
    public int toLoad;
    public float saveInterval = 10;
    public float chunkLoadInterval = 0.5f;
    private List<Vector3Int> loadBuffer;
    private List<Chunk> unloadBuffer;
    private Vector3Int oldPlayerCoords;
    private Thread saveThread;
    private float saveTimer = 0;
    private float chunkLoadTimer = 0;
    private bool loadedOnce;
    private void Start()
    {
        loadBuffer = new List<Vector3Int>(13 * LoadDist * LoadDist); //should be bigger than needed: this is more than the surface area of the sphere
        unloadBuffer = new List<Chunk>(13 * UnloadDist * UnloadDist);
        saveThread = null;
        saveTimer = 0;
        loadedOnce = false;
    }
    public void Update()
    {
        if (world == null)
        {
            return;
        }
        if (NetworkClient.active && player != null)
        {
            Vector3Int playerChunkCoords = world.WorldToChunkCoords(player.transform.position);
            if (playerChunkCoords != oldPlayerCoords || !loadedOnce)
            {
                loadedOnce = true; //this way the client loads chunks first before moving.
                //cant do this in start because player is not assigned.
                checkChunkLoading();
            }
            oldPlayerCoords = playerChunkCoords;
        }
        
        if (NetworkServer.active)
        {
            saveTimer += Time.deltaTime;
            chunkLoadTimer += Time.deltaTime;
            if (saveTimer > saveInterval)
            {
                saveOnThread();
            }
            if (chunkLoadTimer > chunkLoadInterval)
            {
                chunkLoadTimer = 0;
                checkChunkLoading();
            }
        }
        
    }
    public void OnApplicationQuit()
    {
        //doing this on the main thread at the end to make sure it completes.
        if (saveThread != null && saveThread.IsAlive)
        {
            saveThread.Abort();
        }
        world.saveAll();
    }
    private void saveOnThread()
    {
        saveTimer = 0;
        if (saveThread != null && saveThread.IsAlive)
        {
            saveThread.Abort();
        }
        saveThread = new Thread(new ThreadStart(world.saveAll));
        saveThread.Start();
        Debug.Log("saved");
    }
    public bool chunkNearPlayer(int offsetX, int offsetY, int offsetZ)
    {
        return offsetX*offsetX + offsetY*offsetY + offsetZ*offsetZ <= LoadDist * LoadDist;
    }
    public bool chunkNearPlayer(Vector3Int playerChunkCoords, Vector3Int chunkCoords)
    {
        return (playerChunkCoords - chunkCoords).sqrMagnitude <= LoadDist * LoadDist;
    }
    private void checkChunkLoading()
    {
        unloadBuffer.Clear();
        loadBuffer.Clear();
        if (NetworkServer.active)
        {
            foreach (var chunk in world.loadedChunks.Values)
            {
                foreach(var player in world.players.Values)
                {
                    Vector3Int playerChunkCoords = world.WorldToChunkCoords(player.transform.position);
                    if (chunkNearPlayer(playerChunkCoords,chunk.chunkCoords))
                    {
                        //too close to someone, don't add to unload buffer.
                        goto next;
                    }
                }
                unloadBuffer.Add(chunk);
                next:
                continue;
            }
            foreach (var player in world.players.Values)
            {
                for (int x = -LoadDist; x <= LoadDist; x++)
                {
                    for (int y = -LoadDist; y <= LoadDist; y++)
                    {
                        for (int z = -LoadDist; z <= LoadDist; z++)
                        {
                            Vector3Int playerCoords = new Vector3Int(x, y, z) + world.WorldToChunkCoords(player.transform.position);
                            if (world.chunkInBounds(playerCoords) && chunkNearPlayer(x,y,z) && !world.loadedChunks.ContainsKey(playerCoords))
                            {
                                loadBuffer.Add(playerCoords);
                            }
                        }
                    }
                }
            }
        }
        else //if host & play, we don't run this, don't want everything to be unloaded!!!
        {
            Vector3Int playerChunkCoords = world.WorldToChunkCoords(player.transform.position);
            foreach (var chunk in world.loadedChunks.Values)
            {
                if (!chunkNearPlayer(playerChunkCoords, chunk.chunkCoords))
                {
                    //too far away
                    unloadBuffer.Add(chunk);
                }
            }
            for (int x = -LoadDist; x <= LoadDist; x++)
            {
                for (int y = -LoadDist; y <= LoadDist; y++)
                {
                    for (int z = -LoadDist; z <= LoadDist; z++)
                    {
                        Vector3Int playerCoords = new Vector3Int(x, y, z) + world.WorldToChunkCoords(player.transform.position);
                        if (world.chunkInBounds(playerCoords) && chunkNearPlayer(x, y, z) && !world.loadedChunks.ContainsKey(playerCoords))
                        {
                            loadBuffer.Add(playerCoords);
                        }
                    }
                }
            }
        }
        
        foreach (var chunk in unloadBuffer)
        {
            world.unloadChunkBuffer.TryAdd(chunk.chunkCoords, chunk);
        }
        toLoad = loadBuffer.Count;
        loadAll(loadBuffer);
    }
    private void loadAll(List<Vector3Int> positions)
    {
        if (NetworkServer.active)
        {
            Task.Run(() => world.generateChunks(positions));
            if (NetworkClient.active)
            {
                //host & play
                foreach (var pos in positions)
                {
                    //only draw chunks close enough to player
                    if (chunkNearPlayer(world.WorldToChunkCoords(player.transform.position),pos))
                        WorldManager.singleton.SendRequestChunk(pos);
                }
            }
        }
        else if (NetworkClient.active)
        {
            foreach (var pos in positions)
            {
                WorldManager.singleton.SendRequestChunk(pos);
            }
        }
    }
}