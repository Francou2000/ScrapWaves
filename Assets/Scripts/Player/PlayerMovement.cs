using System;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement s_Instance;

    public static Transform PlayerTransform => s_Instance != null ? s_Instance.transform : null;

    public event Action OnJump;
    public event Action OnAirJump;
    public event Action OnLanded;
    public event Action OnCrouchStarted;
    public event Action OnCrouchEnded;
    public event Action OnSlideStarted;
    public event Action OnSlideEnded;
    public event Action OnDashStarted;
    public event Action OnDashEnded;
    public event Action<int, int> OnDashChargesChanged;

    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private float _rotationSpeed = 540f;
    [SerializeField, Min(0.1f)] private float _baseMoveAcceleration = 38f;
    [SerializeField, Min(0.1f)] private float _baseFriction = 18f;
    [SerializeField, Min(0f)] private float _groundCheckExtraDistance = 0.08f;
    [SerializeField] private LayerMask _groundMask = ~0;
    [SerializeField, Min(0f)] private float _airFrictionMultiplier = 0.4f;
    [SerializeField, Min(0f)] private float _crouchAccelerationMultiplier = 0.2f;
    [SerializeField, Min(0f)] private float _slideFrictionMultiplier = 0.4f;
    [SerializeField, Min(0.01f)] private float _dashDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float _groundedDashRegenTime = 2f;
    [SerializeField, Min(0.01f)] private float _airborneDashRegenTime = 4f;
    [SerializeField, Min(0f)] private float _slideSpeedThreshold = 5f;
    [SerializeField, Min(0f)] private float _minSlideSpeed = 2f;
    [SerializeField, Min(0f)] private float _postDashFrictionMultiplier = 0.3f;
    [SerializeField, Min(0f)] private float _postDashFrictionDuration = 0.18f;

    private Rigidbody _rb;
    private PlayerStats _stats;
    private Collider _ownCollider;

    private Vector2 _moveInput;
    private Vector3 _moveDirectionWorld;
    private bool _jumpPressed;
    private bool _crouchHeld;
    private bool _crouchPressed;
    private bool _crouchReleased;
    private bool _dashPressed;

    private bool _isGrounded;
    private bool _wasGrounded;
    private bool _isCrouching;
    private bool _isSliding;
    private bool _isDashing;

    private float _dashTimer;
    private float _dashRegenTimer;
    private float _postDashFrictionTimer;
    private int _remainingAirJumps;
    private int _currentDashCharges;

    // Cache movement components and initialize singleton and physics defaults.
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _stats = GetComponent<PlayerStats>();
        _ownCollider = GetComponent<Collider>() ?? GetComponentInChildren<Collider>();
        s_Instance = this;

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = true;
    }

    // Clear singleton when this movement instance is destroyed.
    private void OnDestroy()
    {
        if (s_Instance == this) s_Instance = null;
    }

    // Resolve camera reference and initialize jump and dash counters.
    private void Start()
    {
        if (_cameraTransform == null && Camera.main != null) _cameraTransform = Camera.main.transform;

        _isGrounded = IsGrounded();
        _wasGrounded = _isGrounded;
        _remainingAirJumps = Mathf.Max(0, _stats.GetStatInt(StatType.AirJumps));
        SyncDashChargesToStatMax();
    }

    // Read buffered player inputs and trigger stateful actions.
    private void Update()
    {
        if (_cameraTransform == null) return;

        ReadInput();

        if (_jumpPressed) TryJump();
        if (_crouchPressed) TryStartCrouchOrSlide();
        if (_crouchReleased) StopCrouchOrSlide();
        if (_dashPressed) TryDash();

        _jumpPressed = false;
        _crouchPressed = false;
        _crouchReleased = false;
        _dashPressed = false;
    }

    // Run physics movement, friction, dash timer, and grounded transitions.
    private void FixedUpdate()
    {
        if (_rb == null || _cameraTransform == null) return;

        UpdateGroundedState();

        if (_isDashing)
        {
            HandleDashTimer();
        }
        else
        {
            HandleMovement();
            ApplyPlanarSpeedCap();
            HandleFriction();
        }

        TickPostDashFrictionWindow();
        HandleDashRegeneration();
    }

    // Poll keyboard and build normalized camera-relative movement direction.
    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = ReadWasd();
        if (_moveInput.sqrMagnitude > 1f) _moveInput.Normalize();

        Vector3 flatForward = FlattenOnXZ(_cameraTransform.forward);
        Vector3 flatRight = FlattenOnXZ(_cameraTransform.right);
        _moveDirectionWorld = flatForward * _moveInput.y + flatRight * _moveInput.x;
        if (_moveDirectionWorld.sqrMagnitude > 0.0001f) _moveDirectionWorld.Normalize();

        _jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
        _dashPressed = keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame;

        bool crouchNowHeld = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
        _crouchPressed = crouchNowHeld && !_crouchHeld;
        _crouchReleased = !crouchNowHeld && _crouchHeld;
        _crouchHeld = crouchNowHeld;
    }

    // Apply acceleration force and rotate player while respecting movement state priorities.
    private void HandleMovement()
    {
        if (_moveDirectionWorld.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_moveDirectionWorld);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime);
        }

        if (_isSliding) return;

        float acceleration = _baseMoveAcceleration;
        if (_isCrouching) acceleration *= _crouchAccelerationMultiplier;

        if (_moveDirectionWorld.sqrMagnitude > 0.0001f)
        {
            float maxSpeed = Mathf.Max(0.1f, _stats.GetMoveSpeed());
            float speedRatio = Mathf.Clamp01(CurrentPlanarSpeed() / maxSpeed);
            float speedScaledAcceleration = acceleration * Mathf.Lerp(1f, 0.35f, speedRatio);
            _rb.AddForce(_moveDirectionWorld * speedScaledAcceleration, ForceMode.Acceleration);
        }
    }

    // Apply velocity-opposing friction adjusted by grounded, slide, and dash states.
    private void HandleFriction()
    {
        Vector3 planarV = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        if (planarV.sqrMagnitude <= 0.0001f) return;

        float friction = _baseFriction;
        if (!_isGrounded) friction *= _airFrictionMultiplier;
        if (_isSliding && _isGrounded) friction *= _slideFrictionMultiplier;
        if (_postDashFrictionTimer > 0f) friction *= _postDashFrictionMultiplier;

        Vector3 frictionDir = -planarV.normalized;
        _rb.AddForce(frictionDir * friction, ForceMode.Acceleration);
    }


    // Clamp horizontal velocity to movement speed stat for controlled top speed.
    private void ApplyPlanarSpeedCap()
    {
        float maxSpeed = Mathf.Max(0.1f, _stats.GetMoveSpeed());
        Vector3 planarV = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        float planarSpeed = planarV.magnitude;
        if (planarSpeed <= maxSpeed || planarSpeed <= 0.0001f) return;

        Vector3 cappedPlanar = planarV * (maxSpeed / planarSpeed);
        _rb.linearVelocity = new Vector3(cappedPlanar.x, _rb.linearVelocity.y, cappedPlanar.z);
    }

    // Decrease post-dash friction grace timer used to preserve dash momentum.
    private void TickPostDashFrictionWindow()
    {
        if (_postDashFrictionTimer <= 0f) return;
        _postDashFrictionTimer = Mathf.Max(0f, _postDashFrictionTimer - Time.fixedDeltaTime);
    }
    // Attempt jump using ground status and remaining air jumps.
    private void TryJump()
    {
        if (_isGrounded)
        {
            PerformJump(false);
            return;
        }

        if (_remainingAirJumps > 0)
        {
            _remainingAirJumps--;
            PerformJump(true);
        }
    }

    // Execute jump impulse from jump height stat and clear descending velocity.
    private void PerformJump(bool isAirJump)
    {
        float jumpHeight = Mathf.Max(0.01f, _stats.GetStat(StatType.JumpHeight));
        float g = Mathf.Abs(Physics.gravity.y);
        float jumpSpeed = Mathf.Sqrt(2f * g * jumpHeight);

        Vector3 lv = _rb.linearVelocity;
        if (lv.y < 0f) lv.y = 0f;
        _rb.linearVelocity = lv;
        _rb.AddForce(Vector3.up * (jumpSpeed * _rb.mass), ForceMode.Impulse);

        if (_isSliding)
        {
            _isSliding = false;
            OnSlideEnded?.Invoke();
        }

        if (isAirJump) OnAirJump?.Invoke();
        else OnJump?.Invoke();
    }

    // Enter crouch or slide based on grounded state and speed thresholds.
    private void TryStartCrouchOrSlide()
    {
        if (_isDashing) return;

        bool canStartSlide = CurrentPlanarSpeed() > _slideSpeedThreshold || !_isGrounded;
        if (canStartSlide)
        {
            _isSliding = true;
            _isCrouching = false;
            OnSlideStarted?.Invoke();
            return;
        }

        if (!_isCrouching)
        {
            _isCrouching = true;
            OnCrouchStarted?.Invoke();
        }
    }

    // Exit crouch and slide states when crouch input is released.
    private void StopCrouchOrSlide()
    {
        if (_isSliding)
        {
            _isSliding = false;
            OnSlideEnded?.Invoke();
        }

        if (_isCrouching)
        {
            _isCrouching = false;
            OnCrouchEnded?.Invoke();
        }
    }

    // Validate dash requirements, consume charge, and apply dash impulse.
    private void TryDash()
    {
        if (_isDashing) return;

        int maxCharges = Mathf.Max(0, _stats.GetStatInt(StatType.DashCharges));
        if (maxCharges <= 0 || _currentDashCharges <= 0) return;

        Vector3 dashDirection = _moveDirectionWorld;
        if (dashDirection.sqrMagnitude <= 0.0001f) return;

        _isDashing = true;
        _isSliding = false;
        _isCrouching = false;
        _dashTimer = _dashDuration;

        float dashSpeed = Mathf.Max(0.1f, _stats.GetStat(StatType.DashSpeed));
        Vector3 desiredVelocity = dashDirection * dashSpeed;
        Vector3 currentPlanar = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 velocityChange = desiredVelocity - currentPlanar;
        _rb.AddForce(velocityChange * _rb.mass, ForceMode.Impulse);

        _currentDashCharges = Mathf.Max(0, _currentDashCharges - 1);
        _dashRegenTimer = 0f;
        OnDashChargesChanged?.Invoke(_currentDashCharges, maxCharges);
        OnDashStarted?.Invoke();
    }

    // Advance dash timer and restore regular movement behavior after dash ends.
    private void HandleDashTimer()
    {
        _dashTimer -= Time.fixedDeltaTime;
        if (_dashTimer > 0f) return;

        _isDashing = false;
        _postDashFrictionTimer = _postDashFrictionDuration;
        OnDashEnded?.Invoke();

        if (_crouchHeld) TryStartCrouchOrSlide();
    }

    // Regenerate dash charges using grounded or airborne recharge rates.
    private void HandleDashRegeneration()
    {
        int maxCharges = Mathf.Max(0, _stats.GetStatInt(StatType.DashCharges));
        if (maxCharges <= 0)
        {
            if (_currentDashCharges != 0)
            {
                _currentDashCharges = 0;
                OnDashChargesChanged?.Invoke(_currentDashCharges, maxCharges);
            }
            return;
        }

        if (_currentDashCharges > maxCharges)
        {
            _currentDashCharges = maxCharges;
            OnDashChargesChanged?.Invoke(_currentDashCharges, maxCharges);
        }

        if (_currentDashCharges >= maxCharges) return;

        _dashRegenTimer += Time.fixedDeltaTime;
        float regenTime = _isGrounded ? _groundedDashRegenTime : _airborneDashRegenTime;
        if (_dashRegenTimer < regenTime) return;

        _dashRegenTimer = 0f;
        _currentDashCharges = Mathf.Min(maxCharges, _currentDashCharges + 1);
        OnDashChargesChanged?.Invoke(_currentDashCharges, maxCharges);
    }

    // Update grounded transitions and refresh air jumps when landing.
    private void UpdateGroundedState()
    {
        _wasGrounded = _isGrounded;
        _isGrounded = IsGrounded();

        if (_isGrounded && !_wasGrounded)
        {
            _remainingAirJumps = Mathf.Max(0, _stats.GetStatInt(StatType.AirJumps));
            OnLanded?.Invoke();
        }

        if (_isSliding && (!_crouchHeld || CurrentPlanarSpeed() < _minSlideSpeed))
        {
            _isSliding = false;
            OnSlideEnded?.Invoke();
            if (_crouchHeld)
            {
                _isCrouching = true;
                OnCrouchStarted?.Invoke();
            }
        }
    }

    // Check for floor contact using collider-aware downward raycast.
    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.02f;
        float distance = 0.2f + _groundCheckExtraDistance;

        if (_ownCollider != null)
        {
            distance = Mathf.Max(distance, _ownCollider.bounds.extents.y + _groundCheckExtraDistance);
            origin = new Vector3(transform.position.x, _ownCollider.bounds.center.y, transform.position.z);
        }

        return Physics.Raycast(origin, Vector3.down, distance, _groundMask, QueryTriggerInteraction.Ignore);
    }

    // Initialize current dash charges to stat-provided maximum.
    private void SyncDashChargesToStatMax()
    {
        int maxCharges = Mathf.Max(0, _stats.GetStatInt(StatType.DashCharges));
        _currentDashCharges = maxCharges;
        OnDashChargesChanged?.Invoke(_currentDashCharges, maxCharges);
    }

    // Return current horizontal speed ignoring vertical velocity.
    private float CurrentPlanarSpeed()
    {
        Vector3 planarV = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        return planarV.magnitude;
    }

    // Remove vertical component and normalize with a safe fallback direction.
    private static Vector3 FlattenOnXZ(Vector3 v)
    {
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f) return Vector3.forward;
        return v.normalized;
    }

    // Read normalized WASD axes as a 2D movement vector.
    private static Vector2 ReadWasd()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return Vector2.zero;

        float x = 0f;
        if (keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed) x += 1f;

        float y = 0f;
        if (keyboard.sKey.isPressed) y -= 1f;
        if (keyboard.wKey.isPressed) y += 1f;

        return new Vector2(x, y);
    }
}