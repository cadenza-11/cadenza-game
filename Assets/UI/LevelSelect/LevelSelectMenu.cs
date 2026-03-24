using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Cadenza
{
    public class LevelSelectMenu : UIPanel
    {
        private class LevelCard
        {
            public VisualElement Container;
            public Label Name;
            public VisualElement SlotsContainer;
            public VisualElement PreviewImage;
        }

        private class PlayerVote
        {
            public VisualElement Container;
            public Label Name;
        }

        private class VoteState
        {
            public int SelectedLevelIndex;
            public bool IsReady;
        }

        private readonly List<LevelCard> levelCards = new();
        private readonly Dictionary<Player, PlayerVote> playerVotes = new();
        private readonly Dictionary<Player, VoteState> voteStates = new();

        [SerializeField] private VisualTreeAsset levelCardTemplate;
        [SerializeField] private VisualTreeAsset levelVoteTemplate;

        private IReadOnlyList<Level> levels => ApplicationController.Levels;
        private VisualElement levelList;
        private Label statusLabel;
        private bool isRedirecting;

        public override void OnInitialize()
        {
            this.levelList = this.root.Q<VisualElement>("c_LevelList");
            this.statusLabel = this.root.Q<Label>("txt_Status");
            this.Hide();
        }

        public override void OnShow()
        {
            this.isRedirecting = false;
            this.RebuildVoteStates();
            this.RebuildLevelCards();

            InputSystem.UIPlayerSubmit += this.OnSubmit;
            InputSystem.UIPlayerCancel += this.OnCancel;
            InputSystem.UIPlayerNavigate += this.OnNavigate;
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

            this.RefreshDisplay();
        }

        public override void OnHide()
        {
            InputSystem.UIPlayerSubmit -= this.OnSubmit;
            InputSystem.UIPlayerCancel -= this.OnCancel;
            InputSystem.UIPlayerNavigate -= this.OnNavigate;

            this.voteStates.Clear();
            this.playerVotes.Clear();
            this.levelCards.Clear();
            this.levelList?.Clear();
        }

        private void OnSubmit(Player player)
        {
            if (!this.TryGetVoteState(player, out VoteState voteState) || this.isRedirecting)
                return;

            if (this.AreAllPlayersReady())
                this.CommitVote();

            voteState.IsReady = true;
            this.RefreshDisplay();
        }

        private void OnCancel(Player player)
        {
            if (!this.TryGetVoteState(player, out VoteState voteState) || this.isRedirecting)
                return;

            if (voteState.IsReady)
            {
                voteState.IsReady = false;
                this.RefreshDisplay();
                return;
            }

            if (!this.AnyPlayersReady() && this.previousPanel != null)
                this.TransitionTo(this.previousPanel);
        }

        private void OnNavigate(MoveDirection moveDirection, Player player)
        {
            if (!this.TryGetVoteState(player, out VoteState voteState)
                || this.isRedirecting
                || voteState.IsReady
                || this.levels.Count == 0)
                return;

            int step = moveDirection switch
            {
                MoveDirection.Left or MoveDirection.Up => -1,
                MoveDirection.Right or MoveDirection.Down => 1,
                _ => 0
            };

            if (step == 0)
                return;

            voteState.SelectedLevelIndex = (voteState.SelectedLevelIndex + step + this.levels.Count) % this.levels.Count;

            this.RefreshDisplay();
        }

        private void RebuildVoteStates()
        {
            this.voteStates.Clear();
            this.playerVotes.Clear();
            int defaultIndex = 0;

            foreach (var player in PlayerSystem.Players)
            {
                this.voteStates[player] = new VoteState()
                {
                    SelectedLevelIndex = defaultIndex,
                    IsReady = false
                };

                TemplateContainer voteTree = this.levelVoteTemplate.Instantiate();
                var playerVote = new PlayerVote
                {
                    Container = voteTree.Q<VisualElement>("c_LevelVote") ?? voteTree,
                    Name = voteTree.Q<Label>("txt_PlayerName")
                };

                playerVote.Name.text = player.Name;
                this.playerVotes[player] = playerVote;
            }
        }

        private void RebuildLevelCards()
        {
            this.levelList.Clear();
            this.levelCards.Clear();

            foreach (var level in this.levels)
            {
                TemplateContainer cardTree = this.levelCardTemplate.Instantiate();
                var card = new LevelCard
                {
                    Container = cardTree.Q<VisualElement>("c_LevelCard") ?? cardTree,
                    Name = cardTree.Q<Label>("txt_LevelName"),
                    SlotsContainer = cardTree.Q<VisualElement>("c_VoteSlots"),
                    PreviewImage = cardTree.Q<VisualElement>("image_LevelPreview"),
                };

                card.Name.text = level.Name;
                card.PreviewImage.style.backgroundImage = level.PreviewImage;

                this.levelCards.Add(card);
                this.levelList.Add(cardTree);
            }
        }

        private bool TryGetVoteState(Player player, out VoteState voteState)
        {
            return this.voteStates.TryGetValue(player, out voteState);
        }

        private bool AreAllPlayersReady()
        {
            return this.voteStates.Count > 0
                && this.voteStates.Values.All(state => state.IsReady);
        }

        private bool AnyPlayersReady()
        {
            return this.voteStates.Values.Any(state => state.IsReady);
        }

        private void CommitVote()
        {
            if (ApplicationController.IsRedirecting)
                return;

            this.isRedirecting = true;

            Level selectedLevel = this.ResolveWinningLevel();
            GameManager.SetSelectedLevel(selectedLevel);

            // Don't hide this panel until the fader is visible.
            // Wait enough time for the fader to be visible after redirect.
            this.Schedule(0.5f, () => this.Hide());
            GameManager.RedirectToBackstage();
        }

        private Level ResolveWinningLevel()
        {
            if (this.levels.Count == 0)
                return null;

            // Count votes.
            int[] voteCounts = this.GetOrderedVoteCounts();

            // If a tie occurs, select one randomly.
            // Otherwise, select the highest-voted level.
            if (this.CheckTie(voteCounts, out var tiedLevels, out _))
                return tiedLevels[Random.Range(0, tiedLevels.Count)];
            else
                return tiedLevels[0];
        }

        private bool CheckTie(int[] voteCounts, out List<Level> tiedLevels, out int highestCount)
        {
            highestCount = voteCounts.Max();
            tiedLevels = new List<Level>();

            for (int i = 0; i < voteCounts.Length; i++)
            {
                if (voteCounts[i] == highestCount)
                    tiedLevels.Add(this.levels[i]);
            }
            return tiedLevels.Count > 1;
        }

        private int[] GetOrderedVoteCounts()
        {
            int[] voteCounts = new int[this.levels.Count];
            foreach (var voteState in this.voteStates.Values)
            {
                if (!voteState.IsReady)
                    continue;
                int voteIndex = Mathf.Clamp(voteState.SelectedLevelIndex, 0, this.levels.Count - 1);
                voteCounts[voteIndex]++;
            }
            return voteCounts;
        }

        private void RefreshDisplay()
        {
            int[] voteCounts = this.GetOrderedVoteCounts();
            int highestCount = voteCounts.Max();
            int readyCount = this.voteStates.Values.Count(voteState => voteState.IsReady);
            this.statusLabel.text = $"{readyCount}/{this.voteStates.Count} players ready";

            // Update level UI.
            for (int i = 0; i < this.levelCards.Count; i++)
            {
                bool isLeading = highestCount > 0 && voteCounts[i] == highestCount;
                this.RefreshLevelCard(i, isLeading);
            }
        }

        private void RefreshLevelCard(int levelIndex, bool isLeading)
        {
            var card = this.levelCards[levelIndex];
            card.Container.EnableInClassList("is-leading", isLeading);
            card.SlotsContainer.Clear();

            // Display the players that voted for this level.
            var voters = this.voteStates
                .Where(entry => entry.Value.SelectedLevelIndex == levelIndex)
                .OrderBy(entry => entry.Key.ID)
                .ToArray();

            card.SlotsContainer.style.display = voters.Length == 0
                ? DisplayStyle.None
                : DisplayStyle.Flex;

            for (int i = 0; i < voters.Length; i++)
            {
                var (player, voteState) = voters[i];
                if (!this.playerVotes.TryGetValue(player, out PlayerVote voteView))
                    continue;

                voteView.Container.EnableInClassList("is-ready", voteState.IsReady);
                voteView.Container.EnableInClassList("is-pending", !voteState.IsReady);
                card.SlotsContainer.Add(voteView.Container);
            }
        }
    }
}
