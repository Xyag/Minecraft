using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Entity Player;
    public InventoryUI inventoryUI;
    public GameObject blockHighlight;
    public WorldManager worldManager;
    private bool inventoryOpen = false;

    public void Update()
    {
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
            worldManager.world.setBlockAndMesh(hit.coords, BlockType.empty);
        }
        if (Input.GetMouseButtonDown(1))
        {
            ItemData equipped = Player.inventory.getItemData(0);
            if (hit.hit && hit.blockHit.interactable)
            {
                Vector3Int chunkCoords = worldManager.world.WorldToChunkCoords(hit.coords);
                hit.blockHit.interact(hit.coords, worldManager.world);
            }
            else
            {
                equipped.onUse(Player, transform.forward, hit.coords, worldManager.world);
            }
        }
        
    }
    public void finishedLoading()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
}