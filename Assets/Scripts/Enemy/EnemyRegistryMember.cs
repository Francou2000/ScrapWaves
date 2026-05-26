using UnityEngine;

public class EnemyRegistryMember : MonoBehaviour
{
    private void OnEnable()
    {
        EnemyRegistry.Register(transform);
    }

    private void OnDisable()
    {
        EnemyRegistry.Unregister(transform);
    }

    private void OnDestroy()
    {
        
        EnemyRegistry.Unregister(transform);
    }
}
