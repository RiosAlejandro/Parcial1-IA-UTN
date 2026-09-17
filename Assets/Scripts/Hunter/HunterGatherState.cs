using UnityEngine;

public class HunterGatherState : State
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;
    private float _gatherTimer;
    private bool _isGathering;

    public HunterGatherState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = BoidRegistry.FindNearestDead(_hunter.transform.position, _hunter.PerceptionRadius);
        _gatherTimer = 0f;
        _isGathering = false;

        if (_target == null)
            StateMachine.ChangeState(HunterStates.Patrol);
    }

    public override void Exit()
    {
        _hunter.Stop();
        _target = null;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsDead)
        {
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (!_isGathering)
        {
            if (distance <= _hunter.MeleeAttackRadius)
            {
                _isGathering = true;
                _hunter.Stop();
            }
            else
            {
                _hunter.MoveTowards(_target.transform.position);
            }

            return;
        }

        _gatherTimer += Time.deltaTime;

        if (_gatherTimer >= _hunter.GatherDuration)
        {
            _target.Gather();
            StateMachine.ChangeState(HunterStates.Patrol);
        }
    }
}
