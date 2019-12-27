using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

[RequireComponent(typeof(WorldManager))]
public class WorldLoader : MonoBehaviour
{
    [HideInInspector]
    public Entity player;
    public World world;
    public int LoadDist = 5;
    public int UnloadDist = 7;
    public int toLoad;
    private List<Vector3Int> loadBuffer;
    private List<Chunk> unloadBuffer;
    private Vector3Int oldPlayerCoords;
    private Thread checkingThread;
    private void Start()
    {
        loadBuffer = new List<Vector3Int>(13 * LoadDist * LoadDist); //should be bigger than needed: this is more than the surface area of the sphere
        unloadBuffer = new List<Chunk>(13 * UnloadDist * UnloadDist);
        oldPlayerCoords = world.WorldToChunkCoords(player.transform.position);

        checkingThread = new Thread(new ParameterizedThreadStart(checkChunkLoading));
        checkingThread.Start(world.WorldToChunkCoords(player.transform.position));
    }

    public void Update()
    {
        Vector3Int playerChunkCoords = world.WorldToChunkCoords(player.transform.position);
        if (playerChunkCoords != oldPlayerCoords)
        {
            //doing this so we can check off the main thread and make sure there is only one thread doing the checking.
            if (!checkingThread.IsAlive)
                checkingThread.Abort();
            checkingThread = new Thread(new ParameterizedThreadStart(checkChunkLoading));
            checkingThread.Start(playerChunkCoords);
        }
        oldPlayerCoords = playerChunkCoords;
    }
    //how do we avoid putting a chunk in the unload buffer if it's already there? is the best solution really to use a contains on every fucking chunk?
    //but yeah we need to fix this
    private void checkChunkLoading(object playerChunkCoordsObj)
    {
        Vector3Int playerChunkCoords = (Vector3Int)playerChunkCoordsObj;
        unloadBuffer.Clear();
        foreach (var chunk in world.loadedChunks.Values)
        {
            if ((chunk.chunkCoords - playerChunkCoords).sqrMagnitude >= UnloadDist * UnloadDist)
            {
                //too far away
                unloadBuffer.Add(chunk);
            }
        }
        foreach (var chunk in unloadBuffer)
        {
            world.unloadChunkBuffer.Enqueue(chunk);
        }
        loadBuffer.Clear();
        for (int x = -LoadDist; x <= LoadDist; x++)
        {
            for (int y = -LoadDist; y <= LoadDist; y++)
            {
                for (int z = -LoadDist; z <= LoadDist; z++)
                {
                    Vector3Int coords = playerChunkCoords + new Vector3Int(x, y, z);
                    int sqrDist = x * x + y * y + z * z;
                    if (sqrDist <= LoadDist * LoadDist && !world.loadedChunks.ContainsKey(coords))
                    {
                        loadBuffer.Add(coords);
                    }
                }
            }
        }
        toLoad = loadBuffer.Count;
        Task.Run(() => loadAll(loadBuffer));
    }

    private async void loadAll(List<Vector3Int> pos)
    {
        //Debug.Log("loadAll called, count: " + pos.Count);
        List<Chunk> chunks = await WorldGenerator.generateList(world, pos);
        MeshGenerator.spawnAll(chunks, world);
    }
}