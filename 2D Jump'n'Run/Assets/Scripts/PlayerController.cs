using UnityEngine;
using UnityEngine.InputSystem;
public class PlayerController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float runSpeed = 4f; 
    //[SerializeField] private float maxSpeed = 60f;
    [SerializeField] private float airAcceleration = 0.8f;

    [Header("Vertical Movement")]
    [SerializeField] private float jumpPowerStatic = 11f;
    
    [SerializeField] private bool canMultipleJumps = false;
    [SerializeField] private int maxJumps = 2;
    
    [SerializeField] private bool canDynamicJump = false; // Varied jump heights.
    [SerializeField] private float jumpPowerDynamic = 12f;
    [SerializeField] private float jumpPowerReduction = 0.5f; // Jump's power reduction while jump button is not pressed.
    
    [SerializeField] private bool canBufferJump; // Buffer combined with static jumps better.
    [SerializeField] private float jumpBuffer = 0.2f;
    
    [SerializeField] private bool canCoyoteTimeJump;
    [SerializeField] private float jumpCoyoteTime = 0.2f;
    
    [SerializeField] private float fallMultiplier = 0.5f;

    [Header("Wall Movement")]
    [SerializeField] private bool canWallSlide = false;
    [SerializeField] private float slideResistance = 0.1f;
    
    [SerializeField] private bool canWallJump = false;
    [SerializeField] private float jumpPowerWallX = 11f;
    [SerializeField] private float jumpPowerWallY = 11f;

    [Header("States (Not serialized)")]
    [SerializeField] private bool isFacingRight;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isWalled;
    [SerializeField] private bool isWallSliding;
    [SerializeField] private bool isJumping;
    [SerializeField] private int jumpNumber;
    [SerializeField] private int jumpCount;
    [SerializeField] private float jumpBufferCountDown;
    [SerializeField] private float jumpCoyoteTimeCount;
    
    [SerializeField] private float groundCheckDelay = 0.4f;
    [SerializeField] private float groundCheckDelayCount;
    
    [SerializeField] private bool contextStartedJump;
    [SerializeField] private bool alreadyTriggered; // contextStartedJump
    [SerializeField] private bool contextCanceledJump;

    [Header("Components")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.31f;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckRadius = 0f;

    private Rigidbody2D rb2D;
    private Vector2 moveDirection;
    private int runState;
    private float jumpPower;
    private Vector2 vecGravity;
    private bool hasAlreadyBeenInitialized; // jumpCoyoteTimeCount

    
    /// <summary>
    /// Activates components.
    /// </summary>
    private void Awake()
    {
        rb2D = GetComponent<Rigidbody2D>();
        vecGravity = new Vector2(0, -Physics2D.gravity.y);
        isFacingRight = true;
        isJumping = false;
        isWallSliding = false;
    }

    /// <summary>
    /// Depending on Fixed Timestep (Default: Time interval 0.02 seconds, 50 updates per second).
    /// </summary>
    private void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        isWalled = Physics2D.OverlapCircle(wallCheck.position, wallCheckRadius, wallLayer);

        JumpPhysics();
        FallPhysics();
    }

    /// <summary>
    /// Input for the horizontal movement.
    /// </summary>
    /// <param name="context"></param>
    public void OnMove(InputAction.CallbackContext context)
    {
        moveDirection = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Input for the jump.
    /// </summary>
    /// <param name="context"></param>
    public void OnJump(InputAction.CallbackContext context)
    {
        contextStartedJump = context.started;
        contextCanceledJump = context.canceled;
    }

    /// <summary>
    /// Input for the run.
    /// </summary>
    /// <param name="context"></param>
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.started == true) runState = 1;
        if (context.canceled == true) runState = 0;
    }

    /// <summary>
    /// Update method.
    /// </summary>
    private void Update()
    {
        Flip();
        WallSlidePhysics();
        HorizontalMovement();
        Jump();
    }

    /// <summary>
    /// Properties of the mechanic horizontal movement.
    /// </summary>
    private void HorizontalMovement()
    {
        float acceleration = isGrounded ? 1 : airAcceleration;

        float speed = walkSpeed + runState * runSpeed;

        float horizontalVelocity = Mathf.Lerp(rb2D.velocity.x, moveDirection.x * speed * acceleration, Time.deltaTime * 10f);

        rb2D.velocity = new Vector2(horizontalVelocity, rb2D.velocity.y);
        /*
        if (moveDirection.x < 0 || moveDirection.x > 0)  // the query is required, because moveDirection can be implement up or down
        {
            // if velocity is bigger as max speed or is running, speed = max speed
            if (rb2D.velocity.x >= maxSpeed || rb2D.velocity.x <= -maxSpeed || isRunning)
                rb2D.velocity = new Vector2(moveDirection.x * acceleration * maxSpeed * Time.deltaTime, rb2D.velocity.y); // max speed
            else
                rb2D.AddForce(moveDirection * walkSpeed * acceleration * Time.deltaTime, ForceMode2D.Impulse);
        }
        */
    }

    /// <summary>
    /// Properties of the mechanic jump.
    /// </summary>
    private void Jump()
    {
        // Buffer
        if (canBufferJump && rb2D.velocity.y < 0) 
        {
            if (contextStartedJump) jumpBufferCountDown = jumpBuffer;

            jumpBufferCountDown -= Time.deltaTime;
        }

        // CoyoteTime
        if (canCoyoteTimeJump && rb2D.velocity.y < 0 && isJumping == false && !isWallSliding)  
        {
            if (hasAlreadyBeenInitialized == false) // The initializing of jumpCoyoteTimeCount should run once. 
            {
                jumpCoyoteTimeCount = jumpCoyoteTime;
                hasAlreadyBeenInitialized = true;
            }
            
            jumpCoyoteTimeCount -= Time.deltaTime;
        }
        else hasAlreadyBeenInitialized = false;

        // Jump events
        if (jumpCount < maxJumps && !isWallSliding &&
            /* Buffer */                ((canBufferJump && jumpBufferCountDown > 0f && isGrounded) ||
            /* Ground Jump */               (isGrounded && contextStartedJump && jumpCount < maxJumps) ||
            /* Air Jump */            (canMultipleJumps && contextStartedJump && jumpCount < maxJumps) ||
            /* CoyoteTime */         (canCoyoteTimeJump && contextStartedJump && jumpCoyoteTimeCount > 0f)))
        {
            if (alreadyTriggered == false) // So that if-query is not run again, because 'contextStarted = true'
            { //                                can be being implemented in more as 1 loop.
                isJumping = true;
                jumpCount++;
                rb2D.velocity = new Vector2(rb2D.velocity.x, jumpPower);
                alreadyTriggered = true;
            }
        }
        else alreadyTriggered = false;

        // Dynamic jump
        if (canDynamicJump) 
        {
            if (rb2D.velocity.y > 0f && contextCanceledJump)
            {
                rb2D.velocity = new Vector2(rb2D.velocity.x, rb2D.velocity.y * jumpPowerReduction);
            }
        }

        // Wall Jump Bounce while Sliding.
        if (canWallJump) 
        {
            if (isWallSliding && contextStartedJump)
            {
                if (isFacingRight)
                    rb2D.velocity = new Vector2(-jumpPowerWallX, jumpPowerWallY);
                else
                    rb2D.velocity = new Vector2(jumpPowerWallX, jumpPowerWallY);
            }
        }
    }

    /// <summary>
    /// Physics of the mechanic jump.
    /// </summary>
    private void JumpPhysics()
    {
        if (isJumping) groundCheckDelayCount += Time.deltaTime;

        if (groundCheckDelayCount >= groundCheckDelay && isGrounded || isWalled) 
        {
            jumpCount = 0;
            isJumping = false;
            groundCheckDelayCount = 0;
        }

        if (canMultipleJumps == false) maxJumps = 1;

        if (canDynamicJump == false) // Static or dynamic jump.
            jumpPower = jumpPowerStatic; 
        else jumpPower = jumpPowerDynamic; 
    }

    /// <summary>
    /// Scales the transform of player for the flip.
    /// </summary>
    private void Flip()
    {
        if (isFacingRight && moveDirection.x < 0f || !isFacingRight && moveDirection.x > 0f)
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1f;
            transform.localScale = localScale;
        }
    }

    /// <summary>
    /// Physics of the wall slide.
    /// </summary>
    private void WallSlidePhysics()
    {
        if (canWallSlide && isWalled && moveDirection.x != 0)
        {
            isWallSliding = true;
            rb2D.velocity = new Vector2(0, rb2D.velocity.y * slideResistance);
        }
        else isWallSliding = false;
    }

    /// <summary>
    /// Physics of the fall.
    /// </summary>
    private void FallPhysics()
    {
        if (rb2D.velocity.y < 0) // Fall in the air
        {
            rb2D.velocity -= vecGravity * fallMultiplier * Time.deltaTime;
        }
    }
}

    /* OnEnable OnDisable Collision
    [SerializeField]
    private float speed = 0.1f, maxSpeed, jumpForce;

    public Rigidbody2D _RB2D;

    //float _movement;

    private float _moveDirection;

    private float _jumpDirection;

    private bool isGrounded = true;

    //private PlayerInputAction playerControls;

    //private float move;

    public InputActionReference Move;

    public InputActionReference Jump;

    private void Awake()
    {
        _RB2D = GetComponent<Rigidbody2D>();
        //playerControls = new PlayerInputAction();
    }

    private void OnEnable()
    {
        Jump.Enable();
        jumpAction.performed += OnJump; // Event abonnieren
    }

    private void OnDisable()
    {
        jumpAction.performed -= OnJump; // Event abmelden
        jumpAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (isGrounded) // Nur springen, wenn der Spieler auf dem Boden ist
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false; // Spieler ist jetzt in der Luft
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Spieler ist auf dem Boden, wenn er eine Kollision mit dem Boden hat
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
    */

    /* AddForce
    public void OnMove(InputAction.CallbackContext context)
    {
        move = playerControls.Land.Move.ReadValue<float>();

        if (move == 1)
        {
            if (_RB2D.velocity.x < maxSpeed)
            {
                _RB2D.AddForce(Vector2.right * speed * Time.deltaTime);
            }
        }

        if (move == -1)
        {
            if (_RB2D.velocity.x > -maxSpeed)
            {
                _RB2D.AddForce(Vector2.left * speed * Time.deltaTime);
            }
        }
    }
    */

  /*  // ChatGPT
    // Unity C# Script for 2D Jump'n'Run Character Controller
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class CharacterController2D : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;

    [Header("Jump Settings")]
    public float jumpForce = 10f;
    public float maxJumpHoldTime = 0.2f;
    public bool enableDoubleJump = true;
    public bool enableCoyoteTime = true;
    public bool enableWallJump = true;
    public bool enableWallSlide = true;
    public float coyoteTimeDuration = 0.2f;
    public float wallSlideSpeed = 2f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private Collider2D col;
    private bool isGrounded;
    private bool isJumping;
    private bool canDoubleJump;
    private bool isWallSliding;
    private bool jumpBuffered;
    private float jumpBufferTime = 0.2f;
    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private float jumpHoldCounter;
    private int wallDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    void Update()
    {
        HandleInput();
        CheckGrounded();
        HandleCoyoteTime();
        HandleJumpBuffer();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleWallSlide();
    }

    private void HandleInput()
    {
        // Jump buffering
        if (Input.GetButtonDown("Jump"))
        {
            jumpBuffered = true;
            jumpBufferCounter = jumpBufferTime;
        }
        if (Input.GetButtonUp("Jump"))
        {
            jumpBuffered = false;
        }
    }

    private void HandleMovement()
    {
        float moveInput = Input.GetAxis("Horizontal");
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        rb.velocity = new Vector2(moveInput * targetSpeed, rb.velocity.y);

        if (jumpBuffered && (isGrounded || coyoteTimeCounter > 0))
        {
            Jump();
        }

        if (enableDoubleJump && jumpBuffered && !isGrounded && canDoubleJump && !isWallSliding)
        {
            Jump();
            canDoubleJump = false;
        }

        if (jumpBuffered && isWallSliding && enableWallJump)
        {
            WallJump();
        }

        if (Input.GetButton("Jump") && isJumping && jumpHoldCounter < maxJumpHoldTime)
        {
            jumpHoldCounter += Time.deltaTime;
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    private void CheckGrounded()
    {
        isGrounded = Physics2D.OverlapBox(col.bounds.center, col.bounds.size, 0f, groundLayer);
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTimeDuration;
            canDoubleJump = true;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }

    private void HandleCoyoteTime()
    {
        if (!enableCoyoteTime)
        {
            coyoteTimeCounter = 0;
        }
    }

    private void HandleJumpBuffer()
    {
        if (jumpBuffered)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (jumpBufferCounter <= 0)
            {
                jumpBuffered = false;
            }
        }
    }

    private void HandleWallSlide()
    {
        if (!enableWallSlide) return;

        RaycastHit2D wallCheck = Physics2D.Raycast(transform.position, Vector2.right * Mathf.Sign(rb.velocity.x), 0.5f, groundLayer);

        if (wallCheck.collider != null && !isGrounded && rb.velocity.y < 0)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
            wallDirection = (int)Mathf.Sign(wallCheck.point.x - transform.position.x);
        }
        else
        {
            isWallSliding = false;
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        isJumping = true;
        jumpHoldCounter = 0;
        jumpBuffered = false;
    }

    private void WallJump()
    {
        rb.velocity = new Vector2(-wallDirection * runSpeed, jumpForce);
        isWallSliding = false;
        jumpBuffered = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
} */

     






