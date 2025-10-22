using UnityEngine;
using UnityEngine.UIElements;

public class AccuracyBar : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    private Slider accuracyBar;
    private Label accuracyText;

    private const string EarlyText = "Early!";
    private const string LateText = "Late!";
    private const float OnTimeThreshold = 0.1f;

    void Start()
    {
        this.accuracyBar = this.uiDocument.rootVisualElement.Q<Slider>();
        this.accuracyText = this.uiDocument.rootVisualElement.Q<Label>();
    }

    public void SetAccuracy(float accuracy)
    {
        this.accuracyBar.value = Mathf.Abs(accuracy);
        this.accuracyText.text = accuracy < 0 ? EarlyText : LateText;
    }
}
