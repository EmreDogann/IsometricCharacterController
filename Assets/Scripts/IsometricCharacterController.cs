using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
using static Utilities.SpringMotion;

[SelectionBase]
public class IsometricCharacterController : MonoBehaviour
{
    [Header("References")]
    public CharacterController controller;
    public Transform playerMesh;
    public Transform parachute;

    public PlayerInput playerInput;
    private PlayerInputActions playerInputActions;

    private ControlType currentControlType;
    private enum ControlType
    {
        KeyboardAndMouse,
        Gamepad,
    }

    [Header("General Controls")]
    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float speed = 12.0f;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float turnSpeed = 10.0f;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float turnRotationSpeed = 45.0f;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float tiltAngle = 15.0f;

    [Header("Jump Settings")]
    [SerializeField]
    private float gravity = -9.81f;

    [SerializeField]
    [Range(0.0f, 30.0f)]
    private float glideDrag = 4.0f;

    [SerializeField]
    [Range(0.0f, 30.0f)]
    private float terminalVelocity = 15.0f;

    [SerializeField]
    [Range(0.0f, 30.0f)]
    private float jumpHeight = 3.0f;

    [SerializeField]
    [Range(0.0f, 10.0f)]
    private float jumpVelocityFalloff = 2.5f;

    [SerializeField]
    [Range(1.0f, 10.0f)]
    private float fallMultiplier = 2.5f;

    [SerializeField]
    [Range(1.0f, 10.0f)]
    private float lowJumpMultiplier = 2.0f;

    [Space]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float jumpBufferTime;
    private float jumpBufferCounter;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float coyoteTime;
    private float coyoteTimeCounter;

