using UnityEngine;
using System.Collections.Generic;

public class FallingBlockData : BlockData
{
    public FallingBlockData()
    {
    }
    public override void onBlockUpdate(Vector3Int worldPos, World world)
    {
        /*Debug.Log(world.getBlock(new Vector3Int(worldPos.x, worldPos.y - 1, worldPos.z)).type);
        if (!world.getBlock(new Vector3Int(worldPos.x, worldPos.y - 1, worldPos.z)).fullCollision)
        {
            world.setBlockAndMesh(worldPos, BlockType.empty, false);
            GameObject flyingBlock = world.spawnEntity(EntityType.flyingBlock, worldPos, Vector3.zero);
            flyingBlock.GetComponent<FlyingBlock>().setType(type);
        }*/
    }
}