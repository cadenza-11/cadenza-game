namespace Cadenza
{
    public interface IAttackArea
    {
        void SetActive(bool enabled);
        void StartLightAttack(Character character);
        void StartHeavyAttack(Character character);
        void EndAttack();
    }
}
