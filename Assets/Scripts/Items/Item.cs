using System.Collections.Generic;

public struct Item
{
    public static ItemData[] itemData =
    {
        new ItemData {type = ItemType.empty, maxStack = 0 },
        new ItemData {type = ItemType.block },
        new MinisharkData {type = ItemType.minishark, textureName = "minishark", displayName = "Minishark", maxStack = 1 },
    };


    public ItemType type;
    public BlockType blockType;
    public int count;

    public Item(ItemType type, int count, BlockType blockType = BlockType.empty)
    {
        this.type = type;
        this.count = count;
        this.blockType = blockType;
    }
}

public enum ItemType
{
    empty,
    block,
    minishark,
}