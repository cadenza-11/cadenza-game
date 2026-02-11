namespace Cadenza
{
    public interface IAttackArea
    {
        void SetActive(bool enabled);
        void StartAttack(Character character);
        void EndAttack();
    }
}
