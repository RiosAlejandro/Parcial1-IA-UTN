using System.Collections.Generic;
using UnityEngine;

public static class BoidRegistry
{
    private static readonly List<BoidAgent> _all = new List<BoidAgent>();

    public static IReadOnlyList<BoidAgent> All => _all;

    public static void Register(BoidAgent boid)
    {
        if (!_all.Contains(boid))
            _all.Add(boid);
    }

    public static void Unregister(BoidAgent boid)
    {
        _all.Remove(boid);
    }

    public static BoidAgent FindNearestAlive(Vector3 origin, float radius) =>
        FindNearest(origin, radius, boid => boid.IsAlive);

    public static BoidAgent FindNearestDead(Vector3 origin, float radius) =>
        FindNearest(origin, radius, boid => boid.IsDead);

    private static BoidAgent FindNearest(Vector3 origin, float radius, System.Func<BoidAgent, bool> predicate)
    {
        BoidAgent nearest = null;
        float nearestSqrDist = radius * radius;

        foreach (BoidAgent boid in _all)
        {
            if (!predicate(boid)) continue;

            float sqrDist = (boid.transform.position - origin).sqrMagnitude;

            if (sqrDist <= nearestSqrDist)
            {
                nearest = boid;
                nearestSqrDist = sqrDist;
            }
        }

        return nearest;
    }
}
