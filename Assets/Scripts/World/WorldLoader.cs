using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

[RequireComponent(typeof(WorldManager))]
public class WorldLoader : MonoBehaviour
{
    [HideInInspector]
    public Entity player;
    public World world;
    public int LoadDist = 5;
    public int UnloadDist = 7;
    private List<Vector3Int> chunkBuffer;
    private Vector3Int oldPlayerCoords;
    private void Start()
    {
        chunkBuffer = new List<Vector3Int>(13 * UnloadDist * UnloadDist); //should be bigger than needed: this is more than the surface area of the sphere
        oldPlayerCoords = world.WorldToChunkCoords(player.transform.position);
    }

    public void Update()
    {
        Vector3Int playerChunkCoords = world.WorldToChunkCoords(player.transform.position);
        if (playerChunkCoords != oldPlayerCoords)
        {
            checkChunkLoading();
        }
        oldPlayerCoords = playerChunkCoords;
    }

    private void checkChunkLoading()
    {
        Debug.Log("checking chunks...");
        Vector3Int playerChunkCoords = world.WorldToChunkCoords(new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y, (int)player.transform.position.z));
        chunkBuffer.Clear();
        foreach (var chunk in world.loadedChunks.Values)
        {
            if ((chunk.chunkCoords - playerChunkCoords).sqrMagnitude >= UnloadDist * UnloadDist)
            {
                //too far away
                chunkBuffer.Add(chunk.chunkCoords);
            }
        }
        Debug.Log("unloading " + chunkBuffer.Count + " chunks...");
        foreach (var chunk in chunkBuffer)
        {
            world.unloadChunk(chunk);
        }
        chunkBuffer.Clear();
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
                        chunkBuffer.Add(coords);
                    }
                }
            }
        }
        Debug.Log("loading " + chunkBuffer.Count + " chunks...");
        Task.Run(() => generateAll(chunkBuffer));
    }

    private async void generateAll(List<Vector3Int> pos)
    {
        List<Chunk> chunks = await WorldGenerator.generateList(world, pos);
        Debug.Log("finished generating " + chunks.Count + " chunks");
        MeshGenerator.spawnAll(chunks, world);
    }
}