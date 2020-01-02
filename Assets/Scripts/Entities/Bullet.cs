using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : Entity
{
    public float explosionStrength = 8;
    public override void Update()
    {
        base.Update();
        transform.LookAt(transform.position + velocity);
    }
    protected override void onCollision()
    {
        base.onCollision();
        world.createExplosion(explosionStrength, new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));
        Disable();
    }
}
