using UnityEngine;

public class FunkSummoning : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private Material summonSign;
    [SerializeField] private GameObject summoned;

    private float lerpNum;
    private bool firstHalf;


    void Start()
    {
        this.lerpNum = 0.0f;
        this.firstHalf = true;
    }

    // Update is called once per frame
    void Update()
    {
        this.lerpNum += Time.deltaTime / 2;
        if (this.firstHalf)
        {
            this.summonSign.SetFloat("_TransLerp", Mathf.Lerp(0, 1, (Mathf.Clamp(this.lerpNum, 0, 1))));
        }
        else
        {
            this.summonSign.SetFloat("_TransLerp", Mathf.Lerp(1, 0, (Mathf.Clamp(this.lerpNum, 0, 1))));
        }

        if(this.firstHalf && Mathf.Clamp(this.lerpNum, 0, 1) == 1)
        {
            this.lerpNum = 0;
            this.firstHalf = false;
            Instantiate(this.summoned, new Vector3(this.transform.position.x, -0.3f, this.transform.position.z), Quaternion.identity);
        }
        else if (!this.firstHalf && Mathf.Clamp(this.lerpNum, 0, 1) == 1)
        {
            Destroy(this.gameObject);
        }
    }
}
