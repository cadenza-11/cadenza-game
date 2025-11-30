using Cadenza;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A collider that redirects to another scene once contacted.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Redirector : MonoBehaviour
{
    [SerializeField] private int sceneBuildIndex;
    private bool isSceneValid =>
        this.sceneBuildIndex >= 0 &&
        this.sceneBuildIndex < SceneManager.sceneCountInBuildSettings;

    void OnValidate()
    {
        // If a scene is not in the build scene list, Unity returns a greater out-of-bounds value.
        if (!this.isSceneValid)
            Debug.LogWarning("Redirector assigned scene that is not currently in the build scene list. Add the scene in the Build Settings or choose a different scene.");
    }

    void OnTriggerEnter(Collider player)
    {
        if (!this.isSceneValid)
            return;

        _ = ApplicationController.SetSceneAsync(this.sceneBuildIndex);
        this.enabled = false;
    }
}
