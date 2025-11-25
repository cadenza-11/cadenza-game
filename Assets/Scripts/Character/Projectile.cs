using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float timer = 0.0f;
    private Rigidbody rb;
    public bool direction;
    public bool speedSet = true;
    [SerializeField] private int speed = 0;
    void Start()
    {
        this.rb = this.gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.timer += Time.deltaTime;
        if (this.timer > 5.0f)
        {
            Destroy(this.gameObject);
        }
        if(this.speedSet == false)
        {
            int dirNum;
            if (this.direction == true)
            {
                dirNum = 1;
            } else
            {
                dirNum = -1;
            }
            Vector3 moveDir = new Vector3(this.speed * dirNum, 0, 0);
            this.rb.linearVelocity = moveDir;
            this.speedSet = true;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        Debug.Log("dealt" + 5 + "damage");
        Destroy(this.gameObject);
    }
}
