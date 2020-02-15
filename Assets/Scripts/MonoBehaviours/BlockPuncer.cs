using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPuncer : MonoBehaviour
{
    public WorldManager worldManager;
    public GameObject blockHighlight;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            worldManager.world.raycast(transform.position, transform.forward, 10);
        }
        /*if (hit.hit)
        {
            if (Input.GetMouseButtonDown(0))
            {
                worldManager.world.setBlockAndMesh(hit.coords, BlockType.empty);
            }
            blockHighlight.SetActive(true);
            blockHighlight.transform.position = hit.coords;
            //Debug.Log("hit " + hit.coords);
        }
        else
        {
            //Debug.Log("miss");
            blockHighlight.SetActive(false);
        }*/
        /*if (Input.GetMouseButtonDown(1))
        {
            Vector3Int dest = GetRayDest();
            BlockData dat = worldManager.world.getBlock(dest);
            if (dat.interactable)
            {
                dat.interact(dest, worldManager.world.WorldToChunkCoords(dest), worldManager.world.loadedChunks[worldManager.world.WorldToChunkCoords(dest)], worldManager.world);
            }
            else
            {
                worldManager.world.setBlockAndMesh(dest, BlockType.tnt);
            }

        }*/
    }
}
