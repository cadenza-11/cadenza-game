using UnityEngine;

public class RockStationaryWave : MonoBehaviour
{

    [SerializeField] private GameObject warning;
    [SerializeField] private GameObject hurtBox;
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        this.timer += Time.deltaTime;
        if (this.timer >= 3.0f)
        {
            this.hurtBox.SetActive(true);
            this.warning.SetActive(false);
        }
    }
}
