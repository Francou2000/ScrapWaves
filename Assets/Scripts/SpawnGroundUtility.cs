using UnityEngine;

/// <summary>
/// Ancla el pie (transform del <see cref="CharacterController"/>) a superficie bajo un punto XZ
/// mediante raycast, y reduce penetración contra colliders sólidos con pasos pequeños.
/// </summary>
public static class SpawnGroundUtility
{
    private const int MaxOverlapHits = 24;
    private static readonly Collider[] OverlapBuffer = new Collider[MaxOverlapHits];
    private const int MaxRaycastHits = 32;
    private static readonly RaycastHit[] RaycastBuffer = new RaycastHit[MaxRaycastHits];

    /// <summary>
    /// Usa <paramref name="candidateXZ"/> en XZ; ignora su Y. Si el raycast principal falla y
    /// <paramref name="fallbackGroundRaycastMask"/> no es 0, se intenta un segundo raycast.
    /// </summary>
    public static bool TryResolveFootPosition(
        Vector3 candidateXZ,
        Transform spawnRoot,
        CharacterController characterController,
        float referenceYForRayStart,
        float maxAbsDeltaYFromReference,
        LayerMask primaryGroundRaycastMask,
        LayerMask fallbackGroundRaycastMask,
        LayerMask overlapSolidMask,
        float raycastStartHeight,
        float raycastMaxDistance,
        float surfaceSeparation,
        int maxProjectionIterations,
        float resolveStepUp,
        float resolveStepOut,
        out Vector3 footWorldPosition)
    {
        footWorldPosition = candidateXZ;

        if (spawnRoot == null || characterController == null)
            return false;

        Vector3 xz = new Vector3(candidateXZ.x, 0f, candidateXZ.z);
        Vector3 rayOrigin = new Vector3(xz.x, referenceYForRayStart + raycastStartHeight, xz.z);

        if (!TryRaycastGroundPreferHeight(rayOrigin, raycastMaxDistance, primaryGroundRaycastMask, referenceYForRayStart, maxAbsDeltaYFromReference, out RaycastHit groundHit))
        {
            if (fallbackGroundRaycastMask.value == 0)
                return false;

            if (!TryRaycastGroundPreferHeight(rayOrigin, raycastMaxDistance, fallbackGroundRaycastMask, referenceYForRayStart, maxAbsDeltaYFromReference, out groundHit))
                return false;
        }

        footWorldPosition = groundHit.point + groundHit.normal * surfaceSeparation;
        spawnRoot.position = footWorldPosition;

        for (int i = 0; i < maxProjectionIterations; i++)
        {
            if (!TryGetDepenetrationDelta(spawnRoot, characterController, overlapSolidMask, out Vector3 delta))
                return true;

            Vector3 dir = delta;
            if (dir.sqrMagnitude > 0.0001f)
                dir.Normalize();
            else
                dir = Vector3.up;

            footWorldPosition += dir * resolveStepOut + Vector3.up * resolveStepUp;
            spawnRoot.position = footWorldPosition;
        }

        return !TryGetDepenetrationDelta(spawnRoot, characterController, overlapSolidMask, out _);
    }

    private static bool TryRaycastGroundPreferHeight(
        Vector3 origin,
        float maxDistance,
        LayerMask mask,
        float referenceY,
        float maxAbsDeltaY,
        out RaycastHit hit)
    {
        hit = default;

        int count = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            RaycastBuffer,
            maxDistance,
            mask,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
            return false;

        float bestAbsDy = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < count; i++)
        {
            float absDy = Mathf.Abs(RaycastBuffer[i].point.y - referenceY);
            if (maxAbsDeltaY > 0f && absDy > maxAbsDeltaY)
                continue;

            if (absDy < bestAbsDy)
            {
                bestAbsDy = absDy;
                bestIndex = i;
            }
        }

        // Si no hubo ningún hit dentro del umbral vertical, igual elegimos el más cercano en Y.
        // La idea es "priorizar" la altura, no bloquear el spawn.
        if (bestIndex < 0 && maxAbsDeltaY > 0f)
        {
            bestAbsDy = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float absDy = Mathf.Abs(RaycastBuffer[i].point.y - referenceY);
                if (absDy < bestAbsDy)
                {
                    bestAbsDy = absDy;
                    bestIndex = i;
                }
            }
        }

        if (bestIndex < 0)
            return false;

        hit = RaycastBuffer[bestIndex];
        return true;
    }

    private static bool TryGetDepenetrationDelta(
        Transform spawnRoot,
        CharacterController cc,
        LayerMask overlapSolidMask,
        out Vector3 delta)
    {
        delta = Vector3.zero;

        GetWorldCapsule(spawnRoot, cc, out Vector3 p0, out Vector3 p1, out float r);

        int count = Physics.OverlapCapsuleNonAlloc(
            p0,
            p1,
            r,
            OverlapBuffer,
            overlapSolidMask,
            QueryTriggerInteraction.Ignore);

        if (count <= 0)
            return false;

        Vector3 capsuleCenter = (p0 + p1) * 0.5f;
        Vector3 sum = Vector3.zero;
        int used = 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = OverlapBuffer[i];
            if (col == null)
                continue;

            if (col.transform == spawnRoot || col.transform.IsChildOf(spawnRoot))
                continue;

            // Collider.ClosestPoint falla con MeshCollider no convexo.
            // Para el spawn, una aproximación basada en bounds es suficiente para empujar fuera.
            Vector3 away = capsuleCenter - col.bounds.center;
            away.y = 0f;
            float sqr = away.sqrMagnitude;
            if (sqr < 1e-8f)
            {
                away = Random.onUnitSphere;
                away.y = 0f;
                sqr = away.sqrMagnitude;
            }

            if (sqr > 1e-8f)
                sum += away / Mathf.Sqrt(sqr);
            used++;
        }

        if (used == 0)
            return false;

        delta = sum;
        return true;
    }

    private static void GetWorldCapsule(Transform spawnRoot, CharacterController cc, out Vector3 p0, out Vector3 p1, out float r)
    {
        Vector3 worldCenter = spawnRoot.TransformPoint(cc.center);
        Vector3 up = spawnRoot.up.normalized;

        Vector3 lossy = spawnRoot.lossyScale;
        float scaleY = Mathf.Abs(lossy.y) < 1e-4f ? 1f : Mathf.Abs(lossy.y);
        float scaleH = Mathf.Max(Mathf.Abs(lossy.x), Mathf.Abs(lossy.z));
        if (scaleH < 1e-4f)
            scaleH = 1f;

        r = cc.radius * scaleH;
        float half = Mathf.Max(cc.height * scaleY * 0.5f - r, 0.001f);

        p0 = worldCenter - up * half;
        p1 = worldCenter + up * half;
    }
}
