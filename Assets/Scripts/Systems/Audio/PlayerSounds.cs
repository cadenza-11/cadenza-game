namespace Cadenza
{
    public class PlayerSounds
    {
        public void Initialize()
        {
            // Register for hit events.
            ScoreSystem.TeamHit += this.OnTeamHit;
        }

        public void OnGameStart()
        {
            foreach (var player in PlayerSystem.Players)
                player.PlayerHit += this.OnPlayerHit;
        }

        public void OnGameStop()
        {
            foreach (var player in PlayerSystem.Players)
                player.PlayerHit -= this.OnPlayerHit;
        }

        private void OnPlayerHit(ScoreDef def)
        {
            AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerHitEvent, "ClassID", def.Player.CharacterClass.ID, immediate: true);
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
                AudioSystem.PlayOneShotWithParameter(AudioSystem.PlayerOneShotsEvent, "ID", soundID, immediate: false);
        }
    }
}
