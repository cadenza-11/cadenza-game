using UnityEngine;

public interface ICharacter
{
    Transform Transform { get; }
    int currentHealth { get; set; }
    int specialMeter { get; set; }

    void Move(Vector2 input);
    void LightAttack();
    void StrongAttack();
    void SpecialAttack();
    void StartTeamAttk();
    void JoinTeamAttk();
    void DoDamage();
}
