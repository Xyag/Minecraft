using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPuncer : MonoBehaviour
{
    public WorldManager worldManager;
    public int GroundLayer;
    private int groundLayerMask;
    // Start is called before the first frame update
    void Start()
    {
        groundLayerMask = 1 << GroundLayer;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var hit = worldManager.world.raycast(transform.position, transform.forward, 10);
            if (hit.hit)
            {
                Debug.Log(hit.coords);
                worldManager.world.setBlockAndMesh(hit.coords, BlockType.empty);
            }
            else
            {
                Debug.Log("no hit");
            }
        }
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