    [Header("Input Damping")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float inputDampingRotation = 0.1f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float inputDampingMovementBasic = 0.1f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float inputDampingMovementAccel = 0.1f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float inputDampingMovementDecel = 0.05f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float inputDampingMovementTurn = 0.1f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float midAirDampingMove = 0.15f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float midAirDampingRot = 0.15f;

    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float glideDamping = 0.15f;

    [SerializeField]
    [Range(0.0f, 50.0f)]
    private float glideSpringFrequency = 0.15f;

    private Vector3 gravityVector;

    private Vector3 velocity;
    private Vector3 velocityAfterGlideStart;
    private float additionalVelocity;
    private Vector3 prevVelocity;

    private Vector3 inputVectorRot;
    private Vector3 inputVelocityRot;
    private Vector3 inputVectorMove;
    private Vector3 inputVelocityMove;

    private Vector3 glideVector;
    private Vector3 glideVelocity;

    private bool isGrounded;
    private bool isJumping;
    private bool isGliding;
    private bool wantToGlide;

    private float currentDampingMove;
    private float currentDampingRot;

    private float time;

    private void Awake()
    {
        playerInputActions = new PlayerInputActions();
        getCurrentControlType(playerInput);
    }

    private void OnEnable()
    {
        playerInput.onControlsChanged += onControlsChanged;
        playerInputActions.Player.Enable();

        playerInputActions.Player.Move.started += MoveStarted;
        playerInputActions.Player.Glide.canceled += GlideCanceled;
    }

    private void OnDisable()
    {
        playerInput.onControlsChanged -= onControlsChanged;
        playerInputActions.Player.Move.started -= MoveStarted;
        playerInputActions.Player.Glide.canceled -= GlideCanceled;
        playerInputActions.Player.Disable();
    }

    void Update()
    {
        // Check if grounded.
        isGrounded = controller.isGrounded;
        currentDampingRot = inputDampingRotation;

        // WASD movement.
        Vector3 input = new Vector3(playerInputActions.Player.Move.ReadValue<Vector2>().x, 0.0f, playerInputActions.Player.Move.ReadValue<Vector2>().y);

        if (!playerInputActions.Player.Move.IsInProgress() && isGrounded) // Decelerating
        {
            currentDampingMove = inputDampingMovementDecel;
        }

        if (playerInputActions.Player.Move.IsInProgress() && inputVectorMove.toIso().normalized != Vector3.zero)
        {
            float dot = Vector3.Dot(input.toIso().normalized, inputVectorMove.toIso().normalized);
            if (dot > 0.0f)
            {
                currentDampingMove = isGrounded ? inputDampingMovementBasic : inputDampingMovementBasic + midAirDampingMove;
                currentDampingRot = isGrounded ? currentDampingRot : inputDampingRotation + midAirDampingRot;
            }
            else if (dot <= 0.0f)
            {
                currentDampingMove = isGrounded ? inputDampingMovementTurn : inputDampingMovementTurn + midAirDampingMove;
                currentDampingRot = isGrounded ? currentDampingRot : inputDampingRotation;
            }
        }
        if (!playerInputActions.Player.Move.IsInProgress() && inputVectorMove.magnitude < 0.01f)
        {
            currentDampingMove = 0;
        }

        // Smoothly interpolate rotation.
        inputVectorRot = Vector3.SmoothDamp(inputVectorRot, input, ref inputVelocityRot, currentDampingRot, isGrounded ? Mathf.Infinity : 6);

        // Smoothly interpolate movement.
        inputVectorMove = Vector3.SmoothDamp(inputVectorMove, input, ref inputVelocityMove, currentDampingMove, turnSpeed);

        //Debug.DrawRay(transform.position, input.toIso() * 3, Color.green);
        //Debug.DrawRay(transform.position, inputVectorMove.toIso() * 3, Color.red);
        //Debug.DrawRay(transform.position, inputVelocityMove.toIso() * 3, Color.blue);
        Debug.DrawRay(transform.position, velocity, Color.yellow);
        Debug.DrawRay(transform.position, glideVector, Color.red);

        // Don't move the player if not movement buttons are being pressed and the movement is not being smoothed.
        if (inputVectorRot != Vector3.zero)
        {
            // .toIso() transforms the input vector to align with the isometric view.
            // LookRotation line will rotate player around the global up axis. This might cause problems when
            // implementing climbing.
            Quaternion targetRot = Quaternion.LookRotation(inputVectorRot.toIso(), Vector3.up); // Returns target rotation.
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnRotationSpeed * Time.deltaTime);

            //Calculates the tilt based on user input
            //Quaternion targetTilt = Quaternion.Euler(inputVectorRot.magnitude * tiltAngle, 0.0f, (inputVelocityRot.x * 0.2f) * -Mathf.Sign(inputVectorRot.z) * tiltAngle);
            Quaternion targetTilt = Quaternion.Euler(
                !isGliding ? inputVectorRot.magnitude * tiltAngle : 0.0f,
                0.0f,
                (inputVelocityRot.magnitude * 0.2f) *
                (Mathf.Abs(Vector3.Dot(inputVectorRot.normalized, inputVelocityRot.normalized)) >= 0.95f ? 0 : 1) *
                -angleDir(inputVectorRot.normalized, inputVelocityRot.normalized, transform.up) *
                tiltAngle
            );
            playerMesh.rotation = Quaternion.RotateTowards(playerMesh.rotation, targetRot * targetTilt, 100.0f * Time.deltaTime);
        }
        velocity.x = inputVectorMove.toIso().x * speed;
        velocity.z = inputVectorMove.toIso().z * speed;

        // Keep history of grounded in the last x seconds. Called Coyote time.
        coyoteTimeCounter -= Time.deltaTime;
        if (isGrounded)
        {
            if (isGliding || wantToGlide) stopGlide();
            else if (isJumping)
            {
                isJumping = false;
                playerMesh.gameObject.GetComponent<PlayerAnimation>().Deactivate();
            }
            coyoteTimeCounter = coyoteTime;

            if (velocity.y < 0.0f)
            {
                velocity.y = 0.0f;
            }
        }

        // Jump Calculations
        jumpBufferCounter -= Time.deltaTime;
        if (playerInputActions.Player.Jump.WasPressedThisFrame())
        {
            jumpBufferCounter = jumpBufferTime;
        }

        // Delay glide activate until apex of jump.
        if (playerInputActions.Player.Glide.WasPressedThisFrame())
        {
            if (!isGrounded && coyoteTimeCounter < 0.0f)
            {
                wantToGlide = true;
            }
        }

        if (jumpBufferCounter > 0.0f && coyoteTimeCounter > 0.0f)
        {
            isJumping = true;

            jumpBufferCounter = 0.0f;
            coyoteTimeCounter = 0.0f;
            velocity.y = jumpHeight;

            playerMesh.gameObject.GetComponent<PlayerAnimation>().Activate();
        }

        // Activate Glide
        if (wantToGlide && !isGliding && !isGrounded && jumpBufferCounter < 0.0f && coyoteTimeCounter < 0.0f && velocity.y < 0.0f)
        {
            wantToGlide = false;
            isGliding = true;
            parachute.gameObject.GetComponent<ParachuteAnimation>().Activate();

            glideVector.y = (-gravity * fallMultiplier) - (velocity.y / glideDamping);
            velocityAfterGlideStart = velocity;
            additionalVelocity = 0.0f;
        }


        // Apply gravity.
        if (velocity.y > 0.0f && !playerInputActions.Player.Jump.IsPressed() && !isGliding)
        {
            gravityVector.y = (gravity * lowJumpMultiplier);
        }
        else if (velocity.y < jumpVelocityFalloff)
        {
            gravityVector.y = (gravity * fallMultiplier);
        }
        else
        {
            gravityVector.y = gravity;
        }

        prevVelocity = glideVector;
        // Apply gliding drag force.
        if (isGliding)
        {
            CalcDampedSimpleHarmonicMotion(ref velocity.y, ref glideVelocity.y, -glideDrag, Time.deltaTime, glideSpringFrequency, glideDamping);
            // Round to remove floating point precision errors (maybe from Time.deltaTime accumulation?).
            velocity.y = Mathf.Round(velocity.y * 1000000.0f) * 0.000001f;
        }
        else
        {
            velocity.y += (gravityVector.y) * Time.deltaTime;
        }

        velocity.y = Mathf.Clamp(velocity.y, -terminalVelocity, terminalVelocity);

        controller.Move(velocity * Time.deltaTime);
    }

    // Test is the targetDir is pointing either on the left or right side of a transform relative to its forward.
    float angleDir(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 right = Vector3.Cross(up, fwd); // right vector
        float dir = Vector3.Dot(right, targetDir);

        if (dir > 0f)
        {
            return 1f;
        }
        else if (dir < 0f)
        {
            return -1f;
        }
        else
        {
            return 0f;
        }
    }

    // Not used
    private void getCurrentControlType(PlayerInput input)
    {
        if (input.currentControlScheme == "Gamepad")
        {
            currentControlType = ControlType.Gamepad;
        }
        else if (input.currentControlScheme == "Keyboard&Mouse")
        {
            currentControlType = ControlType.KeyboardAndMouse;
        }
    }

    private void MoveStarted(InputAction.CallbackContext ctx)
    {
        currentDampingMove = inputDampingMovementAccel; // When starting to move.
    }

    private void GlideCanceled(InputAction.CallbackContext ctx)
    {
        stopGlide();
    }

    private void stopGlide()
    {
        isGliding = false;
        wantToGlide = false;
        parachute.gameObject.GetComponent<ParachuteAnimation>().Deactivate();
        glideVector.y = 0.0f;
    }

    private void onControlsChanged(PlayerInput obj)
    {
        getCurrentControlType(obj);
    }
}
