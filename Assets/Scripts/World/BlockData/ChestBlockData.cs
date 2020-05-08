using UnityEngine;
using System.Collections.Generic;

public class ChestBlockData : BlockData
{
    public override void onPlace(Vector3Int worldPos, World world)
    {
        Chunk home = world.getChunk(world.WorldToChunkCoords(worldPos));
        //home.instanceData.Add(worldPos % 16, new BlockInstanceChest());
    }
}