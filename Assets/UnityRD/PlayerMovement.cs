using UnityEngine;
using System.Collections;

[SelectionBase]
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [System.Serializable]
    public class MouseLook
    {
        public Transform playerTransform;
        public Transform cameraTransform;

        [Tooltip("Rotations per second, given in degrees.")]
        [SerializeField]
        private float rotationsPerSecond = 68.8f;
        
        public float sensitivity = 1;

        public void Update()
        {
            float mod = rotationsPerSecond * sensitivity * Time.deltaTime;

            // player is affected by Y rotation
            playerTransform.Rotate(Vector3.up, Input.GetAxisRaw("Mouse X") * mod);

            // camera is affected by X rotation
            cameraTransform.Rotate(Vector3.left, Input.GetAxisRaw("Mouse Y") * mod);
        }
    }
    public MouseLook lookSettings;
    public PlayerMovementMecanim mecanimBridge;

    private Rigidbody attachedRigidbody;
    private Collider attachedCollider;

    private bool wishJump = false;

    public bool isGrounded { get; set; }

    public float jumpForce = 2f;

    public float minGroundAngle = 60.0f;
    public float groundFriction;
    private Vector3 avgGroundNormal;

    public Vector3 lastVelocity;

    public float groundAcceleration;
    public float maxGroundVelocity;

    public float airAcceleration;
    public float maxAirVelocity;

    // Returns the final velocity of the player after accelerating in a certain direction.
    private Vector3 Accelerate(Vector3 wishDir, Vector3 prevVelocity, float accelerate, float maxVelocity)
    {
        float projectVel = Vector3.Dot(prevVelocity, wishDir);
        float accelerationVel = accelerate * Time.fixedDeltaTime;  // match fixed time step

        // cap acceleration vector
        if (projectVel + accelerationVel > maxVelocity)
            accelerationVel = maxVelocity - projectVel;

        return prevVelocity + wishDir * accelerationVel;
    }

    // Returns the final velocity of the player after accelerating on the ground.
    private Vector3 MoveGround(Vector3 wishDir, Vector3 prevVelocity)
    {
        // apply friction if was moving
        float speed = prevVelocity.magnitude;
        if (speed != 0) // To avoid divide by zero errors
        {
            // decelerate due to friction
            float drop = speed * groundFriction * Time.fixedDeltaTime;

            // scale the velocity based on friction.
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // be careful to not drop below zero
            
        }

        return Accelerate(wishDir, prevVelocity, groundAcceleration, maxGroundVelocity);
    }
    
    // Returns the final velocity of the player after accelerating mid-air.
    private Vector3 MoveAir(Vector3 accelDir, Vector3 prevVelocity)
    {
        return Accelerate(accelDir, prevVelocity, airAcceleration, maxAirVelocity);
    }

    private Vector3 Jump(Vector3 prevVelocity)
    {
        wishJump = false;
        return isGrounded ? prevVelocity + (Vector3.up * jumpForce) : prevVelocity;
    }

    // Returns true if the player is grounded.
    bool CheckGrounded()
    {
        RaycastHit hitInfo;

        const float groundRayLength = 1.1f;
        Debug.DrawRay(transform.position, Vector3.down * groundRayLength, Color.green);

        // TODO: adjust raycast by collider size
        if(Physics.Raycast(new Ray(transform.position, Vector3.down), out hitInfo, groundRayLength))
        {
            float angle = Vector3.Angle(Vector3.forward, hitInfo.normal);
            return angle > minGroundAngle;
        }

        return false;
    }

    // Unity Events
    void Start()
    {
        attachedRigidbody = GetComponent<Rigidbody>();
        attachedCollider = GetComponent<Collider>();
        mecanimBridge = new PlayerMovementMecanim();
        mecanimBridge.anim = GetComponentInChildren<Animator>();
    }
    void FixedUpdate()
    {
        // determine if the player is grounded
        isGrounded = CheckGrounded();

        // retrieve player input
        // TODO: Refactor this into a PlayerController that feeds in input!
        Vector3 playerInput = new Vector3(Input.GetAxisRaw("Horizontal"),
                                          0,
                                          Input.GetAxisRaw("Vertical"));

        // transform player movement into world-space
        playerInput = transform.TransformVector(playerInput);

        // determine final velocity
        Vector3 finalPlayerVelocity = isGrounded ? MoveGround(playerInput, attachedRigidbody.velocity) :
                                                   MoveAir(playerInput, attachedRigidbody.velocity);
        
        // handle jump, if requested                                    
        if (wishJump)
            finalPlayerVelocity = Jump(finalPlayerVelocity);

        // assign final velocity
        attachedRigidbody.velocity = lastVelocity = finalPlayerVelocity;

        mecanimBridge.Update(this);
    }
    void Update()
    {
        // perform mouselook
        lookSettings.Update();

        // preserve existing jump request or do so when jump button is depressed
        wishJump = wishJump || Input.GetButtonDown("Jump");
    }
}
