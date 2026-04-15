using Cadenza;
using UnityEngine;
using UnityEngine.VFX;

public class BeamPianoFlip : MonoBehaviour
{

    [SerializeField] private GameObject character;
    [SerializeField] private VisualEffect beam;
    private void OnEnable()
    {
        this.beam.SetBool("FacingRight", this.character.GetComponent<Character>().FacingRight);
        this.transform.localPosition = new Vector3(this.character.GetComponent<Character>().FacingRight ? -1f : 1f, 0.0f, 0.0f);
    }
}
