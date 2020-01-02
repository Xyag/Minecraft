using UnityEngine;

[RequireComponent(typeof(PhysicsObject))]
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
    public bool flying = false;
    public bool sprinting = false;

    public GameObject playerCamera;
    public float bulletSpeed = 40;

    private PhysicsObject playerPhysics;
    private Entity player;
    private float jumpTimer = 0;

    protected void Awake()
    {
        playerPhysics = GetComponent<PhysicsObject>();
        player = GetComponent<Entity>();
        jumpTimer = JumpTimeout;
    }

    protected void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            flying = !flying;
            if (flying)
            {
                playerPhysics.acceleration.y = 0;
            }
            else
            {
                playerPhysics.acceleration.y = -9.8f;
            }
        }
        sprinting = Input.GetKey(KeyCode.LeftShift);
        //x of this is the right direction, y of this is the forward direction
        Vector2 moveVector = new Vector2(Time.deltaTime * MoveAccel * Input.GetAxis("Horizontal"), Time.deltaTime * MoveAccel * Input.GetAxis("Vertical"));
        Vector3 localVelocity = transform.worldToLocalMatrix * playerPhysics.velocity;
        Vector3 worldMoveForward = transform.localToWorldMatrix * new Vector3(0, 0, moveVector.y);
        Vector3 worldMoveRight = transform.localToWorldMatrix * new Vector3(moveVector.x, 0, 0);
        float maxSpeed = sprinting ? SprintSpeed : MoveSpeed;

        if ((moveVector.x > 0 && localVelocity.x < maxSpeed) || (localVelocity.x > maxSpeed && moveVector.x < 0))
        {
            playerPhysics.velocity += worldMoveRight;
        }
        else if ((moveVector.x < 0 && localVelocity.x > -maxSpeed) || (localVelocity.x < -maxSpeed && moveVector.x > 0))
        {
            playerPhysics.velocity += worldMoveRight;
        }

        if ((moveVector.y > 0 && localVelocity.z < maxSpeed) || (localVelocity.z > maxSpeed && moveVector.y < 0))
        {
            playerPhysics.velocity += worldMoveForward;
        }
        else if ((moveVector.y < 0 && localVelocity.z > -maxSpeed) || (localVelocity.z < -maxSpeed && moveVector.y > 0))
        {
            playerPhysics.velocity += worldMoveForward;
        }
        if (flying)
        {
            if (Input.GetKey(KeyCode.Space) && playerPhysics.velocity.y < MoveSpeed)
            {
                playerPhysics.acceleration.y = FlyVerticalAccel;
            }
            else if (Input.GetKey(KeyCode.LeftControl) && playerPhysics.velocity.y > -MoveSpeed)
            {
                playerPhysics.acceleration.y = -FlyVerticalAccel;
            }
            else
            {
                playerPhysics.acceleration.y = 0;
                playerPhysics.velocity.y -= playerPhysics.velocity.y * FlyFrictionCoeff;
                if (worldMoveRight.x == 0) playerPhysics.velocity.x -= playerPhysics.velocity.x * FlyFrictionCoeff;
                if (worldMoveForward.z == 0) playerPhysics.velocity.z -= playerPhysics.velocity.z * FlyFrictionCoeff;

                if (playerPhysics.velocity.sqrMagnitude < 0.01f) playerPhysics.velocity = Vector3.zero;
            }
        }
        if (jumpTimer > JumpTimeout && Input.GetKeyDown(KeyCode.Space) && playerPhysics.hitDirections[(int)Direction.NegY])
        {
            jumpTimer = 0;
            playerPhysics.velocity.y = JumpVelocity;
        }

        if (Input.GetMouseButton(1))
        {
            player.useItem(0, playerCamera.transform.forward);
        }

        jumpTimer += Time.deltaTime;
    }
}