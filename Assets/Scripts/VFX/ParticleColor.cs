using UnityEngine;
using UnityEngine.VFX;
using Cadenza;

public class ParticleColor : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Character character;
    [SerializeField] private ParticleSystem particle;
    void Start()
    {
        Color sColor = this.character.Player.Colorway.SecondaryColor;
        sColor.a = 1;
        var particleMain = this.particle.main;
        particleMain.startColor = sColor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
