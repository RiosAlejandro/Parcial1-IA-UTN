using System.Collections;
using UnityEngine;

public class BoidAgent : Agent
{
    private enum BoidState { Alive, Dead, Gathered }

    [Header("Movimiento")]
    [SerializeField] private float _maxSpeed = 4f;
    [SerializeField] private float _maxSteering = 4f;

    [Header("Flocking - Separation")]
    [SerializeField] private float _separationRadius = 1.5f;
    [SerializeField, Range(0f, 3f)] private float _separationWeight = 1.5f;

    [Header("Flocking - Alignment y Cohesion")]
    [SerializeField] private float _flockRadius = 4f;
    [SerializeField, Range(0f, 3f)] private float _alignmentWeight = 1f;
    [SerializeField, Range(0f, 3f)] private float _cohesionWeight = 1f;

    [Header("Evade")]
    [SerializeField] private FieldOfViewSensor _hunterSensor;

    [Header("Puntos de interes")]
    [SerializeField] private float _poiDetectionRadius = 10f;
    [SerializeField] private float _interactDistance = 0.6f;
    [SerializeField] private float _interactInterval = 1f;
    [SerializeField] private float _damagePerInteraction = 10f;
    [SerializeField] private float _slowingDistance = 2.5f;
    [SerializeField] private float _minArriveDistance = 0.4f;

    [Header("Vida")]
    [SerializeField] private float _maxHealth = 100f;

    [Header("Respawn")]
    [SerializeField] private float _respawnDelay = 5f;

    private BoidState _state = BoidState.Alive;
    private float _health;
    private PointOfInterest _currentTarget;
    private float _interactTimer;

    public bool IsAlive => _state == BoidState.Alive;
    public bool IsDead => _state == BoidState.Dead;
    public bool IsVisible => _state != BoidState.Gathered;

    private void Awake()
    {
        _health = _maxHealth;
        BoidRegistry.Register(this);

        Vector3 randomDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        _velocity = randomDirection.normalized * _maxSpeed;
        transform.forward = _velocity;
    }

    private void OnDestroy()
    {
        BoidRegistry.Unregister(this);
    }

    private void Update()
    {
        if (_state != BoidState.Alive) return;

        UpdatePointOfInterestTarget();
        TryInteractWithPointOfInterest();

        _velocity += SteeringVector();
        _velocity.y = 0f;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);

        transform.position += _velocity * Time.deltaTime;
        transform.position = Bounds.Instance.OutOfBounds(transform.position);

        if (_velocity != Vector3.zero)
            transform.forward = _velocity;
    }

    private Vector3 SteeringVector()
    {
        if (_hunterSensor != null && _hunterSensor.CanSeeTarget && _hunterSensor.Target != null)
            return Evade(_hunterSensor.Target);

        Vector3 steering = Flocking();

        if (_currentTarget != null)
            steering += Arrive(_currentTarget.transform.position);

        return steering;
    }

    // ===================== Flocking =====================

    private Vector3 Flocking()
    {
        return CalculateSeparation() * _separationWeight
             + CalculateAlignment() * _alignmentWeight
             + CalculateCohesion() * _cohesionWeight;
    }

    private Vector3 CalculateSeparation()
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (BoidAgent other in BoidRegistry.All)
        {
            if (other == this || !other.IsVisible) continue;

            Vector3 offset = other.transform.position - transform.position;

            if (offset.sqrMagnitude <= _separationRadius * _separationRadius)
            {
                desired += offset;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(-desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateAlignment()
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (BoidAgent other in BoidRegistry.All)
        {
            if (other == this || !other.IsAlive) continue;

            if (InRange(other.transform.position, _flockRadius))
            {
                desired += other.Velocity;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return CalculateSteering(desired.normalized * _maxSpeed);
    }

    private Vector3 CalculateCohesion()
    {
        Vector3 desired = Vector3.zero;
        int count = 0;

        foreach (BoidAgent other in BoidRegistry.All)
        {
            if (other == this || !other.IsAlive) continue;

            if (InRange(other.transform.position, _flockRadius))
            {
                desired += other.transform.position;
                count++;
            }
        }

        if (count == 0) return Vector3.zero;
        desired /= count;

        return Seek(desired);
    }

    private bool InRange(Vector3 pos, float radius) => (pos - transform.position).sqrMagnitude <= radius * radius;

    // ===================== Steering basico =====================

    private Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = desired - _velocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }

    private Vector3 DesiredVector(Vector3 target) => (target - transform.position).normalized * _maxSpeed;

    private Vector3 Seek(Vector3 target) => CalculateSteering(DesiredVector(target));

    private Vector3 Flee(Vector3 target) => CalculateSteering(-DesiredVector(target));

    private Vector3 Arrive(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        float distance = direction.magnitude;

        if (distance < _minArriveDistance)
            return Vector3.zero;

        float desiredSpeed = Mathf.Min(_maxSpeed * (distance / _slowingDistance), _maxSpeed);
        Vector3 desired = direction.normalized * desiredSpeed;

        return CalculateSteering(desired);
    }

    private Vector3 Evade(Transform target)
    {
        Vector3 futurePosition = CalculateFuture(target);
        return Flee(futurePosition);
    }

    private Vector3 CalculateFuture(Transform target)
    {
        IVelocityProvider velocityProvider = target.GetComponent<IVelocityProvider>();
        Vector3 targetVelocity = velocityProvider != null ? velocityProvider.Velocity : Vector3.zero;

        Vector3 direction = target.position - transform.position;
        float prediction = direction.magnitude / (_maxSpeed + targetVelocity.magnitude);

        return target.position + targetVelocity * prediction;
    }

    // ===================== Puntos de interes =====================

    private void UpdatePointOfInterestTarget()
    {
        if (_currentTarget != null && !_currentTarget.IsDestroyed) return;

        _currentTarget = PointOfInterest.FindNearestActive(transform.position, _poiDetectionRadius);
        _interactTimer = 0f;
    }

    private void TryInteractWithPointOfInterest()
    {
        if (_currentTarget == null) return;

        float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (distance > _interactDistance) return;

        _interactTimer += Time.deltaTime;
        if (_interactTimer < _interactInterval) return;

        _interactTimer = 0f;
        _currentTarget.TakeDamage(_damagePerInteraction);

        if (_currentTarget.IsDestroyed)
            _currentTarget = null;
    }

    // ===================== Vida / muerte / respawn =====================

    public void TakeDamage(float amount)
    {
        if (_state != BoidState.Alive) return;

        _health -= amount;

        if (_health <= 0f)
        {
            _health = 0f;
            _state = BoidState.Dead;
            _velocity = Vector3.zero;
        }
    }

    public void Gather()
    {
        if (_state != BoidState.Dead) return;

        _state = BoidState.Gathered;
        SetVisible(false);
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);

        _health = _maxHealth;
        _velocity = Vector3.zero;
        transform.position = Bounds.Instance.GetRandomPoint();
        _currentTarget = null;

        SetVisible(true);
        _state = BoidState.Alive;
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;

        foreach (Collider collider in GetComponentsInChildren<Collider>())
            collider.enabled = visible;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _flockRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _poiDetectionRadius);
    }
}


