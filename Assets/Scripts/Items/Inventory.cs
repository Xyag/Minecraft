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
    public ItemData getItemData(int slot)
    {
        return Item.itemData[(int)items[slot].type];
    }
}
