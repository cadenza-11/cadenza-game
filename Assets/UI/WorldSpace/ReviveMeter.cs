using Cadenza;
using UnityEngine;
using UnityEngine.UIElements;

public class ReviveMeter : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private ProgressBar reviveMeter;
    private InputHint inputHint;

    void Awake()
    {
        this.reviveMeter = this.uiDocument.rootVisualElement.Q<ProgressBar>();
        this.inputHint = this.uiDocument.rootVisualElement.Q<InputHint>();
    }

    public void SetInputHint(ControllerType controller)
    {
        this.inputHint.ShownControls = controller;
    }

    public void SetThreshold(float threshold)
    {
        this.reviveMeter.highValue = threshold;
    }

    public void SetRevive(float revive)
    {
        this.reviveMeter.value = Mathf.Min(revive, this.reviveMeter.highValue);
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
