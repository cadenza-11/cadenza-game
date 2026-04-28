using UnityEngine;

public class TeamWave : MonoBehaviour
{
    private float timer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.timer = 0.1f;
    }

    // Update is called once per frame
    void Update()
    {
        //Time the wave is active is equal to the number in the if statement divided by the that delta time is multiplied by
        this.timer += Time.deltaTime * 3f;
        this.gameObject.transform.localScale = new Vector3(this.timer, 1f, this.timer);
        if(this.timer > 6f)
        {
            Destroy(this.gameObject);
        }
    }
}
