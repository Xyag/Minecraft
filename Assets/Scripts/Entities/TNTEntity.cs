using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TNTEntity : Entity
{
    public Vector3 startVelocity = Vector3.up;
    [HideInInspector]
    public float timeToDetonate;
    [HideInInspector]
    public float explosionStrength;

    public Color baseColor;
    public Color flashColor;
    public float flashInterval;

    private float flashTimer;
    private bool onBaseColor = true;
    public override void Start()
    {
        base.Start();
        type = EntityType.tnt;
        rigidbody.AddForce(startVelocity, ForceMode.VelocityChange);
        flashTimer = 0;
        renderer.material.color = baseColor;
    }
    public override void Update()
    {
        flashTimer += Time.deltaTime;
        if (flashTimer > flashInterval)
        {
            flashTimer = 0;
            onBaseColor = !onBaseColor;
            if (onBaseColor)
            {
                renderer.material.color = baseColor;
            }
            else
            {
                renderer.material.color = flashColor;
            }
        }

        timeToDetonate -= Time.deltaTime;
        if (0 > timeToDetonate)
            explode();
    }
    private void explode()
    {
        world.createExplosion(explosionStrength, new Vector3Int((int)transform.position.x, (int)transform.position.y, (int)transform.position.z));
        Delete();
    }
}