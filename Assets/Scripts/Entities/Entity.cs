using UnityEngine;

public class Entity : PhysicsObject
{
    public int health;

    public EntityType type;
    public Inventory inventory;

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
}

public enum EntityType
{
    player,
    tnt,
    bullet,
    flyingBlock,
}