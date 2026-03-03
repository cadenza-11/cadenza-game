using System.Linq;
using Cadenza;
using Cadenza.Utils;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Leaderboard : UIPanel, IInteractable
{
    [SerializeField] private VisualTreeAsset resultLineAsset;

    protected override bool IsWorldSpace => true;
    protected override InputMode UIInputMode => InputMode.Single;
    protected override VisualElement InitialFocus => this.exitButton;

    private Button exitButton;
    private Player openingPlayer;
    private ScrollView resultsElement;
    private Results[] results = System.Array.Empty<Results>();

    public override void OnInitialize()
    {
        this.root.RegisterCallback<NavigationCancelEvent>(_ => this.Close(), TrickleDown.TrickleDown);

        // Configure exit button.
        this.exitButton = this.root.Q<Button>("b_Exit");
        this.exitButton.clicked += this.Close;

        this.resultsElement = this.root.Q<ScrollView>("results");
        this.resultsElement.focusable = false;

        InputSystem.UIPlayerCancel += this.OnPlayerCancel;
        SaveSystem.ResultsFileCreated += this.RefreshResults;
        SaveSystem.ResultsFileDeleted += this.RefreshResults;
    }

    public override void OnApplicationStop()
    {
        InputSystem.UIPlayerCancel -= this.OnPlayerCancel;
        SaveSystem.ResultsFileCreated -= this.RefreshResults;
        SaveSystem.ResultsFileDeleted -= this.RefreshResults;
    }

    private VisualElement CreatePlayerStatsLine(Results.PlayerDef playerDef, ResultsDef playerResult)
    {
        string playerClassText = TeamSystem.AvailableClasses != null &&
            TeamSystem.AvailableClasses.TryGetCharacterByID(playerDef.ClassID, out var characterClass)
                ? $" ({characterClass.Name})"
                : string.Empty;

        var row = new VisualElement();
        row.AddToClassList("player-result");

        var header = new VisualElement();
        header.AddToClassList("player-header");

        var playerName = new Label($"{playerDef.Name}{playerClassText}");
        playerName.AddToClassList("player-name");
        header.Add(playerName);

        var score = new Label($"Score: {playerResult.ScoreTotal:F2}");
        score.AddToClassList("player-score");
        header.Add(score);

        row.Add(header);

        var metrics = new VisualElement();
        metrics.AddToClassList("player-metrics");
        metrics.Add(this.CreateMetricLabel($"Hits: {playerResult.Hits}"));
        metrics.Add(this.CreateMetricLabel($"Deaths: {playerResult.Deaths}"));
        row.Add(metrics);

        return row;
    }

    private Label CreateMetricLabel(string text)
    {
        var label = new Label(text);
        label.AddToClassList("player-metric");
        return label;
    }

    public void OnInteract(Player player)
    {
        this.openingPlayer = player;
        this.previousPanel = null;
        this.Show();
    }

    public override void OnShow()
    {
        if (this.openingPlayer != null)
            InputSystem.SwitchInputMapSinglePlayer(InputSystem.InputMap.UI, this.openingPlayer);

        this.RefreshResults();
    }

    public override void OnHide()
    {
        if (ApplicationController.State == ApplicationState.GameSession)
            InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);

        this.openingPlayer = null;
    }

    private void OnPlayerCancel(Player _)
    {
        this.Close();
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

        int rank = 1;
        foreach (var result in this.results)
        {
            string time = UI.GetHumanizedTime(result.Timestamp);
            string teamName = string.IsNullOrEmpty(result.TeamName) ? "Unnamed Team" : result.TeamName;
            string levelName = string.IsNullOrEmpty(result.LevelName) ? "Unnamed Level" : result.LevelName;
            var resultLine = this.resultLineAsset.CloneTree();
            var foldout = resultLine.Q<Foldout>("result-foldout");
            var playerStats = resultLine.Q<VisualElement>("player-stats");

            if (foldout == null || playerStats == null)
                continue;

            foldout.text = $"#{rank}. {teamName} in {levelName} ({time})";
            rank++;

            foreach ((var playerDef, var playerResult) in result.PlayerResults.OrderBy(entry => entry.Key.ID))
                playerStats.Add(this.CreatePlayerStatsLine(playerDef, playerResult));

            if (playerStats.childCount == 0)
            {
                var empty = new Label("No player stats recorded.");
                empty.AddToClassList("player-empty");
                playerStats.Add(empty);
            }

            this.resultsElement.Add(resultLine);
        }
    }

    private void Close()
    {
        if (this.previousPanel != null && ApplicationController.State != ApplicationState.GameSession)
            this.TransitionTo(this.previousPanel);
        else
            this.Hide();
    }
}
