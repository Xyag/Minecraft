using UnityEngine;

public class MinisharkData : ItemData
{
    public EntityType bulletType = EntityType.bullet;
    public float bulletSpeed = 60;
    public float useInterval = 0.1f;

    private float lastUseTime = 0;
    public override void onUse(Entity user, Vector3 useDirection, World world)
    {
        if (Time.time - lastUseTime > useInterval)
        {
            lastUseTime = Time.time;
            GameObject obj = user.world.spawnEntity(bulletType, user.transform.position);
            Vector3 shootV = useDirection * bulletSpeed;
            obj.transform.LookAt(user.transform.position + useDirection);
            obj.GetComponent<PhysicsObject>().velocity = shootV;
        }
    }
}