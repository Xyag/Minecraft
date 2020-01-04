using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CursorInventoryUI : MonoBehaviour
{
    public GameObject itemFrame;
    public WorldManager worldManager;

    private RectTransform itemTransform;
    private Image itemImage;

    void Start()
    {
        itemTransform = itemFrame.GetComponent<RectTransform>();
        itemImage = itemFrame.GetComponent<Image>();
    }

    void Update()
    {
        itemTransform.position = Input.mousePosition;
        if (worldManager.cursorInventory.items[0].type == ItemType.empty)
        {
            itemImage.enabled = false;
        }
        else
        {
            itemImage.enabled = true;
            itemImage.sprite = Item.itemData[(int)worldManager.cursorInventory.items[0].type].sprite;
        }

    }
}
