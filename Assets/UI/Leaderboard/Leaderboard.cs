using Cadenza;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

[RequireComponent(typeof(Collider), typeof(UIDocument))]
public class Leaderboard : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private Button exitButton;
    private Player player;

    void Start()
    {
        this.uiDocument = this.GetComponent<UIDocument>();
        this.root = this.uiDocument.rootVisualElement;
        this.root.style.display = DisplayStyle.None;

        this.exitButton = this.root.Q<Button>("b_Exit");
        this.exitButton.clicked += () => this.Hide();
        this.exitButton.RegisterCallback<NavigationSubmitEvent>(_ => this.Hide());
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
