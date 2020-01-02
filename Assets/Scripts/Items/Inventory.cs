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
}
