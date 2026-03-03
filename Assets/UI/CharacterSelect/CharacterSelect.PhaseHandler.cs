using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace Cadenza
{
    public partial class CharacterSelect
    {
        private readonly Dictionary<SelectPhase, PhaseHandler> phaseHandlers = new();

        private abstract class PhaseHandler
        {
            protected readonly CharacterSelect Owner;

            protected PhaseHandler(CharacterSelect owner)
            {
                this.Owner = owner;
            }

            public virtual void OnSubmit(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
            }

            public virtual void OnCancel(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
            }

            public virtual void OnNavigate(MoveDirection moveDirection, Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
            }

            public virtual void OnUpdate(Player player, PlayerTracker tracker, PlayerContainer playerContainer)
            {
            }
        }

        private void InitializePhaseHandlers()
        {
            // Rebuild the phase-to-handler map used by input dispatch.
            this.phaseHandlers.Clear();

            // Register handler implementations for each CharacterSelect phase.
            this.phaseHandlers[SelectPhase.None] = new NonePhaseHandler(this);
            this.phaseHandlers[SelectPhase.Joining] = new JoiningPhaseHandler(this);
            this.phaseHandlers[SelectPhase.CharacterSelection] = new CharacterSelectionPhaseHandler(this);
            this.phaseHandlers[SelectPhase.Settings] = new SettingsPhaseHandler(this);
            this.phaseHandlers[SelectPhase.Haptics] = new HapticsPhaseHandler(this);
            this.phaseHandlers[SelectPhase.CalibratingInProgress] = new CalibratingInProgressPhaseHandler(this);
            this.phaseHandlers[SelectPhase.CalibratingDone] = new CalibratingDonePhaseHandler(this);
            this.phaseHandlers[SelectPhase.PlayerNaming] = new PlayerNamingPhaseHandler(this);
            this.phaseHandlers[SelectPhase.Ready] = new ReadyPhaseHandler(this);
        }
    }
}
