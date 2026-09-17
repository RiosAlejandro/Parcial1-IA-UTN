using System.Collections.Generic;
using UnityEngine;

public class PointOfInterest : MonoBehaviour
{
    [SerializeField] private float _maxHealth = 30f;

    private float _health;

    public bool IsDestroyed { get; private set; }

    private static readonly List<PointOfInterest> _active = new List<PointOfInterest>();

    public static int ActiveCount => _active.Count;

    private void Awake()
    {
        _health = _maxHealth;
        _active.Add(this);
    }

    private void OnDestroy()
    {
        _active.Remove(this);
    }

    public void TakeDamage(float amount)
    {
        if (IsDestroyed) return;

        _health -= amount;

        if (_health <= 0f)
        {
            IsDestroyed = true;
            Destroy(gameObject);
        }
    }

    public static PointOfInterest FindNearestActive(Vector3 origin, float maxDistance)
    {
        PointOfInterest nearest = null;
        float nearestSqrDist = maxDistance * maxDistance;

        foreach (PointOfInterest poi in _active)
        {
            if (poi.IsDestroyed) continue;

            float sqrDist = (poi.transform.position - origin).sqrMagnitude;

            if (sqrDist <= nearestSqrDist)
            {
                nearest = poi;
                nearestSqrDist = sqrDist;
            }
        }

        return nearest;
    }
}
