using UnityEngine;
using Mirror;

public class PlayerManager : NetworkBehaviour
{
    public static NetworkIdentity playerIdentity;
    public static PlayerManager singleton;
    public Entity Player;
    public InventoryUI inventoryUI;
    public GameObject blockHighlight;
    public WorldManager worldManager;
    private bool inventoryOpen = false;

    public void Update()
    {
        if (Player == null)
        {
            return;
        }
        var hit = worldManager.world.raycast(transform.position, transform.forward, 10);
        blockHighlight.SetActive(hit.hit);
        blockHighlight.transform.position = hit.coords;
        if (Input.GetKeyDown(KeyCode.E))
        {
            inventoryOpen = !inventoryOpen;
            if (inventoryOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                inventoryUI.display(Player.inventory);
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
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
                Player.inventory[0].onUse(Player, transform.forward, hit, worldManager.world);
            }
        }
        
    }
    public void finishedLoading()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

}