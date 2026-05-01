using System.Collections.Generic;
using System.Linq;
using Cadenza;
using Cadenza.Utils;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Leaderboard : UIPanel, IInteractable
{
    public enum ShowMode
    {
        FromMenu,
        FromGame,
        FromResults,
    }

    [SerializeField] private VisualTreeAsset resultLineAsset;

    protected override bool IsWorldSpace => true;
    protected override InputMode UIInputMode => InputMode.Single;
    protected override VisualElement InitialFocus => this.exitButton;

    private Button exitButton;
    private Player openingPlayer;
    private ScrollView resultsElement;
    private Results[] results = System.Array.Empty<Results>();
    private readonly List<Foldout> resultFoldouts = new();
    private ShowMode showMode;

    public override void OnInitialize()
    {
        this.root.RegisterCallback<NavigationCancelEvent>(_ => this.Close(), TrickleDown.TrickleDown);
        this.root.RegisterCallback<NavigationMoveEvent>(this.OnNavigationMove, TrickleDown.TrickleDown);

        // Configure exit button.
        this.exitButton = this.root.Q<Button>("b_Exit");
        this.exitButton.clicked += this.Close;

        this.resultsElement = this.root.Q<ScrollView>("results");
        this.resultsElement.focusable = false;

        SaveSystem.ResultsFileCreated += this.RefreshResults;
        SaveSystem.ResultsFileDeleted += this.RefreshResults;
    }

    public override void OnApplicationStop()
    {
        SaveSystem.ResultsFileCreated -= this.RefreshResults;
        SaveSystem.ResultsFileDeleted -= this.RefreshResults;
    }

    public override void OnShow()
    {
        if (this.openingPlayer != null)
            InputSystem.SwitchInputMapSinglePlayer(InputSystem.InputMap.UI, this.openingPlayer);

        this.RefreshResults();
    }

    public override void OnHide()
    {
        if (this.showMode == ShowMode.FromGame)
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);
        else if (this.showMode == ShowMode.FromResults)
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.UI);

        this.openingPlayer = null;
    }

    public void Show(ShowMode mode)
    {
        this.showMode = mode;
        this.Show();
    }

    private void PopulatePlayerStatsLine(VisualElement row, Results.PlayerDef playerDef, ResultsDef playerResult)
    {
        if (row == null)
            return;

        var portrait = row.Q<VisualElement>("player-portrait");
        if (portrait != null &&
            TeamSystem.AvailableClasses != null &&
            TeamSystem.AvailableClasses.TryGetCharacterByID(playerDef.ClassID, out var characterClass) &&
            characterClass.HeadshotPortrait != null)
        {
            portrait.style.backgroundImage = characterClass.HeadshotPortrait;
        }

        SetLabelText(row, "player-name", playerDef.Name);
        SetLabelText(row, "player-score", $"{playerResult.ScoreTotal:F2}");
        SetLabelText(row, "player-score-label", "score");
        SetLabelText(row, "player-bad", $"Bad: {CreateScoreClassPercentageText(playerResult, ScoreClass.Bad)}");
        SetLabelText(row, "player-ok", $"OK: {CreateScoreClassPercentageText(playerResult, ScoreClass.OK)}");
        SetLabelText(row, "player-great", $"Great: {CreateScoreClassPercentageText(playerResult, ScoreClass.Great)}");
        SetLabelText(row, "player-perfect", $"Perfect: {CreateScoreClassPercentageText(playerResult, ScoreClass.Perfect)}");
        SetLabelText(row, "player-deaths", $"Deaths: {playerResult.Deaths}");
        SetLabelText(row, "player-hits", $"Hits: {playerResult.Hits}");
    }

    private static void ClearPlayerStatsLine(VisualElement row)
    {
        SetLabelText(row, "player-name", string.Empty);
        SetLabelText(row, "player-score", string.Empty);
        SetLabelText(row, "player-score-label", string.Empty);
        SetLabelText(row, "player-bad", string.Empty);
        SetLabelText(row, "player-ok", string.Empty);
        SetLabelText(row, "player-great", string.Empty);
        SetLabelText(row, "player-perfect", string.Empty);
        SetLabelText(row, "player-deaths", string.Empty);
        SetLabelText(row, "player-hits", string.Empty);
        ClearBackgroundImage(row, "player-portrait");
    }

    private static string CreateScoreClassPercentageText(ResultsDef playerResult, ScoreClass scoreClass)
    {
        float percentage = playerResult.Hits == 0
            ? 0f
            : (float)playerResult.GetCount(scoreClass) / playerResult.Hits * 100f;

        return $"{percentage:F0}%";
    }

    private static void SetLabelText(VisualElement container, string labelName, string text)
    {
        var label = container.Q<Label>(labelName);
        if (label != null)
            label.text = text;
    }

    private static void ClearBackgroundImage(VisualElement container, string elementName)
    {
        var element = container.Q<VisualElement>(elementName);
        if (element != null)
            element.style.backgroundImage = StyleKeyword.None;
    }

    public void OnInteract(Player player)
    {
        this.openingPlayer = player;
        this.previousPanel = null;
        this.Show(ShowMode.FromGame);
    }

    public void FocusResult(Results result)
    {
        this.RefreshResults();

        int index = System.Array.IndexOf(this.results, result);
        this.FocusResult(index);
    }

    public void FocusResult(int index)
    {
        this.RefreshResults();
        if (index >= 0 && index < this.resultsElement.childCount)
        {
            var resultLine = this.resultsElement.Children().ElementAt(index);

            // Override default scrolling behavior with transitions.
            // Restore default scrolling after transition ends or is cancelled.
            var contentContainer = this.resultsElement.contentContainer;
            contentContainer.AddToClassList("scroll-view--animated");
            contentContainer.RegisterCallbackOnce<TransitionEndEvent>(_ => contentContainer.RemoveFromClassList("scroll-view--animated"));
            contentContainer.RegisterCallbackOnce<TransitionCancelEvent>(_ => contentContainer.RemoveFromClassList("scroll-view--animated"));
            this.resultsElement.ScrollTo(resultLine);

            var foldout = resultLine.Q<Foldout>();
            foldout.Focus();
            foldout.value = true;
        }
    }

    private void RefreshResults()
    {
        if (this.resultsElement == null)
            return;

        SaveSystem.GetPreviousRuns(out var loadedResults);
        this.results = loadedResults ?? System.Array.Empty<Results>();
        this.results = this.results
            .OrderByDescending(r => r.OverallScore)
            .ToArray();

        this.resultsElement.Clear();
        this.resultFoldouts.Clear();

        int rank = 1;
        foreach (var result in this.results)
        {
            // Fill header.
            string time = UI.GetHumanizedTime(result.Timestamp);
            string teamName = string.IsNullOrEmpty(result.TeamName) ? "Unnamed Team" : result.TeamName;
            string levelName = string.IsNullOrEmpty(result.LevelName) ? "Unnamed Level" : result.LevelName;
            var resultLine = this.resultLineAsset.CloneTree().Q<VisualElement>("result-entry");
            var foldout = resultLine.Q<Foldout>("result-foldout");

            // Fill team stats.
            var teamStats = resultLine.Q<VisualElement>("team-stats");
            if (foldout == null || teamStats == null)
                continue;

            SetLabelText(teamStats, "team-score", $"{result.OverallScore:F2}");
            SetLabelText(teamStats, "team-rank", ScoreSystem.GetGradeLetter(result.OverallScore));

            // Fill player stats.
            var playerStats = resultLine.Q<VisualElement>("player-stats");

            if (foldout == null || playerStats == null)
                continue;

            var playerStatsLines = playerStats.Query<VisualElement>("player-result-slot").ToList();
            foreach (var playerStatsLine in playerStatsLines)
                ClearPlayerStatsLine(playerStatsLine);

            this.resultFoldouts.Add(foldout);
            foldout.text = $"#{rank}. {teamName} vs. {levelName} ({time})";
            rank++;

            int playerStatsLineIndex = 0;
            foreach ((var playerDef, var playerResult) in result.PlayerResults.OrderBy(entry => entry.Key.ID))
            {
                if (playerStatsLineIndex >= playerStatsLines.Count)
                    break;

                this.PopulatePlayerStatsLine(playerStatsLines[playerStatsLineIndex], playerDef, playerResult);
                playerStatsLineIndex++;
            }

            this.resultsElement.Add(resultLine);
        }

        // Keep only one foldout open at a time, close other foldouts if one is opened.
        List<Foldout> allFoldouts = this.resultsElement.Query<Foldout>().ToList();
        foreach (var foldout in allFoldouts)
        {
            foldout.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    foreach (var other in allFoldouts)
                    {
                        if (other != foldout)
                            other.value = false;
                    }
                }
            });
        }
    }

    private void OnNavigationMove(NavigationMoveEvent evt)
    {
        if (evt.direction != NavigationMoveEvent.Direction.Up &&
            evt.direction != NavigationMoveEvent.Direction.Down)
            return;

        // Don't perform default focus behavior (wrapping).
        // Handle that in this method.
        this.root.panel.focusController.IgnoreEvent(evt);

        if (evt.target is not VisualElement target)
            return;

        // Move to first result from exit button.
        if (evt.direction == NavigationMoveEvent.Direction.Down && this.IsInsideElement(target, this.exitButton))
        {
            if (this.TryFocusResult(0))
                evt.StopImmediatePropagation();

            return;
        }

        int focusedResultIndex = this.GetFocusedResultIndex(target);
        if (focusedResultIndex < 0)
            return;

        if (evt.direction == NavigationMoveEvent.Direction.Up)
        {
            // Move to exit button from results.
            if (focusedResultIndex == 0)
                this.exitButton.Focus();
            else
                this.TryFocusResult(focusedResultIndex - 1);
        }
        else
        {
            this.TryFocusResult(focusedResultIndex + 1);
        }
    }

    private int GetFocusedResultIndex(VisualElement target)
    {
        var foldout = this.GetAncestorOfType<Foldout>(target);
        return foldout == null ? -1 : this.resultFoldouts.IndexOf(foldout);
    }

    private bool TryFocusResult(int index, bool expand = false)
    {
        if (index < 0 || index >= this.resultFoldouts.Count)
            return false;

        var foldout = this.resultFoldouts[index];
        if (foldout == null)
            return false;

        this.resultsElement.ScrollTo(foldout.parent ?? foldout);
        foldout.Focus();
        if (expand)
            foldout.value = true;

        return true;
    }

    private bool IsInsideElement(VisualElement target, VisualElement container)
    {
        while (target != null)
        {
            if (target == container)
                return true;

            target = target.parent;
        }

        return false;
    }

    private T GetAncestorOfType<T>(VisualElement target) where T : VisualElement
    {
        while (target != null)
        {
            if (target is T matched)
                return matched;

            target = target.parent;
        }

        return null;
    }

    private void Close()
    {
        if (this.previousPanel != null && ApplicationController.State != ApplicationState.GameSession)
            this.TransitionTo(this.previousPanel);
        else
            this.Hide();
    }
}
