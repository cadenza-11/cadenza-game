using System.Linq;
using System.Text;
using Cadenza;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Leaderboard : UIPanel, IInteractable
{
    [SerializeField] VisualTreeAsset resultLineAsset;

    protected override bool IsWorldSpace => true;
    protected override InputMode UIInputMode => InputMode.Single;
    protected override VisualElement InitialFocus => this.exitButton;
    private Button exitButton;
    private Player openingPlayer;
    private Results[] results;

    public override void OnInitialize()
    {
        this.root.RegisterCallback<NavigationCancelEvent>(_ => this.Hide(), TrickleDown.TrickleDown);
        InputSystem.UIPlayerCancel += _ => this.Hide();

        // Configure exit button.
        this.exitButton = this.root.Q<Button>("b_Exit");
        this.exitButton.clicked += () => this.Hide();
        this.exitButton.RegisterCallback<NavigationSubmitEvent>(_ => this.Hide());

        // Populate leaderboard UI.
        SaveSystem.GetPreviousRuns(out this.results);
        this.results = this.results
            .OrderByDescending(r => r.TeamResults.ScoreTotal)
            .ToArray();

        var resultsElement = this.root.Q<VisualElement>("results");
        resultsElement.focusable = false;
        int i = 0;
        foreach (var result in this.results)
        {
            string teamName = string.IsNullOrEmpty(result.TeamName) ? "Unnamed Team" : result.TeamName;
            var resultLine = this.resultLineAsset.CloneTree();

            StringBuilder sb = new();
            sb.AppendLine($"#{++i}. {teamName} ... {result.TeamResults.ScoreTotal}");

            foreach ((string playerName, var playerResult) in result.PlayerResults)
                sb.AppendLine($"\t {playerName}: {playerResult.ScoreTotal} ({playerResult.Hits} hits)");

            resultLine.Q<Label>().text = sb.ToString();
            resultsElement.Add(resultLine);
        }
    }

    public void OnInteract(Player player)
    {
        this.openingPlayer = player;
        this.Show();
    }

    public override void OnShow()
    {
        InputSystem.SwitchInputMapSinglePlayer(InputSystem.InputMap.UI, this.openingPlayer);
    }

    public override void OnHide()
    {
        InputSystem.SwitchInputMapMultiPlayer(InputSystem.InputMap.Player);
    }
}
