using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Entity Player;
    public InventoryUI inventoryUI;
    public GameObject blockHighlight;
    public WorldManager worldManager;
    private bool inventoryOpen = false;

    public void Start()
    {
        Player.inventory.items = new Item[10];
        Player.inventory[1] = new ItemMinishark(ItemType.minishark, 1);
        Player.inventory[2] = new Item(ItemType.bullet, 10);
        Player.inventory[0] = new ItemBlock(BlockType.tnt, 999);
    }
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