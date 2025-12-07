using Cadenza;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float timer = 0.0f;
    private Rigidbody rb;
    public bool direction;
    public bool speedSet = true;
    [SerializeField] private int speed = 0;
    [SerializeField] private int damage = 2;
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
        Debug.Log("dealt " + this.damage + " damage");
        if(collider.CompareTag("Player"))
        {
            Character hitEntity = collider.gameObject.GetComponent<Character>();
            hitEntity.DoDamage(this.damage);
        }
        else
        {
            Debug.Log("dealt" + this.damage + "damage");
        }
        if(collider.CompareTag("Enemy"))
        {
            Debug.Log("AttackArea: Area collided with Enemy");
            Enemy hitEntity = collider.gameObject.GetComponent<Enemy>();
            hitEntity.DoDamage(2);
        }
        Destroy(this.gameObject);
    }
}
