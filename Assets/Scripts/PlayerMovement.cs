using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    private static PlayerMovement s_Instance;

    public static Transform PlayerTransform => s_Instance != null ? s_Instance.transform : null;

    [SerializeField] private Transform _cameraTransform;

    [SerializeField, Tooltip("Grados por segundo al girar hacia la dirección de movimiento.")]
    private float _rotationSpeed = 540f;

    [SerializeField, Tooltip("Aceleración vertical cuando no hay suelo (CharacterController).")]
    private float _gravity = -25f;

    [SerializeField, Min(0.1f), Tooltip("Altura objetivo del salto (en unidades).")]
    private float _jumpHeight = 1.5f;

    private CharacterController _characterController;
    private float _verticalVelocity;
    private PlayerStats _stats;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _stats = GetComponent<PlayerStats>();
        s_Instance = this;
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

        Vector2 input = ReadWasd();
        if (input.sqrMagnitude > 1f)
            input.Normalize();

        Vector3 flatForward = FlattenOnXZ(_cameraTransform.forward);
        Vector3 flatRight = FlattenOnXZ(_cameraTransform.right);

        Vector3 moveDirection = flatForward * input.y + flatRight * input.x;
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            moveDirection.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

        if (_characterController.isGrounded)
        {
            if (_verticalVelocity < 0f)
                _verticalVelocity = -1f;

            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }
        else
        {
            _verticalVelocity += _gravity * Time.deltaTime;
        }

        Vector3 velocity = moveDirection * _stats.GetMoveSpeed();
        velocity.y = _verticalVelocity;
        _characterController.Move(velocity * Time.deltaTime);
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
