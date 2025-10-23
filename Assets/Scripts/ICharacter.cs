using UnityEngine;

public interface ICharacter
{
    int currentHealth { get; set; }
    int specialMeter { get; set; }

    void Move(Vector2 input);
    void WeakAttack();
    void StrongAttack();
    void SpecialAttack();
    void StartTeamAttk();
    void JoinTeamAttk();
    void DoDamage();
}
