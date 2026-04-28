using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-34)]
public class XPPool : MonoBehaviour
{
    [SerializeField, Tooltip("Prefab raíz con XPDrop + XPPoolMember + collider opcional.")]
    private GameObject _xpOrbPrefab;

    [SerializeField, Tooltip("Padre de los clones; si es null, se crea un contenedor en runtime.")]
    private Transform _container;

    [SerializeField, Min(1)] private int _initialPoolSize = 128;

    [SerializeField] private bool _allowPoolGrowth = true;

    [SerializeField, Min(1)] private int _maxPoolSize = 2048;

    private readonly Queue<GameObject> _inactive = new Queue<GameObject>();
    private readonly List<GameObject> _instances = new List<GameObject>();

    private int _leasedCount;

    private Transform _runtimeParent;
    private bool _ownsRuntimeParent;

    public int ActiveLeasedCount => _leasedCount;
    public int TotalPooledInstances => _instances.Count;

    /// <summary>Último <see cref="XPPool"/> activo en escena (registrado en <see cref="OnEnable"/>).</summary>
    public static XPPool Instance { get; private set; }

    /// <summary>Resuelve el pool: <see cref="Instance"/> o, si hace falta, un <see cref="Object.FindAnyObjectByType{T}"/>.</summary>
    public static XPPool GetInstance()
    {
        if (Instance != null)
            return Instance;
        return FindAnyObjectByType<XPPool>();
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    private Transform GetPoolParent()
    {
        if (_container != null)
            return _container;
        EnsureRuntimeParentExists();
        return _runtimeParent;
    }

    private void EnsureRuntimeParentExists()
    {
        if (_container != null || _runtimeParent != null)
            return;

        var holder = new GameObject($"[PooledXP] {gameObject.name}");
        _runtimeParent = holder.transform;
        _ownsRuntimeParent = true;

        Scene targetScene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
        SceneManager.MoveGameObjectToScene(holder, targetScene);
    }

    private void Awake()
    {
        if (_container == null)
            EnsureRuntimeParentExists();

        if (_xpOrbPrefab == null)
        {
            Debug.LogError("XPPool: asigna prefab de bolita de XP.", this);
            return;
        }

        if (_maxPoolSize < _initialPoolSize)
            _maxPoolSize = _initialPoolSize;

        for (int i = 0; i < _initialPoolSize; i++)
            CreateInstance(enqueueInactive: true);
    }

    private void OnDestroy()
    {
        if (_ownsRuntimeParent && _runtimeParent != null)
            Destroy(_runtimeParent.gameObject);
    }

    private void OnValidate()
    {
        if (_maxPoolSize < _initialPoolSize)
            _maxPoolSize = _initialPoolSize;
    }

    /// <summary>Activa una bolita en la posición con la cantidad de XP indicada.</summary>
    public bool TrySpawn(Vector3 worldPosition, int experienceAmount)
    {
        if (_xpOrbPrefab == null || experienceAmount <= 0)
            return false;

        GameObject instance = TryGet();
        if (instance == null)
            return false;

        instance.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);

        XPDrop drop = instance.GetComponent<XPDrop>();
        if (drop == null)
        {
            Release(instance);
            return false;
        }

        drop.ActivateFromPool(this, experienceAmount);
        return true;
    }

    private GameObject TryGet()
    {
        if (_xpOrbPrefab == null)
            return null;

        GameObject instance;

        if (_inactive.Count > 0)
            instance = _inactive.Dequeue();
        else if (_allowPoolGrowth && _instances.Count < _maxPoolSize)
            instance = CreateInstance(enqueueInactive: false);
        else
            return null;

        ActivateInstance(instance);
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null || !instance.activeSelf)
            return;

        XPPoolMember member = instance.GetComponent<XPPoolMember>();
        if (member == null || !member.BelongsTo(this))
            return;

        instance.SetActive(false);
        instance.transform.SetParent(GetPoolParent(), false);
        _leasedCount--;
        _inactive.Enqueue(instance);
    }

    private GameObject CreateInstance(bool enqueueInactive)
    {
        EnsureRuntimeParentExists();

        GameObject instance = Instantiate(_xpOrbPrefab);
        instance.name = $"{_xpOrbPrefab.name} (pool)";

        Scene targetScene = gameObject.scene.IsValid() ? gameObject.scene : SceneManager.GetActiveScene();
        SceneManager.MoveGameObjectToScene(instance, targetScene);

        instance.transform.SetParent(GetPoolParent(), false);

        XPPoolMember member = instance.GetComponent<XPPoolMember>();
        if (member == null)
            member = instance.AddComponent<XPPoolMember>();
        member.Bind(this);

        instance.SetActive(false);
        _instances.Add(instance);

        if (enqueueInactive)
            _inactive.Enqueue(instance);

        return instance;
    }

    private void ActivateInstance(GameObject instance)
    {
        instance.SetActive(true);
        _leasedCount++;
    }
}
