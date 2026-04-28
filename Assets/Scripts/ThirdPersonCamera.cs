using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private Transform _followTarget;

    [SerializeField, Tooltip("Posición de la cámara respecto al pivote, en el espacio de la órbita (yaw/pitch). Típico: Y altura, Z negativo = detrás.")]
    private Vector3 _followOffset = new Vector3(0f, 1.7f, -4.2f);

    [SerializeField, Tooltip("Punto al que mira la cámara, relativo al target (suele ser pecho/cabeza).")]
    private Vector3 _lookAtOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField, Tooltip("Escala del movimiento horizontal del ratón (yaw).")]
    private float _horizontalSensitivity = 0.12f;

    [SerializeField, Tooltip("Escala del movimiento vertical del ratón (pitch).")]
    private float _verticalSensitivity = 0.12f;

    [SerializeField, Tooltip("Si está activo, mover el ratón hacia arriba inclina la vista hacia abajo (eje vertical invertido).")]
    private bool _invertVertical;

    [SerializeField, Tooltip("Límite inferior del pitch (mirar hacia abajo).")]
    private float _minPitch = -40f;

    [SerializeField, Tooltip("Límite superior del pitch (mirar hacia arriba).")]
    private float _maxPitch = 55f;

    [SerializeField] private bool _lockCursorOnPlay = true;

    private float _yaw;
    private float _pitch;

    /// <summary>Cuando es true (p. ej. menú de mejoras), no se aplica el ratón a la órbita y el cursor queda libre para la UI.</summary>
    private bool _lookBlockedByUi;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _pitch = NormalizeEulerPitch(euler.x);
        _yaw = euler.y;

        if (_lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>Llama <see cref="UpgradeManager"/> al mostrar/ocultar la UI de elección. Bloquea la órbita y libera el cursor.</summary>
    public void SetLookBlockedByUi(bool blocked)
    {
        if (blocked == _lookBlockedByUi)
            return;

        _lookBlockedByUi = blocked;

        if (blocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (_lockCursorOnPlay)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        if (_followTarget == null)
            return;

        Mouse mouse = Mouse.current;
        if (!_lookBlockedByUi && mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * _horizontalSensitivity;
            float verticalSign = _invertVertical ? 1f : -1f;
            _pitch += verticalSign * delta.y * _verticalSensitivity;
            _pitch = Mathf.Clamp(_pitch, _minPitch, _maxPitch);
        }

        Quaternion yawRot = Quaternion.AngleAxis(_yaw, Vector3.up);
        Vector3 pitchAxis = yawRot * Vector3.right;
        Quaternion pitchRot = Quaternion.AngleAxis(_pitch, pitchAxis);
        Quaternion orbit = yawRot * pitchRot;

        Vector3 pivot = _followTarget.position;
        transform.position = pivot + orbit * _followOffset;

        Vector3 lookPoint = pivot + _lookAtOffset;
        transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
    }

    private static float NormalizeEulerPitch(float x)
    {
        if (x > 180f)
            x -= 360f;
        return x;
    }
}
