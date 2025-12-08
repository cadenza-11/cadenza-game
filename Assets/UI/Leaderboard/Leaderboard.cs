using System.Linq;
using System.Text;
using Cadenza;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class Leaderboard : MonoBehaviour, IInteractable
{
    [SerializeField] VisualTreeAsset resultLineAsset;

    private UIDocument uiDocument;
    private VisualElement root;
    private Button exitButton;
    private Player openingPlayer;
    private Results[] results;

    void Start()
    {
        this.uiDocument = this.GetComponent<UIDocument>();
        this.root = this.uiDocument.rootVisualElement;
        this.root.style.display = DisplayStyle.None;
        this.root.RegisterCallback<NavigationCancelEvent>(_ => this.Hide(), TrickleDown.TrickleDown);

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
        int i = 0;
        foreach (var result in this.results)
        {
            string teamName = string.IsNullOrEmpty(result.TeamName) ? "Unnamed Team" : result.TeamName;
            var resultLine = this.resultLineAsset.CloneTree();

            StringBuilder sb = new();
            sb.AppendLine($"#{++i}. {teamName} ... {result.TeamResults.ScoreTotal}");

            foreach ((int id, var playerResult) in result.PlayerResults)
                sb.AppendLine($"\t Player {id + 1}: {playerResult.ScoreTotal} ({playerResult.Hits} hits)");

            resultLine.Q<Label>().text = sb.ToString();
            resultsElement.Add(resultLine);
        }
    }

    public void OnInteract(Player player)
    {
        this.openingPlayer = player;
        this.Show();
    }

    private void Show()
    {
        InputSystem.EnableSinglePlayerInput(this.openingPlayer);
        InputSystem.DisableInputActionMapForPlayers("Player", enableOthers: false, this.openingPlayer);
        this.root.style.display = DisplayStyle.Flex;
        this.exitButton.Focus();
    }

    private void Hide()
    {
        InputSystem.EnableInputActionMapForPlayers("Player", disableOthers: false, PlayerSystem.Players);
        this.root.style.display = DisplayStyle.None;
    }
}
