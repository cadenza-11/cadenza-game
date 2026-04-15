using Cadenza;
using UnityEngine;

public class RockGuitarLightning : MonoBehaviour
{

    [SerializeField] private GameObject warning;
    [SerializeField] private ParticleSystem lightningStrike;
    [SerializeField] private GameObject hurtBox;
    private float timer;
    private bool played = false;
    private int beatCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
        this.beatCount = 0;
        BeatSystem.BeatPlayed += this.BeatPass;
    }

    private void BeatPass()
    {
        this.beatCount++;
    }

    // Update is called once per frame
    void Update()
    {
        if(this.beatCount >= 3)
        {
            this.timer += Time.deltaTime;
            if (this.timer >= 3.0f)
            {
                Destroy(this.gameObject);
            }
            else if (this.timer >= 0.1f)
            {
                this.hurtBox.SetActive(false);
            }
            else
            {
                this.hurtBox.SetActive(true);
                this.warning.SetActive(false);
                if (this.played == false)
                {
                    this.lightningStrike.Play();
                    this.played = true;
                }
            }
        }
        else if(this.beatCount >= 1)
        {
            this.warning.SetActive(true);
        }
    }
}
