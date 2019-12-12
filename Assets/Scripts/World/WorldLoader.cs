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
    public float CheckInterval = 1;
    public int LoadDist = 5;
    private List<Vector3Int> chunkBuffer;
    private bool checking;
    private void Start()
    {
        chunkBuffer = new List<Vector3Int>();
        checking = false;
        StartCoroutine(chunkLoadingRoutine());
    }

    IEnumerator chunkLoadingRoutine()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(CheckInterval);
            if (!checking)
                checkChunkLoading();
            else
                Debug.Log("not checking");
        }
    }
    private async Task checkChunkLoading()
    {
        checking = true;
        Vector3Int playerChunkCoords = world.WorldToChunkCoords(new Vector3Int((int)player.transform.position.x, (int)player.transform.position.y, (int)player.transform.position.z));
        //chunkBuffer.Clear();
        //foreach (var chunk in world.loadedChunks.Values)
        //{
        //    if ((chunk.chunkCoords - playerChunkCoords).sqrMagnitude >= LoadDist * LoadDist)
        //    {
        //        //too far away
        //        chunkBuffer.Add(chunk.chunkCoords);
        //    }
        //}
        //foreach (var chunk in chunkBuffer)
        //{
        //    world.unloadChunk(chunk);
        //}
        chunkBuffer.Clear();
        for (int x = -LoadDist; x <= LoadDist; x++)
        {
            for (int y = -LoadDist; y <= LoadDist; y++)
            {
                for (int z = -LoadDist; z <= LoadDist; z++)
                {
                    Vector3Int coords = playerChunkCoords + new Vector3Int(x, y, z);
                    //int sqrDist = new Vector3Int(x, y, z).sqrMagnitude;
                    if (!world.loadedChunks.ContainsKey(coords))
                    {
                        chunkBuffer.Add(coords);
                    }
                    else
                    {
                        Debug.Log(playerChunkCoords);
                        Debug.Log("rejecting for presence: " + coords);
                    }
                }
            }
        }
        if (chunkBuffer.Count > 0)
        {
            Debug.Log(chunkBuffer.Count);
            await WorldGenerator.generateAndMeshList(world, chunkBuffer);
        }
        checking = false;
    }
}