using UnityEngine;

public static class OrbitalSpawnPlacement
{
    private static readonly string[] DirectionLabels = { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };

    public static string GetDirectionLabel(int directionIndex)
    {
        if (directionIndex < 0 || directionIndex >= DirectionLabels.Length)
            return "?";
        return DirectionLabels[directionIndex];
    }

    public static float GetDirectionAngleDegrees(int directionIndex)
    {
        return directionIndex * 45f;
    }

    public static int PickRandomDirectionIndex()
    {
        return Random.Range(0, 8);
    }

    public static bool TrySpawnAtOrbitalPoint(
        Transform player,
        GameObject prefab,
        int directionIndex,
        float minRadius,
        float maxRadius,
        float spawnHeightOffset,
        LayerMask groundRaycastMask,
        LayerMask fallbackGroundRaycastMask,
        LayerMask overlapSolidMask,
        float raycastStartHeight,
        float raycastMaxDistance,
        float maxAbsSpawnSurfaceDeltaY,
        float surfaceSeparation,
        int maxProjectionIterations,
        float resolveStepUp,
        float resolveStepOut,
        out GameObject instance,
        out Vector3 spawnPosition,
        out string placementLog)
    {
        instance = null;
        spawnPosition = Vector3.zero;
        placementLog = "no player or prefab";

        if (player == null || prefab == null)
            return false;

        float angleRad = GetDirectionAngleDegrees(directionIndex) * Mathf.Deg2Rad;
        float radius = Random.Range(minRadius, maxRadius);
        Vector3 offset = new Vector3(
            Mathf.Sin(angleRad) * radius,
            spawnHeightOffset,
            Mathf.Cos(angleRad) * radius);

        Vector3 ringPos = player.position + offset;
        instance = Object.Instantiate(prefab);
        Transform root = instance.transform;

        CharacterController cc = instance.GetComponent<CharacterController>();
        if (cc == null)
        {
            root.SetPositionAndRotation(ringPos, Quaternion.identity);
            spawnPosition = ringPos;
        }
        else if (!SpawnGroundUtility.TryResolveFootPosition(
                     new Vector3(ringPos.x, 0f, ringPos.z),
                     root,
                     cc,
                     ringPos.y,
                     maxAbsSpawnSurfaceDeltaY,
                     groundRaycastMask,
                     fallbackGroundRaycastMask,
                     overlapSolidMask,
                     raycastStartHeight,
                     raycastMaxDistance,
                     surfaceSeparation,
                     maxProjectionIterations,
                     resolveStepUp,
                     resolveStepOut,
                     out Vector3 foot))
        {
            Object.Destroy(instance);
            instance = null;
            placementLog = "ground resolve failed";
            return false;
        }
        else
        {
            root.SetPositionAndRotation(foot, Quaternion.identity);
            spawnPosition = foot;
        }

        Vector3 toPlayer = player.position - root.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.0001f)
            root.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);

        placementLog = $"dir={GetDirectionLabel(directionIndex)} angle={GetDirectionAngleDegrees(directionIndex):0}° pos={spawnPosition}";
        return true;
    }
}
