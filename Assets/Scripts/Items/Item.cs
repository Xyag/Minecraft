public struct Item
{
    public ItemType type;
    public BlockType blockType;
    public int count;
}

public enum ItemType
{
    empty,
    block,
    minishark,
}