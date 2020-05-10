using UnityEngine;
using System.Collections.Generic;

public class FallingBlockData : BlockData
{
    public FallingBlockData()
    {
    }
    public override void onBlockUpdate(Vector3Int worldPos, World world)
    {
        if (!world.getBlock(new Vector3Int(worldPos.x, worldPos.y - 1, worldPos.z)).fullCollision)
        {
            world.setBlock(worldPos, BlockType.empty, true);
            GameObject flyingBlock = world.spawnEntity(EntityType.flyingBlock, worldPos, Vector3.zero);
            flyingBlock.GetComponent<FlyingBlock>().setType(type);
        }
    }
}