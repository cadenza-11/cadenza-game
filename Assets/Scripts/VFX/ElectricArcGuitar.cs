using UnityEngine;

public class ElectricArcGuitar : MonoBehaviour
{
    [SerializeField] private GameObject Pos1;
    [SerializeField] private GameObject Pos2;
    [SerializeField] private GameObject Pos3;
    [SerializeField] private GameObject Pos4;
    private Transform attack;
    private Transform hit;
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.0f;
    }

    public void Setup(Transform a, Transform h)
    {
        this.attack = a;
        this.hit = h;
    }

    // Update is called once per frame
    void Update()
    {
        this.timer += Time.deltaTime;
        if (this.timer >= 0.25f)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Vector3 normalizedDirection = Vector3.Normalize(this.hit.position - this.attack.position);
            Vector3 offsetTemp2 = this.attack.position + normalizedDirection * 0.333f;
            Vector3 offsetTemp3 = this.hit.position + normalizedDirection * 0.666f;
            Vector3 posTemp2 = new Vector3(offsetTemp2.x, offsetTemp2.y, offsetTemp2.z);
            Vector3 posTemp3 = new Vector3(offsetTemp3.x, offsetTemp3.y, offsetTemp3.z);
            this.Pos1.transform.localPosition = this.attack.position;
            this.Pos2.transform.localPosition = posTemp2;
            this.Pos3.transform.localPosition = posTemp3;
            this.Pos4.transform.localPosition = this.hit.position;
        }
    }
}
