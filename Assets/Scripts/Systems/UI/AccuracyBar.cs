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

    public void OnPlayerHit(ScoreSystem.ScoreDef def)
    {
        this.accuracyBar.value = (float)def.Latency;
        this.accuracyText.text = def.Class.ToString();
    }
}
