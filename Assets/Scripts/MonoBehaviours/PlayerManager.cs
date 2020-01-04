using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public Entity Player;
    public InventoryUI inventoryUI;
    private bool inventoryOpen = false;

    public void Update()
    {
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
    }
    public void finishedLoading()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
}