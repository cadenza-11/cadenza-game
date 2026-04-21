using UnityEngine;
using UnityEngine.UIElements;

public class PlayerNameIndicator : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private Label playerNameLabel;

    void Awake()
    {
        this.playerNameLabel = this.uiDocument.rootVisualElement.Q<Label>("update_Name");
    }
    
    public void SetName(string name)
    {
        this.playerNameLabel.text = name;
    }

    public void Show()
    {
        this.uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        this.uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }
}
