using UnityEngine;

public class ItemMinishark : Item
{
    public ItemMinishark(ItemType type, int count) : base(ItemType.minishark,count){}

    public EntityType bulletType = EntityType.bullet;
    public float bulletSpeed = 60;
    public float useInterval = 0.1f;

    private float lastUseTime = 0;
    public override void onUse(Entity user, Vector3 useDirection, BlockHit usedOn, World world)
    {
        if (Time.time - lastUseTime > useInterval)
        {
            bool hasAmmo = false;
            for (int i = 0; i < user.inventory.items.Length; i++)
            {
                if (user.inventory[i] != null && user.inventory[i].type == ItemType.bullet && user.inventory[i].count > 0)
                {
                    hasAmmo = true;
                    user.inventory.reduceItem(i, 1);
                }
            }
            if (hasAmmo)
            {
                lastUseTime = Time.time;
                GameObject obj = user.world.spawnEntity(bulletType, user.transform.position, useDirection * bulletSpeed);
                obj.transform.LookAt(user.transform.position + useDirection);
            }
        }
    }
}