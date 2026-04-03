using UnityEngine;

public class FunkGuitarLightning : MonoBehaviour
{

    [SerializeField] private GameObject warning;
    [SerializeField] private ParticleSystem lightningStrike;
    [SerializeField] private GameObject hurtBox;
    private float timer;
    private bool played = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        this.timer += Time.deltaTime;
        if (this.timer >= 6.0f)
        {
            Destroy(this.gameObject);
        }
        else if (this.timer >= 3.1f)
        {
            this.hurtBox.SetActive(false);
        }
        else if (this.timer >= 3.0f)
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
}
