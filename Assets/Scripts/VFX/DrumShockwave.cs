using UnityEngine;

public class DrumShockwave : MonoBehaviour
{
    [SerializeField] private ParticleSystem shock;
    private void OnEnable()
    {
        this.shock.Play();
    }
}
