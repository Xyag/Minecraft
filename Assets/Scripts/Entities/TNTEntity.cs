using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(Light))]
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
    new private Light light;
    new private MeshRenderer renderer;
    public override void Start()
    {
        base.Start();
        renderer = GetComponent<MeshRenderer>();
        type = EntityType.tnt;
        velocity = startVelocity;
        light = GetComponent<Light>();
        flashTimer = 0;
        renderer.material.color = baseColor;
    }
    public override void Update()
    {
        base.Update();
        flashTimer += Time.deltaTime;
        if (flashTimer > flashInterval)
        {
            flashTimer = 0;
            onBaseColor = !onBaseColor;
            if (onBaseColor)
            {
                renderer.material.color = baseColor;
                light.color = baseColor;
            }
            else
            {
                renderer.material.color = flashColor;
                light.color = flashColor;
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