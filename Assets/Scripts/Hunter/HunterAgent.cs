using UnityEngine;

public enum HunterStates { Patrol, Attack, Gather }

public class HunterAgent : Agent
{
    [Header("Movimiento")]
    [SerializeField] private float _moveSpeed = 3.5f;

    [Header("Percepcion")]
    [SerializeField] private float _perceptionRadius = 8f;

    [Header("Ataque")]
    [SerializeField] private float _tba = 3f;
    [SerializeField] private float _rangeAttackRadius = 5f;
    [SerializeField] private float _meleeAttackRadius = 1.2f;
    [SerializeField] private float _meleeDamage = 34f;
    [SerializeField] private float _rangedDamage = 20f;

    [Header("Patrol")]
    [SerializeField] private HunterPatrolData _patrolData;
    [SerializeField] private GameObject _pointOfInterestPrefab;
    [SerializeField] private float _poiSpawnInterval = 6f;
    [SerializeField] private int _maxActivePoi = 5;

    [Header("Gather")]
    [SerializeField] private float _gatherDuration = 2f;

    public float PerceptionRadius => _perceptionRadius;
    public float TBA => _tba;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public HunterPatrolData PatrolData => _patrolData;
    public float PoiSpawnInterval => _poiSpawnInterval;
    public float GatherDuration => _gatherDuration;
    public bool IsAttackReady => _tbaTimer >= _tba;

    private StateMachine _stateMachine;
    private float _tbaTimer;

    private void Awake()
    {
        _tbaTimer = _tba;

        _stateMachine = new StateMachine();
        _stateMachine.RegisterState(HunterStates.Patrol, new HunterPatrolState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Attack, new HunterAttackState(this, _stateMachine));
        _stateMachine.RegisterState(HunterStates.Gather, new HunterGatherState(this, _stateMachine));

        _stateMachine.ChangeState(HunterStates.Patrol);
    }

    private void Update()
    {
        _tbaTimer += Time.deltaTime;
        _stateMachine.Update();
    }

    public void ResetAttackTimer() => _tbaTimer = 0f;

    public void MoveTowards(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            Stop();
            return;
        }

        direction.Normalize();
        _velocity = direction * _moveSpeed;

        transform.position += _velocity * Time.deltaTime;
        transform.forward = direction;
    }

    public void Stop()
    {
        _velocity = Vector3.zero;
    }

    public void SpawnPointOfInterest()
    {
        if (_pointOfInterestPrefab == null || Bounds.Instance == null) return;
        if (PointOfInterest.ActiveCount >= _maxActivePoi) return;

        Instantiate(_pointOfInterestPrefab, Bounds.Instance.GetRandomPoint(), Quaternion.identity);
    }

    public void MeleeAttack(BoidAgent target) => target.TakeDamage(_meleeDamage);

    public void RangedAttack(BoidAgent target) => target.TakeDamage(_rangedDamage);

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
    }
}
