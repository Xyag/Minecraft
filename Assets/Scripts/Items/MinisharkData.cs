using UnityEngine;

public class MinisharkData : ItemData
{
    public EntityType bulletType = EntityType.bullet;
    public float bulletSpeed = 60;
    public float useInterval = 0.1f;

    private float lastUseTime = 0;
    public override void onUse(Entity user, Vector3 useDirection, Vector3Int useBlockPos, World world)
    {
        if (Time.time - lastUseTime > useInterval)
        {
            bool hasAmmo = false;
            for (int i = 0; i < user.inventory.items.Length; i++)
            {
                if (user.inventory.items[i].type == ItemType.bullet && user.inventory.items[i].count > 0)
                {
                    hasAmmo = true;
                    user.inventory.items[i].count--;
                    if (user.inventory.items[i].count == 0)
                    {
                        user.inventory.items[i].type = ItemType.empty;
                    }
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