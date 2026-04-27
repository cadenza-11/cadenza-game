using Cadenza;
using UnityEngine;
using UnityEngine.VFX;

public class BurstBass : MonoBehaviour
{
    [SerializeField] private VisualEffect VFX;
    [SerializeField] private Character character;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.Setup();
    }

    public void Setup()
    {
        Color sColor = this.character.Player.Colorway.SecondaryColor;
        Color tColor = this.character.Player.Colorway.TertiaryColor;
        sColor.a = 1;
        tColor.a = 1;
        this.VFX.SetVector4("8thNoteColor", sColor);
        this.VFX.SetVector4("BassClefColor", tColor);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
