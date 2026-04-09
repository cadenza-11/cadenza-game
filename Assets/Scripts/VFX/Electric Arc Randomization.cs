using UnityEngine;

public class ElectricArcRandomization : MonoBehaviour
{
    [SerializeField] private GameObject Pos1;
    [SerializeField] private GameObject Pos2;
    [SerializeField] private GameObject Pos3;
    [SerializeField] private GameObject Pos4;
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 1.0f;
    }

    // Update is called once per frame
    void Update()
    {
        this.timer += Time.deltaTime;
        if(this.timer >= 0.25f)
        {
            this.timer = 0.0f;
            Vector3 posTemp1 = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
            while(Vector3.Distance(posTemp1, this.transform.localPosition) > 1.0f)
            {
                posTemp1 = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
            }
            Vector3 posTemp4 = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
            while (Vector3.Distance(posTemp4, this.transform.localPosition) > 1.0f)
            {
                posTemp4 = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
            }
            Vector3 normalizedDirection = Vector3.Normalize(posTemp4 - posTemp1);
            Vector3 offsetTemp2 = posTemp1 + normalizedDirection * 0.333f;
            Vector3 offsetTemp3 = posTemp4 + normalizedDirection * 0.666f;
            Vector3 posTemp2 = new Vector3(offsetTemp2.x, 500.0f, offsetTemp2.z);
            Vector3 posTemp3 = new Vector3(offsetTemp3.x, 500.0f, offsetTemp3.z);
            this.Pos1.transform.localPosition = posTemp1;
            this.Pos2.transform.localPosition = posTemp2;
            this.Pos3.transform.localPosition = posTemp3;
            this.Pos4.transform.localPosition = posTemp4;
        }
    }
}
