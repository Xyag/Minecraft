using UnityEngine;
using Mirror;
using System;

public class PlayerManager : NetworkBehaviour
{
    public static NetworkIdentity playerIdentity;
    public static PlayerManager singleton;
    public Entity Player;
    public InventoryUI inventoryUI;
    public CloseableUIPanel pauseMenu;
    public GameObject blockHighlight;
    public WorldManager worldManager;

    public event EventHandler<HeadEnterBlockEventArgs> OnHeadEnterBlock;
    private BlockType oldHeadBlock;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //doing this up here so that the server can use the pause menu.
            pauseMenu.toggle();
        }
        if (Player == null)
        {
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        doInput();
        doEvents();

        if (anythingOpen())
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    private void doEvents()
    {
        BlockData headBlock = worldManager.world.getBlock(transform.position.toInt());
        if (headBlock.type != oldHeadBlock)
        {
            OnHeadEnterBlock?.Invoke(this, new HeadEnterBlockEventArgs { block = headBlock, blockPosition = transform.position.toInt() });
        }
        oldHeadBlock = headBlock.type;
    }
    private void doInput()
    {
        var hit = worldManager.world.raycast(transform.position, transform.forward, 10);
        blockHighlight.SetActive(hit.hit);
        blockHighlight.transform.position = hit.coords;
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (inventoryUI.Open)
            {
                inventoryUI.display(Player.inventory);
            }
            else
            {
                inventoryUI.close();
            }
        }
        if (Input.GetMouseButtonDown(0))
        {
            worldManager.SendRequestSetBlock(playerIdentity, hit.coords, BlockType.empty);
        }
        if (Input.GetMouseButtonDown(1))
        {
            if (hit.hit && hit.blockHit.interactable)
            {
                hit.blockHit.interact(hit.coords, worldManager.world);
            }
            else
            {
                worldManager.SendRequestSetBlock(playerIdentity, hit.coords, BlockType.tnt);
                //Player.inventory[0].onUse(Player, transform.forward, hit, worldManager.world);
            }
        }
    }
    private bool anythingOpen()
    {
        return pauseMenu.Open || inventoryUI.Open;
    }
    public void finishedLoading()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

}