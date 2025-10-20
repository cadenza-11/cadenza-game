using UnityEngine;
using UnityEngine.InputSystem;
public interface ICharacter
{
    int currentHealth { get; set; }
    int specialMeter { get; set; }
    void WeakAttack(InputAction.CallbackContext context);
    void StrongAttack(InputAction.CallbackContext context);
    void SpecialAttack();
    void StartTeamAttk();
    void JoinTeamAttk();
    void DoDamage();
}
