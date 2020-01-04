using UnityEngine;

public class ItemData
{
    public ItemType type;
    public string textureName;
    public UnityEngine.Sprite sprite;
    public string displayName;
    public int maxStack = 999;

    public virtual void onUse(Entity user, Vector3 useDirection, World world) { }
    public virtual void onEquip(Entity user, World world) { }
    public virtual void onDequip(Entity user, World world) { }
}