namespace Cadenza
{
    public partial class CharacterSelect
    {
        private sealed class NonePhaseHandler : PhaseHandler
        {
            public NonePhaseHandler(CharacterSelect owner)
                : base(owner)
            {
            }
        }
    }
}
