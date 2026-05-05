using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement s_Instance;

    public static Transform PlayerTransform => s_Instance != null ? s_Instance.transform : null;

    [SerializeField] private Transform _cameraTransform;

    [SerializeField, Tooltip("Grados por segundo al girar hacia la dirección de movimiento.")]
    private float _rotationSpeed = 540f;

    [SerializeField, Min(0.1f), Tooltip("Altura objetivo del salto (en unidades).")]
    private float _jumpHeight = 1.5f;

    [SerializeField, Min(0.01f), Tooltip("Aceleración aplicada en XZ al moverse (AddForce con ForceMode.Acceleration).")]
    private float _moveAcceleration = 55f;

    [SerializeField, Min(0.1f), Tooltip("Factor para frenar cuando no hay input (0 = no frena extra).")]
    private float _brakeAcceleration = 25f;

    [SerializeField, Min(0f), Tooltip("Distancia extra del raycast para considerar grounded.")]
    private float _groundCheckExtraDistance = 0.08f;

    [SerializeField, Tooltip("Capas consideradas suelo para grounded (por defecto: todo).")]
    private LayerMask _groundMask = ~0;

    private Rigidbody _rb;
    private PlayerStats _stats;
    private Vector2 _moveInput;
    private Vector3 _moveDirectionWorld;
    private bool _jumpRequested;
    private Collider _ownCollider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _stats = GetComponent<PlayerStats>();
        s_Instance = this;

        _ownCollider = GetComponent<Collider>();
        if (_ownCollider == null)
            _ownCollider = GetComponentInChildren<Collider>();

        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.freezeRotation = true;
    }

    private void OnDestroy()
    {
        if (s_Instance == this)
            s_Instance = null;
    }

    private void Start()
    {
        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (_cameraTransform == null)
            return;

        Keyboard keyboard = Keyboard.current;

        _moveInput = ReadWasd();
        if (_moveInput.sqrMagnitude > 1f)
            _moveInput.Normalize();

        Vector3 flatForward = FlattenOnXZ(_cameraTransform.forward);
        Vector3 flatRight = FlattenOnXZ(_cameraTransform.right);

        _moveDirectionWorld = flatForward * _moveInput.y + flatRight * _moveInput.x;
        if (_moveDirectionWorld.sqrMagnitude > 0.0001f)
        {
            _moveDirectionWorld.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(_moveDirectionWorld);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            _jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        if (_rb == null || _cameraTransform == null)
            return;

        float maxSpeed = _stats != null ? _stats.GetMoveSpeed() : 7f;

        Vector3 v = _rb.linearVelocity;
        Vector3 planarV = new Vector3(v.x, 0f, v.z);

        if (_moveDirectionWorld.sqrMagnitude > 0.0001f)
        {
            _rb.AddForce(_moveDirectionWorld * _moveAcceleration, ForceMode.Acceleration);

            // Cap planar speed using existing move speed stat (no changes to upgrades/stats).
            Vector3 newPlanarV = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            float sqr = newPlanarV.sqrMagnitude;
            float maxSqr = maxSpeed * maxSpeed;
            if (sqr > maxSqr && sqr > 0.0001f)
            {
                float scale = maxSpeed / Mathf.Sqrt(sqr);
                Vector3 capped = newPlanarV * scale;
                _rb.linearVelocity = new Vector3(capped.x, _rb.linearVelocity.y, capped.z);
            }
        }
        else if (_brakeAcceleration > 0f && planarV.sqrMagnitude > 0.0001f)
        {
            Vector3 brakeDir = -planarV.normalized;
            _rb.AddForce(brakeDir * _brakeAcceleration, ForceMode.Acceleration);
        }

        if (_jumpRequested)
        {
            _jumpRequested = false;
            if (IsGrounded())
            {
                float g = Mathf.Abs(Physics.gravity.y);
                float jumpSpeed = Mathf.Sqrt(2f * g * Mathf.Max(0.01f, _jumpHeight));

                Vector3 lv = _rb.linearVelocity;
                if (lv.y < 0f)
                    lv.y = 0f;
                _rb.linearVelocity = lv;

                _rb.AddForce(Vector3.up * (jumpSpeed * _rb.mass), ForceMode.Impulse);
            }
        }
    }

    private bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.02f;
        float distance = 0.2f + _groundCheckExtraDistance;

        if (_ownCollider != null)
        {
            // Use bounds extents to adapt to collider size.
            distance = Mathf.Max(distance, _ownCollider.bounds.extents.y + _groundCheckExtraDistance);
            origin = new Vector3(transform.position.x, _ownCollider.bounds.center.y, transform.position.z);
        }

        return Physics.Raycast(origin, Vector3.down, distance, _groundMask, QueryTriggerInteraction.Ignore);
    }

    private static Vector3 FlattenOnXZ(Vector3 v)
    {
        v.y = 0f;
        if (v.sqrMagnitude < 0.0001f)
            return Vector3.forward;
        return v.normalized;
    }

    private static Vector2 ReadWasd()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return Vector2.zero;

        float x = 0f;
        if (keyboard.aKey.isPressed)
            x -= 1f;
        if (keyboard.dKey.isPressed)
            x += 1f;

        float y = 0f;
        if (keyboard.sKey.isPressed)
            y -= 1f;
        if (keyboard.wKey.isPressed)
            y += 1f;

        return new Vector2(x, y);
    }
}
