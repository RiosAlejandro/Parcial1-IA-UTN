using UnityEngine;

public class HunterAttackState : State
{
    private readonly HunterAgent _hunter;
    private BoidAgent _target;

    public HunterAttackState(HunterAgent hunter, StateMachine stateMachine) : base(stateMachine)
    {
        _hunter = hunter;
    }

    public override void Enter()
    {
        _target = BoidRegistry.FindNearestAlive(_hunter.transform.position, _hunter.PerceptionRadius);

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
        if (_target == null || !_target.IsAlive)
        {
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance > _hunter.PerceptionRadius)
        {
            StateMachine.ChangeState(HunterStates.Patrol);
            return;
        }

        if (distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.MoveTowards(_target.transform.position);
            ExecuteAttack(() => _hunter.MeleeAttack(_target));
        }
        else if (distance <= _hunter.RangeAttackRadius)
        {
            ExecuteAttack(() => _hunter.RangedAttack(_target));
        }
        else
        {
            _hunter.MoveTowards(_target.transform.position);
        }
    }

    private void ExecuteAttack(System.Action attack)
    {
        attack();
        _hunter.ResetAttackTimer();
        StateMachine.ChangeState(HunterStates.Patrol);
    }
}
