using Cadenza;
using UnityEngine;
using UnityEngine.VFX;

public class BurstBass : MonoBehaviour
{
    [SerializeField] private GameObject Pos1;
    [SerializeField] private VisualEffect VFX;
    private Transform hit;
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
    }

    public void Setup(Transform h, Character character)
    {
        this.hit = h;
        this.Pos1.transform.localPosition = this.hit.position;
        Color sColor = character.Player.Colorway.SecondaryColor;
        Color tColor = character.Player.Colorway.TertiaryColor;
        sColor.a = 1;
        tColor.a = 1;
        this.VFX.SetVector4("8thNoteColor", sColor);
        this.VFX.SetVector4("BassClefColor", tColor);
    }

    // Update is called once per frame
    void Update()
    {
        this.timer += Time.deltaTime;
        if (this.timer >= 0.5f)
        {
            Destroy(this.gameObject);
        }
    }
}
