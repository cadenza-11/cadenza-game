using UnityEngine;

public class BurstBass : MonoBehaviour
{
    [SerializeField] private GameObject Pos1;
    private Transform hit;
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
    }

    public void Setup(Transform h)
    {
        this.hit = h;
        this.Pos1.transform.localPosition = this.hit.position;
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
