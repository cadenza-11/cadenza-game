namespace Cadenza
{
    public class PlayerSounds
    {
        public void Initialize()
        {
            // Register for player hit events.
            PlayerSystem.PlayerHit += this.OnPlayerHit;
            ScoreSystem.TeamHit += this.OnTeamHit;
        }

        public void OnGameStart()
        {
            // Disable all instument tracks.
            foreach (var characterClass in PlayerSystem.CharacterClasses)
                AudioSystem.SetInstrumentActive(characterClass, false);
        }

        public void OnGameStop()
        {
            // Disable all instument tracks.
            foreach (var characterClass in PlayerSystem.CharacterClasses)
                AudioSystem.SetInstrumentActive(characterClass, false);
        }

        private void OnPlayerHit(ScoreDef def)
        {
            var characterClass = def.Player.CharacterClass;
            if (characterClass != null)
                AudioSystem.SetInstrumentActive(characterClass, true);
        }

        private void OnTeamHit(TeamScoreDef def)
        {
            int soundID = def.Class switch
            {
                ScoreClass.Bad => 0,
                ScoreClass.OK => 0,
                ScoreClass.Great => 1,
                ScoreClass.Perfect => 2,
                _ => 0,
            };

            if (soundID != 0)
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", soundID, immediate: true);
        }
    }
}
