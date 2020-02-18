using UnityEngine;

public class PhysicsObject : MonoBehaviour
{
    private const float V_ZERO_THRESHOLD = 0.01f; //sets velocity axis to zero if that axis is less than this number to avoid shaking when still
    private const float BBOX_THICKNESS = 0.1f; //thickness of the bounding box int the moving direction during collision detections.

    public bool checkCollision = true;
    public Vector3 velocity;
    public Vector3 acceleration = new Vector3(0, -9.8f, 0);
    public Vector3 extents = new Vector3(0.4f, 0.85f, 0.4f);
    public float friction = 0.5f;
    public bool[] hitDirections = new bool[6];
    public World world { get; protected set; }

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
        physicsUpdate();
    }
    public void physicsUpdate()
    {
        if (checkCollision)
        {
            doCollision();
        }

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
        transform.position += velocity * Time.deltaTime;
        velocity += acceleration * Time.deltaTime;
    }
    //called after the collision is resloved.
    protected virtual void onCollision(Vector3 oldVelocity)
    {

    }
    protected void doCollision()
    {
        Vector3 oldV = velocity;
        for (int i = 0; i < hitDirections.Length; i++)
        {
            hitDirections[i] = false;
        }
        Vector3 postPosition = transform.position;

        Vector3 frameVelocity = velocity * Time.deltaTime; //this is the amount the object will move this frame

        //we're going to check collision by iterating through each plane orthogonal to the velocity the the three axes directions.
        //we only have to check each plane of blocks that the frame velocity vector hits.

        //y axis
        if (frameVelocity.y < 0)
        {
            //moving down
            for (int y = 0; y > Mathf.FloorToInt(frameVelocity.y); y--)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(extents.x, -extents.y + (float)y, extents.z), new Vector3(extents.x, BBOX_THICKNESS, extents.z));
                if (box.intersectsBlock(world))
                {
                    //postPosition.y = Mathf.RoundToInt(postPosition.y+(float)y);
                    velocity.y = 0;
                    hitDirections[(int)Direction.NegY] = true;
                    break;
                }
            }
        }
        else
        {
            //moving up
            for (int y = 0; y < Mathf.CeilToInt(frameVelocity.y); y++)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(extents.x, extents.y + (float)y, extents.z), new Vector3(extents.x, BBOX_THICKNESS, extents.z));
                if (box.intersectsBlock(world))
                {
                    //postPosition.y = Mathf.RoundToInt(postPosition.y + (float)y);
                    velocity.y = 0;
                    hitDirections[(int)Direction.PosY] = true;
                    break;
                }
            }
        }

        //x axis
        if (frameVelocity.x < 0)
        {
            //-v
            for (int x = 0; x > Mathf.FloorToInt(frameVelocity.x); x--)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(-extents.x + (float)x, extents.y, extents.z), new Vector3(BBOX_THICKNESS, extents.y, extents.z));
                if (box.intersectsBlock(world))
                {
                    //postPosition.x = Mathf.RoundToInt(postPosition.x + (float)x);
                    velocity.x = 0;
                    hitDirections[(int)Direction.NegX] = true;
                    break;
                }
            }
        }
        else
        {

            //+v
            for (int x = 0; x < Mathf.CeilToInt(frameVelocity.x); x++)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(extents.x + (float)x, extents.y, extents.z), new Vector3(BBOX_THICKNESS, extents.y, extents.z));
                if (box.intersectsBlock(world))
                {
                    //postPosition.x = Mathf.RoundToInt(postPosition.x + (float)x);
                    velocity.x = 0;
                    hitDirections[(int)Direction.PosX] = true;
                    break;
                }
            }
        }

        //z axis
        if (frameVelocity.z < 0)
        {
            //-v
            for (int z = 0; z > Mathf.FloorToInt(frameVelocity.z); z--)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(extents.x, extents.y, -extents.z+(float)z), new Vector3(extents.x, extents.y, BBOX_THICKNESS));
                if (box.intersectsBlock(world))
                {
                    //postPosition.z = Mathf.RoundToInt(postPosition.z + (float)z);
                    velocity.z = 0;
                    hitDirections[(int)Direction.NegZ] = true;
                    break;
                }
            }
        }
        else
        {
            //+v
            for (int z = 0; z < Mathf.CeilToInt(frameVelocity.z); z++)
            {
                BoundingBox box = new BoundingBox(postPosition + new Vector3(extents.x, extents.y, extents.z + (float)z), new Vector3(extents.x, extents.y, BBOX_THICKNESS));
                if (box.intersectsBlock(world))
                {
                    //postPosition.z = Mathf.RoundToInt(postPosition.z + (float)z);
                    velocity.z = 0;
                    hitDirections[(int)Direction.PosZ] = true;
                    break;
                }
            }
        }
        transform.position = postPosition;


        for (int i = 0; i < hitDirections.Length; i++)
        {
            if (hitDirections[i])
            {
                onCollision(oldV);
                return;
            }
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public BoundingBox getBoundingBox()
    {
        return new BoundingBox(transform.position, extents);
    }
}