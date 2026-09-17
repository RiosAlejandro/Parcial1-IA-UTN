using UnityEngine;

public class HunterPatrolState : State
{
    private readonly HunterAgent _hunter;
    private int _currentWaypoint;
    private int _direction = 1;
    private float _poiTimer;

    public HunterPatrolState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _poiTimer = 0f;
    }

    public override void Exit()
    {
        _hunter.Stop();
    }

    public override void Update()
    {
        if (TryTransitionToGather()) return;
        if (TryTransitionToAttack()) return;

        Patrol();
        UpdatePointOfInterestSpawn();
    }

    private bool TryTransitionToGather()
    {
        BoidAgent deadBoid = BoidRegistry.FindNearestDead(_hunter.transform.position, _hunter.PerceptionRadius);
        if (deadBoid == null) return false;

        StateMachine.ChangeState(HunterStates.Gather);
        return true;
    }

    private bool TryTransitionToAttack()
    {
        if (!_hunter.IsAttackReady) return false;

        BoidAgent boid = BoidRegistry.FindNearestAlive(_hunter.transform.position, _hunter.PerceptionRadius);
        if (boid == null) return false;

        StateMachine.ChangeState(HunterStates.Attack);
        return true;
    }

    private void Patrol()
    {
        HunterPatrolData data = _hunter.PatrolData;
        if (data == null || data.waypoints == null || data.waypoints.Length == 0) return;

        Transform waypoint = data.waypoints[_currentWaypoint];
        _hunter.MoveTowards(waypoint.position);

        if (Vector3.Distance(_hunter.transform.position, waypoint.position) <= data.waypointThreshold)
            AdvanceWaypoint(data);
    }

    private void AdvanceWaypoint(HunterPatrolData data)
    {
        if (data.waypoints.Length < 2) return;

        if (data.pingPong)
        {
            _currentWaypoint += _direction;

            if (_currentWaypoint >= data.waypoints.Length)
            {
                _currentWaypoint = data.waypoints.Length - 2;
                _direction = -1;
            }
            else if (_currentWaypoint < 0)
            {
                _currentWaypoint = 1;
                _direction = 1;
            }
        }
        else
        {
            _currentWaypoint = (_currentWaypoint + 1) % data.waypoints.Length;
        }
    }

    private void UpdatePointOfInterestSpawn()
    {
        _poiTimer += Time.deltaTime;
        if (_poiTimer < _hunter.PoiSpawnInterval) return;

        _poiTimer = 0f;
        _hunter.SpawnPointOfInterest();
    }
}

[System.Serializable]
public class HunterPatrolData
{
    public Transform[] waypoints;
    public float waypointThreshold = 0.3f;
    public bool pingPong = false;
}
