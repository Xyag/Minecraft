using UnityEngine;

public class ItemData
{
    public Texture texture;
    public string displayName;
    public int maxStack = 999;

    public virtual void onUse(Entity user, Vector3 useDirection, World world) { }
    public virtual void onEquip(Entity user, World world) { }
    public virtual void onDequip(Entity user, World world) { }
}