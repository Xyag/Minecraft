using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    public float MoveAccel = 25;
    public float MoveSpeed = 10;
    public float JumpVelocity = 5;
    public float JumpFixDist = 0.1f;
    public float JumpTimeout = 0.05f;
    public float GroundRayCastDist = 0.05f;
    public int GroundLayer = 8;

    public WorldManager worldManager;

    new private Collider collider;
    new private Rigidbody rigidbody;
    private float jumpTimer = 0;
    private RaycastHit raycastHit;
    private int groundLayerMask;

    protected void Awake()
    {
        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        jumpTimer = JumpTimeout;
        groundLayerMask = 1 << GroundLayer;
    }

    protected void Update()
    {
        Vector2 moveVector = new Vector2(-Time.deltaTime * MoveAccel * Input.GetAxis("Vertical"), Time.deltaTime * MoveAccel * Input.GetAxis("Horizontal"));
        Vector3 localVelocity = transform.worldToLocalMatrix * rigidbody.velocity;

        if ((moveVector.x > 0 && localVelocity.x < MoveSpeed) || (localVelocity.x > MoveSpeed && moveVector.x < 0))
        {
            rigidbody.AddRelativeForce(moveVector.x, 0, 0, ForceMode.VelocityChange);
        }
        else if ((moveVector.x < 0 && localVelocity.x > -MoveSpeed) || (localVelocity.x < -MoveSpeed && moveVector.x > 0))
        {
            rigidbody.AddRelativeForce(moveVector.x, 0, 0, ForceMode.VelocityChange);
        }

        if ((moveVector.y > 0 && localVelocity.z < MoveSpeed) || (localVelocity.z > MoveSpeed && moveVector.y < 0))
        {
            rigidbody.AddRelativeForce(0, 0, moveVector.y, ForceMode.VelocityChange);
        }
        else if ((moveVector.y < 0 && localVelocity.z > -MoveSpeed) || (localVelocity.z < -MoveSpeed && moveVector.y > 0))
        {
            rigidbody.AddRelativeForce(0, 0, moveVector.y, ForceMode.VelocityChange);
        }
        if (jumpTimer > JumpTimeout && Input.GetKeyDown(KeyCode.Space) && Physics.Raycast(transform.position, Vector3.down, GroundRayCastDist, groundLayerMask))
        {
            jumpTimer = 0;
            transform.position += Vector3.up * JumpFixDist;
            rigidbody.AddForce(localVelocity.x, JumpVelocity, localVelocity.z, ForceMode.VelocityChange);
        }

        jumpTimer += Time.deltaTime;
    }
}