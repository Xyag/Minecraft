using UnityEngine;

public class ItemBlock : Item
{
    public BlockType blockType = BlockType.stone;
    public ItemBlock(ItemType type, int count) : base(ItemType.block, count) { }
    public ItemBlock(BlockType blockType, int count) : base(ItemType.block, count)
    {
        this.blockType = blockType;
    }
    

    public override void onUse(Entity user, Vector3 useDirection, BlockHit usedOn, World world)
    {
        if (!usedOn.hit)
            return;

        bool hasBlock = false;
        for (int i = 0; i < user.inventory.items.Length; i++)
        {
            if (user.inventory[i] != null && user.inventory[i].type == ItemType.block && user.inventory[i].count > 0)
            {
                hasBlock = true;
                user.inventory.reduceItem(i, 1);
            }
        }
        if (hasBlock)
        {
            if (Mathf.Abs(usedOn.hitOffset.z) > Mathf.Abs(usedOn.hitOffset.y) && Mathf.Abs(usedOn.hitOffset.z) > Mathf.Abs(usedOn.hitOffset.x))
            {
                //place on z axis
                world.setBlockAndMesh(usedOn.coords + new Vector3Int(0, 0, (int)Mathf.Sign(usedOn.hitOffset.z)), blockType);
            }
            else if (Mathf.Abs(usedOn.hitOffset.y) > Mathf.Abs(usedOn.hitOffset.x))
            {
                //place on y axis
                world.setBlockAndMesh(usedOn.coords + new Vector3Int(0, (int)Mathf.Sign(usedOn.hitOffset.y), 0), blockType);
            }
            else
            {
                //place on x axis
                world.setBlockAndMesh(usedOn.coords + new Vector3Int((int)Mathf.Sign(usedOn.hitOffset.x), 0, 0), blockType);
            }
        }
    }
}