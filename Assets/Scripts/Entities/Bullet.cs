using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Entity
{
    public float explosionStrength = 8;
    public float lifetime = 10;
    private float timer;
    public override void Update()
    {
        base.Update();
        transform.LookAt(transform.position + velocity);
        timer += Time.deltaTime;
        if (timer > lifetime)
        {
            Explode();
        }
    }
    public void Explode()
    {
        world.createExplosion(explosionStrength, new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));
        Disable();
    }
    protected override void onCollision(Vector3 oldV)
    {
        Explode();
    }
    public override void initialize(World world)
    {
        base.initialize(world);
        timer = 0;
    }
}
