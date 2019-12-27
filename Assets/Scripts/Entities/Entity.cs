using UnityEngine;

public class Entity : MonoBehaviour
{
    private const float V_ZERO_THRESHOLD = 0.01f; //sets velocity axis to zero if that axis is less than this number to avoid shaking when still

    public Vector3 velocity;
    public Vector3 acceleration = new Vector3(0,-9.8f,0);
    public Vector3 dimensions = new Vector3(0.8f, 1.7f, 0.8f);
    public float friction = 0.5f;
    public bool[] hitDirections = new bool[6];
    public int health;
    public World world;
    public EntityType type;

    public virtual void Awake()
    {
        for (int i = 0; i < hitDirections.Length; i++)
        {
            hitDirections[i] = false;
        }
    }

    public virtual void Start()
    {

    }
    public virtual void Update()
    {

    }
    public virtual void LateUpdate()
    {
        velocity += acceleration * Time.deltaTime;
        transform.position += velocity * Time.deltaTime;

        Vector3 postPosition = transform.position;
        //these coordinates represent the blocks that are in the corners of the collision rectangle. (what blocks we care about changes for each axis)
        int lowXCoord = Mathf.RoundToInt(transform.position.x - dimensions.x * 0.5f);
        int highXCoord = Mathf.RoundToInt(transform.position.x + dimensions.x * 0.5f);

        int lowYCoord = Mathf.CeilToInt(transform.position.y);
        int highYCoord = Mathf.FloorToInt(transform.position.y + dimensions.y);

        int lowZCoord = Mathf.RoundToInt(transform.position.z - dimensions.z * 0.5f);
        int highZCoord = Mathf.RoundToInt(transform.position.z + dimensions.z * 0.5f);
        
        //hit on -Y
        if (velocity.y < 0)
        {
            bool hit = false;
            for (int x = lowXCoord; x <= highXCoord; x++)
            {
                for (int z = lowZCoord; z <= highZCoord; z++)
                {
                    hit = world.getBlock(new Vector3Int(x, lowYCoord, z)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.y = 0;
                postPosition.y = lowYCoord;
            }
            hitDirections[(int)Direction.NegY] = hit;
        }

        //hit on +Y
        if (velocity.y > 0)
        {
            bool hit = false;
            for (int x = lowXCoord; x <= highXCoord; x++)
            {
                for (int z = lowZCoord; z <= highZCoord; z++)
                {
                    hit = world.getBlock(new Vector3Int(x, highYCoord, z)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.y = 0;
                postPosition.y = (float)highYCoord - dimensions.y;
            }
            hitDirections[(int)Direction.PosY] = hit;
        }

        /*lowXCoord = Mathf.FloorToInt(transform.position.x - dimensions.x * 0.5f);
        highXCoord = Mathf.CeilToInt(transform.position.x + dimensions.x * 0.5f);

        lowYCoord = Mathf.FloorToInt(transform.position.y);
        highYCoord = Mathf.CeilToInt(transform.position.y + dimensions.y);
        Debug.Log("low: " + lowYCoord + ", high: " + highYCoord);
        //hit on -X
        if (velocity.x < 0)
        {
            bool hit = false;
            for (int y = lowYCoord; y <= highYCoord; y++)
            {
                for (int z = lowZCoord; z <= highZCoord; z++)
                {
                    hit = world.getBlock(new Vector3Int(lowXCoord, y, z)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.x = 0;
                postPosition.x = lowXCoord;
            }
            hitDirections[(int)Direction.NegX] = hit;
        }

        //hit on +X
        if (velocity.x > 0)
        {
            bool hit = false;
            for (int y = lowYCoord; y <= highYCoord; y++)
            {
                for (int z = lowZCoord; z <= highZCoord; z++)
                {
                    hit = world.getBlock(new Vector3Int(highXCoord, y, z)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.x = 0;
                postPosition.x = (float)highXCoord-dimensions.x;
            }
            hitDirections[(int)Direction.PosX] = hit;
        }

        /*hit on -Z
        if (velocity.z < 0)
        {
            bool hit = false;
            for (int y = lowYCoord; y <= highYCoord; y++)
            {
                for (int x = lowXCoord; x <= highXCoord; x++)
                {
                    hit = world.getBlock(new Vector3Int(x, y, lowZCoord)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.z = 0;
                postPosition.z = lowZCoord;
            }
            hitDirections[(int)Direction.NegZ] = hit;
        }

        //hit on +X
        if (velocity.x < 0)
        {
            bool hit = false;
            for (int y = lowYCoord; y <= highYCoord; y++)
            {
                for (int x = lowXCoord; x <= highXCoord; x++)
                {
                    hit = world.getBlock(new Vector3Int(x, y, highZCoord)).fullCollision;
                    if (hit) goto hitLabel;
                }
            }
        hitLabel: //cant put label in if scope
            if (hit)
            {
                velocity.z = 0;
                postPosition.x = (float)highZCoord - dimensions.z;
            }
            hitDirections[(int)Direction.PosZ] = hit;
        }*/

        if (hitDirections[(int)Direction.NegY] && acceleration.y < 0)
        {
            //kinetic friction
            velocity += velocity.normalized * friction * Time.deltaTime * acceleration.y;
        }

        //setting velocity to zero if it's less than a threshold to avoid shaking when standing still
        if (Mathf.Abs(velocity.x) < V_ZERO_THRESHOLD)
        {
            velocity.x = 0;
        }
        if (Mathf.Abs(velocity.y) < V_ZERO_THRESHOLD)
        {
            velocity.y = 0;
        }
        if (Mathf.Abs(velocity.z) < V_ZERO_THRESHOLD)
        {
            velocity.z = 0;
        }

        transform.position = postPosition;
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