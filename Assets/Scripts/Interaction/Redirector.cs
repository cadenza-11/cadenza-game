using Cadenza;
using UnityEngine;

/// <summary>
/// A collider that redirects to another scene once contacted.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Redirector : MonoBehaviour
{
    [SerializeField] private Level targetLevel;

    [Tooltip("If true, redirects to the selected level in GameManager instead of the targetLevel.")]
    [SerializeField] private bool useLevelSelection;
    private bool hasRedirected = false;

    void OnValidate()
    {
        // If a scene is not in the build scene list, Unity returns a greater out-of-bounds value.
        if (!this.useLevelSelection && (this.targetLevel == null || !this.targetLevel.IsValid))
            Debug.LogWarning("Redirector assigned level with a scene that is not currently in the build scene list. Add the scene in the Build Settings or choose a different level.");
    }

    void OnTriggerEnter(Collider player)
    {
        if (this.hasRedirected)
            return;

        if (this.useLevelSelection)
        {
            this.hasRedirected = true;
            GameManager.RedirectToSelectedLevel();
            return;
        }

        if (this.targetLevel != null && this.targetLevel.IsValid)
        {
            this.hasRedirected = true;
            ApplicationController.SetLevelAsync(this.targetLevel);
            return;
        }
    }
}
