using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Unity.Cinemachine;
using Cadenza;

public class BindPlayableToCinemachineBrain : MonoBehaviour
{
    [SerializeField] private PlayableDirector director;

    private void Start()
    {
        this.BindCinemachineBrain();
    }

    private void BindCinemachineBrain()
    {
        if (this.director == null || this.director.playableAsset == null)
            return;

        var brain = CameraSystem.CinemachineBrain;
        if (brain == null)
        {
            Debug.LogError("No CinemachineBrain found.");
            return;
        }

        var timeline = this.director.playableAsset as TimelineAsset;
        if (timeline == null)
            return;

        foreach (var track in timeline.GetOutputTracks())
        {
            if (track is CinemachineTrack)
            {
                // Bind the track to the Unity Camera that has the Brain
                this.director.SetGenericBinding(track, brain);
            }
        }
    }
}
