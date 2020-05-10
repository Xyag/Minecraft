using UnityEngine;
using System.Collections.Generic;

public class TNTBlockData : BlockData
{
    public float explosionStrength;
    public float fuseLength;
    public float fuseVariance = 0.5f;

    public TNTBlockData()
    {
    }
    public override void interact(Vector3Int worldPos, World world)
    {
        //world.createExplosion(explosionStrength, worldPos);
        var tntObject = world.spawnEntity(EntityType.tnt, worldPos, Vector3.zero);
        var tntScript = tntObject.GetComponent<TNTEntity>();
        tntScript.explosionStrength = explosionStrength;
        tntScript.timeToDetonate = fuseLength + Random.Range(-fuseVariance, fuseVariance);
        world.setBlock(worldPos, BlockType.empty);
    }
}