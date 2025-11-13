using UnityEngine;

//States for enemy AI state machine

public interface IEnemy
{
    void MeleeAttack();
    void RangedAttack();
    void SpecialAttack();
    void DoDamage();
    void CheckState();
    void IdleState();
    void ChaseState();
    void MeleeState();
    void SpecialState();
    void RunState();
    void RangedState();
    bool CheckIsDead();
}
