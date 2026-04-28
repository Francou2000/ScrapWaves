using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    private static readonly List<Transform> _activeEnemies = new List<Transform>(256);

    public static int ActiveCount => _activeEnemies.Count;

    public static void Register(Transform enemyTransform)
    {
        if (enemyTransform == null)
            return;

        for (int i = 0; i < _activeEnemies.Count; i++)
        {
            if (_activeEnemies[i] == enemyTransform)
                return;
        }

        _activeEnemies.Add(enemyTransform);
    }

    public static void Unregister(Transform enemyTransform)
    {
        if (enemyTransform == null)
            return;

        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] == enemyTransform)
            {
                _activeEnemies.RemoveAt(i);
                return;
            }
        }
    }

    public static bool TryGetClosestOnPlane(Vector3 from, float range, out Transform closest)
    {
        closest = null;
        if (range <= 0f)
            return false;

        float rangeSqr = range * range;
        float bestSqr = float.MaxValue;

        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            Transform t = _activeEnemies[i];
            if (t == null)
            {
                _activeEnemies.RemoveAt(i);
                continue;
            }

            Vector3 delta = t.position - from;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr > rangeSqr || sqr >= bestSqr)
                continue;

            bestSqr = sqr;
            closest = t;
        }

        return closest != null;
    }
}
