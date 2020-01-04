using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class InventorySlotUI : MonoBehaviour
{
    public InventoryUI inventoryUI;
    public Text CountText;
    public int slotNumber;

    public void Start()
    {
        GetComponent<Button>().onClick.AddListener(onClick);
    }
    public void onClick()
    {
        inventoryUI.slotClick(slotNumber);
    }
    public void assignItem(Item item)
    {
        if (item.type != ItemType.empty && item.count > 1)
        {
            CountText.text = item.count.ToString();
        }
        else
        {
            CountText.text = "";
        }
    }
}
