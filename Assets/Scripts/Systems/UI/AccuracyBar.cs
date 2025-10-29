using Cadenza;
using UnityEngine;
using UnityEngine.UIElements;

public class AccuracyBar : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private Slider accuracyBar;
    private Label accuracyText;

    void Start()
    {
        this.accuracyBar = this.uiDocument.rootVisualElement.Q<Slider>();
        this.accuracyText = this.uiDocument.rootVisualElement.Q<Label>();

        float halfPeriod = (float)BeatSystem.SecondsPerBeat / 2;
        this.accuracyBar.lowValue = -halfPeriod;
        this.accuracyBar.highValue = +halfPeriod;
    }

    public void SetAccuracy(float accuracy)
    {
        this.accuracyBar.value = accuracy;

        accuracy = Mathf.Abs(accuracy);
        this.accuracyText.text =
            accuracy < 0.05f ? "Perfect" :
            accuracy < 0.10f ? "Good" : string.Empty;
    }
}
