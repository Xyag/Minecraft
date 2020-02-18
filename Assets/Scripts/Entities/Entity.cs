using UnityEngine;

public class Entity : PhysicsObject
{
    public int health;

    public EntityType type;
    public Inventory inventory;

    public override void Awake()
    {
        base.Awake();
        inventory.items = new Item[10];
        inventory.items[0] = new Item(ItemType.minishark, 1);
        inventory.items[1] = new Item(ItemType.bullet, 10);
        inventory.items[2] = new Item(ItemType.block, 999);
    }

    public virtual void Delete()
    {
        world.loadedEntities.Remove(this);
        Destroy(gameObject);
    }

    public virtual void Disable()
    {
        velocity = Vector3.zero;
        world.loadedEntities.Remove(this);
        gameObject.SetActive(false);
    }

    public virtual void initialize(World world)
    {
        this.world = world;
    }

    public virtual void useItem(int itemSlot)
    {
        ItemData data = Item.itemData[(int)inventory.items[itemSlot].type];
        data.onUse(this, transform.forward, Vector3Int.zero, world);
    }
    public virtual void useItem(int itemSlot, Vector3 direction)
    {
        ItemData data = Item.itemData[(int)inventory.items[itemSlot].type];
        data.onUse(this, direction, Vector3Int.zero, world);
    }
}

public enum EntityType
{
    player,
    tnt,
    bullet,
    flyingBlock,
}