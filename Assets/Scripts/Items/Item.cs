using UnityEngine;
using System.Collections.Generic;

public class Item
{
    public static ItemData[] itemData;


    static Item()
    {
        itemData = new ItemData[] {
            new ItemData { type = ItemType.empty, maxStack = 0 },
            new ItemData { type = ItemType.block, textureName = "block", displayName = "block", maxStack = 999 },
            new ItemData { type = ItemType.minishark, textureName = "minishark", displayName = "Minishark", maxStack = 1 },
            new ItemData { type = ItemType.bullet, maxStack = 999, textureName = "bullet", displayName = "Bullet" },
        };

        for (int i = 1; i < itemData.Length; i++) //skip empty texture
        {
            if (itemData[i].textureName != null && itemData[i].textureName != "")
            {
                Texture2D texture = Resources.Load<Texture2D>("items/" + itemData[i].textureName);
                itemData[i].sprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32);
            }
        }
    }

    public ItemType type;
    public int count;

    public Item(ItemType type, int count)
    {
        this.type = type;
        this.count = count;
    }

    public virtual void onUse(Entity user, Vector3 useDirection, BlockHit usedOn, World world) { }
    public virtual void onEquip(Entity user, World world) { }
    public virtual void onDequip(Entity user, World world) { }
}

public enum ItemType
{
    empty,
    block,
    minishark,
    bullet,
}