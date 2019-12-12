using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(MeshRenderer))]
public class Entity : MonoBehaviour
{

    public int health;
    public World world;
    public EntityType type;
    [HideInInspector]
    new public MeshRenderer renderer;
    [HideInInspector]
    new public Rigidbody rigidbody;

    public virtual void Awake()
    {
        rigidbody = GetComponent<Rigidbody>();
        renderer = GetComponent<MeshRenderer>();
    }

    public virtual void Start()
    {

    }
    public virtual void Update()
    {

    }
    public virtual void Delete()
    {
        world.loadedEntities.Remove(this);
        Destroy(gameObject);
    }
}

public enum EntityType
{
    player,
    tnt,
}