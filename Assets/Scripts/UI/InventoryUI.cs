using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : CloseableUIPanel
{
    public GameObject InventoryScrollView;
    public RectTransform InventoryContent;
    public GameObject SlotPrefab;
    public GameObject ItemImagePrefab;
    public WorldManager worldManager;
    public Vector2 slotPadding = new Vector2(10, 10);
    public Vector2 slotOffset = new Vector2(10, -100);

    private Pool<GameObject> items;
    private Pool<GameObject> slots;
    private Inventory displaying;

    public void Awake()
    {
        if (items == null) items = Pool<GameObject>.createGameObjectPool(ItemImagePrefab);
        if (slots == null) slots = Pool<GameObject>.createGameObjectPool(SlotPrefab);
    }

    public void display(Inventory inventory)
    {
        open();
        displaying = inventory;
        InventoryScrollView.SetActive(true);
        float panelWidth = InventoryScrollView.GetComponent<RectTransform>().sizeDelta.x;
        Vector2 slotSize = SlotPrefab.GetComponent<RectTransform>().sizeDelta;
        int slotsPerRow = (int)(panelWidth/(slotPadding.x + slotSize.x));
        slotsPerRow = slotsPerRow < 1 ? 1 : slotsPerRow;
        InventoryContent.sizeDelta = new Vector2(0, slotOffset.y + (slotPadding.y + slotSize.y) * Mathf.Ceil((float)inventory.items.Length/(float)slotsPerRow));

        for (int i = 0; i < inventory.items.Length; i++)
        {
            GameObject disp = slots.get();
            disp.transform.SetParent(InventoryContent, false);
            disp.transform.localPosition = new Vector3((i % slotsPerRow) * (slotPadding.x + slotSize.x) + slotOffset.x, -(i / slotsPerRow) * (slotPadding.y + slotSize.y) + slotOffset.y);
            var invSlotUI = disp.GetComponent<InventorySlotUI>();
            invSlotUI.slotNumber = i;
            invSlotUI.inventoryUI = this;
            invSlotUI.assignItem(inventory[i]);
            if (inventory[i] != null && inventory[i].type != ItemType.empty)
            {
                GameObject itemDisp = items.get();
                itemDisp.transform.SetParent(disp.transform, false);
                itemDisp.transform.localPosition = Vector3.zero;
                itemDisp.GetComponent<Image>().sprite = Item.itemData[(int)inventory[i].type].sprite;
            }
        }
    }
    public override void close()
    {
        base.close();
        foreach (GameObject g in items.objects)
        {
            g.SetActive(false);
        }
        foreach (GameObject g in slots.objects)
        {
            g.SetActive(false);
        }
        InventoryScrollView.SetActive(false);
    }
    public void slotClick(int slotNum)
    {
        Item temp = displaying[slotNum];
        displaying[slotNum] = worldManager.cursorInventory[0];
        worldManager.cursorInventory[0] = temp;
        display(displaying);
    }
}
