namespace Cadenza
{
    public interface IState
    {
        void Enter(Character character);
        void Exit(Character character);
        void Update(Character character);
        void FixedUpdate(Character character);
    }
}
