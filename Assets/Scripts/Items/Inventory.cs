using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Inventory
{
    public Item[] items;
    public Inventory(int size)
    {
        items = new Item[size];
    }
    public void reduceItem(int index, int count)
    {
        items[index].count -= count;
        if (items[index].count <= 0)
        {
            items[index] = new Item(ItemType.empty, 0);
        }
    }
    public ItemData getItemData(int slot)
    {
        return Item.itemData[(int)items[slot].type];
    }
}
