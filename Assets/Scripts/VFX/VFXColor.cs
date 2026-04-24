using UnityEngine;
using Cadenza;
using UnityEngine.VFX;

public class VFXColor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Character character;
    [SerializeField] private VisualEffect VFX;
    [SerializeField] private string colorVarName;
    void Start()
    {
        this.VFX.SetVector4(this.colorVarName, this.character.Player.Colorway.SecondaryColor);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
