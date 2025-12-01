using System.Linq;
using Cadenza;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider), typeof(UIDocument))]
public class Leaderboard : MonoBehaviour
{
    [SerializeField] VisualTreeAsset resultLineAsset;

    private UIDocument uiDocument;
    private VisualElement root;
    private Button exitButton;
    private Player player;
    private Results[] results;

    void Start()
    {
        this.uiDocument = this.GetComponent<UIDocument>();
        this.root = this.uiDocument.rootVisualElement;
        this.root.style.display = DisplayStyle.None;

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
        foreach (var result in this.results)
        {
            string teamName = string.IsNullOrEmpty(result.TeamName) ? "Unnamed Team" : result.TeamName;
            var resultLine = this.resultLineAsset.CloneTree();
            resultLine.Q<Label>().text = $"{teamName} ... {result.TeamResults.ScoreTotal}";
            resultsElement.Add(resultLine);
        }
    }

    void OnTriggerEnter(Collider collider)
    {
        if (!collider.TryGetComponent(out Character character))
            return;

        InputAction action = character.Player.Input.actions.FindAction("Attack/Light");
        action.performed += this.OpenLeaderboardUI;
    }

    void OnTriggerExit(Collider collider)
    {
        if (!collider.TryGetComponent(out Character character))
            return;

        InputAction action = character.Player.Input.actions.FindAction("Attack/Light");
        action.performed -= this.OpenLeaderboardUI;
    }

    private void OpenLeaderboardUI(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        Debug.Log("Opening leaderboard.");
        this.player = Cadenza.InputSystem.GetPlayerFromDevice(context.control.device);
        this.Show();

    }

    private void Show()
    {
        Cadenza.InputSystem.EnableSinglePlayerInput(this.player);
        Cadenza.InputSystem.DisableInputActionMapForPlayers("Player", enableOthers: false, this.player);
        this.root.style.display = DisplayStyle.Flex;
    }

    private void Hide()
    {
        Cadenza.InputSystem.EnableInputActionMapForPlayers("Player", disableOthers: false, PlayerSystem.Players);
        this.root.style.display = DisplayStyle.None;
    }
}
