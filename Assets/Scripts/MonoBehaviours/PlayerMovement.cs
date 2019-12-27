using UnityEngine;

[RequireComponent(typeof(Entity))]
public class PlayerMovement : MonoBehaviour
{
    public float MoveAccel = 25;
    public float MoveSpeed = 10;
    public float SprintSpeed = 20;
    public float JumpVelocity = 5;
    public float JumpFixDist = 0.1f;
    public float JumpTimeout = 0.05f;
    public float FlyVerticalAccel = 50;
    public float FlyFrictionCoeff = 0.7f;
    public float GroundRayCastDist = 0.05f;
    public int GroundLayer = 8;
    public bool flying = false;
    public bool sprinting = false;

    public WorldManager worldManager;

    private Entity entity;
    private float jumpTimer = 0;
    private RaycastHit raycastHit;
    private int groundLayerMask;

    protected void Awake()
    {
        entity = GetComponent<Entity>();
        jumpTimer = JumpTimeout;
        groundLayerMask = 1 << GroundLayer;
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            flying = !flying;
            if (flying)
            {
                entity.acceleration.y = 0;
            }
            else
            {
                entity.acceleration.y = -9.8f;
            }
        }
        sprinting = Input.GetKey(KeyCode.LeftShift);
        //x of this is the right direction, y of this is the forward direction
        Vector2 moveVector = new Vector2(Time.deltaTime * MoveAccel * Input.GetAxis("Horizontal"), Time.deltaTime * MoveAccel * Input.GetAxis("Vertical"));
        Vector3 localVelocity = transform.worldToLocalMatrix * entity.velocity;
        Vector3 worldMoveForward = transform.localToWorldMatrix * new Vector3(0, 0, moveVector.y);
        Vector3 worldMoveRight = transform.localToWorldMatrix * new Vector3(moveVector.x, 0, 0);
        float maxSpeed = sprinting ? SprintSpeed : MoveSpeed;

        if ((moveVector.x > 0 && localVelocity.x < maxSpeed) || (localVelocity.x > maxSpeed && moveVector.x < 0))
        {
            entity.velocity += worldMoveRight;
        }
        else if ((moveVector.x < 0 && localVelocity.x > -maxSpeed) || (localVelocity.x < -maxSpeed && moveVector.x > 0))
        {
            entity.velocity += worldMoveRight;
        }

        if ((moveVector.y > 0 && localVelocity.z < maxSpeed) || (localVelocity.z > maxSpeed && moveVector.y < 0))
        {
            entity.velocity += worldMoveForward;
        }
        else if ((moveVector.y < 0 && localVelocity.z > -maxSpeed) || (localVelocity.z < -maxSpeed && moveVector.y > 0))
        {
            entity.velocity += worldMoveForward;
        }
        if (flying)
        {
            if (Input.GetKey(KeyCode.Space) && entity.velocity.y < MoveSpeed)
            {
                entity.acceleration.y = FlyVerticalAccel;
            }
            else if (Input.GetKey(KeyCode.LeftControl) && entity.velocity.y > -MoveSpeed)
            {
                entity.acceleration.y = -FlyVerticalAccel;
            }
            else
            {
                entity.acceleration.y = 0;
                entity.velocity.y -= entity.velocity.y * FlyFrictionCoeff;
                if (worldMoveRight.x == 0) entity.velocity.x -= entity.velocity.x * FlyFrictionCoeff;
                if (worldMoveForward.z == 0) entity.velocity.z -= entity.velocity.z * FlyFrictionCoeff;

                if (entity.velocity.sqrMagnitude < 0.01f) entity.velocity = Vector3.zero;
            }
        }
        if (jumpTimer > JumpTimeout && Input.GetKeyDown(KeyCode.Space) && entity.hitDirections[(int)Direction.NegY])
        {
            jumpTimer = 0;
            entity.velocity.y = JumpVelocity;
        }



        jumpTimer += Time.deltaTime;
    }
}