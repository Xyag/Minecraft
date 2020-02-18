using UnityEngine;

public class ItemBlockData : ItemData
{
    public BlockType blockType = BlockType.sand;

    public override void onUse(Entity user, Vector3 useDirection, Vector3Int useBlockPos, World world)
    {
        bool hasBlock = false;
        for (int i = 0; i < user.inventory.items.Length; i++)
        {
            if (user.inventory.items[i].type == ItemType.block && user.inventory.items[i].count > 0)
            {
                hasBlock = true;
            }
        }
        if (hasBlock)
        {
            var hit = world.raycastToEmpty(useBlockPos, -useDirection, 2);
            if (hit.hit)
                world.setBlockAndMesh(hit.coords, blockType);
        }
    }
}