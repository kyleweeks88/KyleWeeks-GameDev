using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterControls : MonoBehaviour
{
    // TEST COMMENT TESTY!!!
    #region Variables 

    public LayerMask whatIsWalkable;

    // MOVEMENT VARIABLES
    [Header("Movement")]
    public float walkSpeed;
    public float crouchSpeed;
    public float sprintSpeed;
    public float ladderClimbSpeed;
    public Transform stepCheckPos;
    public Transform wallCheckPos;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float verticalInput;
    [HideInInspector] public float horizontalInput;

    // JUMPING VARIABLES
    [Header("Jumping")]
    public float jumpForce;
    public float jumpTime;
    public int extraJumps;
    public bool isJumping;
    public bool isFalling;
    public bool isGrounded;
    private int jumpsRemaining;
    [HideInInspector] public float jumpTimeRemaining;
    public Transform groundCheck;
    public float groundCheckRadius;

    // SPRINTING VARIABLES
    [Header("Sprinting")]
    public bool isSprinting;

    // CROUCHING VARIABLES
    [Header("Crouching")]
    public LayerMask whatIsCeiling;
    public Transform ceilingCheck;
    public float ceilingCheckRadius;
    public bool standBlocked;
    public bool isCrouching;
    CapsuleCollider standCollider;

    // STATES VARIABLES
    [Header("Player States")]
    public bool obstacleInteraction;
    public bool portalInteraction;
    public bool climbingLadder;
    public bool doorIn;
    public bool doorOut;
    
    // DIRECTION VARIABLES
    [HideInInspector] public bool facingRight = true;

    // COMPONENTS
    [HideInInspector] public Rigidbody myRB;
    private Animator myAnim;
    private PlayerInput playerInput;
    [HideInInspector] public List<Collider> ragdollColliders = new List<Collider>();
    [HideInInspector] public List<Collider> controllerColliders = new List<Collider>();
    public List<Collider> collidingParts = new List<Collider>();

    [HideInInspector] public Portal currentPortal;
    [HideInInspector] public Obstacle currentObstacle;

    #endregion

    void Awake()
    {
        SetRagdollParts();
    }

    void Start()
    {
        myRB = GetComponent<Rigidbody>();
        myAnim = GetComponentInChildren<Animator>();
        standCollider = GetComponent<CapsuleCollider>();
        playerInput = GetComponent<PlayerInput>();

        jumpsRemaining = extraJumps;
        moveSpeed = walkSpeed;
    }

    void FixedUpdate() 
    { 
        // CHECKS IF CHARACTER IS ON THE GROUND
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, whatIsWalkable);
        // CHECKS IF CHARACTER IS StandBlocked BY OBSTACLE
        standBlocked = Physics.CheckSphere(ceilingCheck.position, ceilingCheckRadius, whatIsCeiling);
        
        if(isGrounded)
        {
            if(horizontalInput == 0 && verticalInput == 0)
                myRB.useGravity = false;
        }
        else
        {
            if (climbingLadder)
            {
                myRB.useGravity = false;
            }
            else
            {
                myRB.useGravity = true;
            }
        }

        // IF PLAYER IS GROUNDED, RESET jumpsRemaining
        if(isGrounded)
        {
            jumpsRemaining = extraJumps;
            jumpTimeRemaining = jumpTime;
        }

        Movement(moveSpeed);
        StepCheck();
        FallCheck();

        if(doorIn)
        {
            if(playerInput != null)
                playerInput.enabled = false;

            FaceTarget(currentPortal.pos_Rear.position);

            float distance = Vector3.Distance(transform.position, currentPortal.pos_Rear.position);
            transform.position = Vector3.Lerp(transform.position, 
                currentPortal.pos_Rear.position, Time.deltaTime * 2f);

            if(distance < 0.01f)
            {
                doorIn = false;
                StartCoroutine(currentPortal.TeleportObject(this.gameObject));
            }
        }
        
        if(doorOut)
        {
            FaceTarget(currentPortal.pos_Front.position);

            float distance = Vector3.Distance(transform.position, currentPortal.pos_Front.position);
            transform.position = Vector3.Lerp(transform.position, 
                currentPortal.pos_Front.position, Time.deltaTime * 2f);

            if(distance < 0.01f)
            {
                FaceTarget(Vector3.zero);
                doorOut = false;
                if(playerInput != null)
                    playerInput.enabled = true;
            }
        }
    }

    #region Functions

    public void Movement(float _moveSpeed)
    {
        if(!isCrouching && !isSprinting)
        {
            _moveSpeed = walkSpeed;
        }

        if (!obstacleInteraction)
        {
            myRB.velocity = new Vector3(0, myRB.velocity.y, horizontalInput * _moveSpeed);
        }
        else
        {
            if (climbingLadder)
            {
                FlipRight();
                _moveSpeed = ladderClimbSpeed;
                myRB.velocity = new Vector3(0, verticalInput * _moveSpeed, horizontalInput * _moveSpeed);
            }
            else
            {
                myRB.velocity = new Vector3(0, myRB.velocity.y, horizontalInput * _moveSpeed);
            }
        }

        // TURNING THE PLAYER
        if (!climbingLadder)
        {
            if (!facingRight && horizontalInput > 0)
            {
                FlipRight();
            }
            else if (facingRight && horizontalInput < 0)
            {
                FlipLeft();
            }
        }
    }

    public void Jumping()
    {
        if(isGrounded && !isCrouching)
        {
            isJumping = true;
            jumpTimeRemaining = jumpTime;
            //myRB.velocity = Vector2.up * jumpForce;
        }
    }

    public void HighJumping()
    {
        if(isJumping)
        {
            if (jumpTimeRemaining > 0)
            {
                myRB.velocity = Vector2.up * jumpForce;
                jumpTimeRemaining -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
    }

    public void Crouching()
    {
        if(isGrounded && !isSprinting)
        {
            if(!isCrouching)
            {
                moveSpeed = crouchSpeed;
                standCollider.enabled = false;
                isCrouching = true;       
            }
            else
            {
                if(!standBlocked)
                {
                    standCollider.enabled = true;
                    isCrouching = false;
                }
                else
                {
                    Debug.Log("Standing is Blocked!");
                }
            }
        }
    }

    public void Sprinting()
    {
        if(isSprinting)
        {
            if(isGrounded && !isCrouching)
            {
                if(horizontalInput != 0)
                {
                    moveSpeed = sprintSpeed;
                }
            }
        }
    }

    public void StepCheck()
    {
        if(horizontalInput >= 0.1 || horizontalInput <= -0.1)
        {
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            RaycastHit stepCheckHit;
            RaycastHit wallCheckHit;
            if(Physics.Raycast(wallCheckPos.position, forward, out wallCheckHit, 0.5f, whatIsWalkable))
            {
                return;
            }
            else if(Physics.Raycast(stepCheckPos.position, forward, out stepCheckHit, 0.5f, whatIsWalkable))
            {
                Debug.DrawRay(stepCheckPos.position, forward, Color.red, 1f);
                myRB.AddForce(Vector3.up * 1f, ForceMode.Impulse);
            }
        }
    }

    public void FallCheck()
    {
        if (!isGrounded && !isJumping)
        {
            RaycastHit fallCheckHit;
            if(!Physics.Raycast(transform.position, -Vector3.up, out fallCheckHit, 3f, whatIsWalkable))
            {
                isFalling = true;
                Debug.DrawRay(transform.position, -Vector3.up, Color.red, 3f);
            }
        }
        else
        {
            isFalling = false;
        }
    }

    public void FlipRight()
    {
        facingRight = true;
        this.gameObject.transform.rotation = Quaternion.Euler(0,0,0);
    }

    public void FlipLeft()
    {
        facingRight = false;
        this.gameObject.transform.rotation = Quaternion.Euler(0,180,0);
    }

    public void FaceTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, 0));
        transform.rotation = Quaternion.Lerp(transform.rotation, lookRotation, Time.deltaTime * 4f);
    }

    void SetRagdollParts()
    {
        Collider[] colliders = this.gameObject.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if(col.gameObject != this.gameObject)
            {
                col.isTrigger = true;
                ragdollColliders.Add(col);
            }
            else
            {
                controllerColliders.Add(col);
            }
        }
    }

    public void TurnOnRagdoll()
    {
        myRB.useGravity = false;
        myAnim.enabled = false;

        foreach (Collider col in controllerColliders)
        {
            col.enabled = false;    
        }

        foreach (Collider col in ragdollColliders)
        {
            col.isTrigger = false;
            col.attachedRigidbody.velocity = Vector3.zero;
        }
    }

    void OnTriggerEnter(Collider col)
    {
        if (col.tag == "Obstacle")
        {
            currentObstacle = col.gameObject.GetComponent<Obstacle>();
            currentObstacle.interactingObj = this.gameObject;
            obstacleInteraction = true;

            Debug.Log(this.gameObject.name + " entered Obstacle Trigger");
        }
        else if(col.tag == "Portal")
        {
            currentPortal = col.gameObject.GetComponent<Portal>();
            portalInteraction = true;

            Debug.Log(this.gameObject.name + " entered Portal Trigger");
        }

        if (ragdollColliders.Contains(col))
        {
            return;
        }
        CharacterControls charCTRL = col.transform.root.GetComponent<CharacterControls>();
        if(charCTRL == null)
        {
            return;
        }
        if(col.gameObject == charCTRL.gameObject)
        {
            return;
        }

        if(!collidingParts.Contains(col))
        {
            collidingParts.Add(col);
        }
    }

    void OnTriggerExit(Collider col)
    {
        if(col.tag == "Obstacle")
        {
            Obstacle obstacle = col.GetComponent<Obstacle>();

            obstacleInteraction = false;
            climbingLadder = false;
            obstacle.interactingObj = null;
        }
        else if(col.tag == "Portal")
        {
            currentPortal = null;
            portalInteraction = false;
        }

        if(collidingParts.Contains(col))
        {
            collidingParts.Remove(col);
        }
    }

    #endregion

    #region Debugging

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }

    #endregion
}
