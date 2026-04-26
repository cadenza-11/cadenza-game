using Cadenza;
using UnityEngine;

public class RockStationaryWave : MonoBehaviour
{

    [SerializeField] private GameObject warning;
    [SerializeField] private GameObject hurtBox;
    private int beatCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
        if (this.beatCount >= 3)
        {
                this.hurtBox.SetActive(true);
                this.warning.SetActive(false);
        }
        else if (this.beatCount >= 1)
        {
            this.warning.SetActive(true);
        }
        if (!GameManager.IsCombatActive)
        {
            Destroy(this.gameObject);
        }
    }
    private void OnDestroy()
    {
        BeatSystem.BeatPlayed -= this.BeatPass;
    }
}
