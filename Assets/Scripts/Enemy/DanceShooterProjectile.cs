using Cadenza;
using UnityEngine;

public class DanceShooterProjectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float timer = 0.0f;
    private Rigidbody rb;
    public Vector2 direction;
    public bool speedSet = true;
    public float knockbackScale;
    [SerializeField] private int speed = 1;
    [SerializeField] private int damage = 2;
    void Start()
    {
        this.rb = this.gameObject.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(this.direction.y == 1)
        {
            //Debug.Log(this.rb.linearVelocity.x + ", " + this.rb.linearVelocity.y + ", " + this.rb.linearVelocity.z);
            //Debug.Log(this.speed * this.direction.y);
        }
        this.timer += Time.deltaTime;
        if (this.timer > 5.0f)
            Destroy(this.gameObject);

        if (this.speedSet == false)
        {
            Vector3 moveDir = new Vector3(this.speed * this.direction.x, 0, this.speed * this.direction.y);
            this.rb.linearVelocity = moveDir;
            if(this.direction.y == 1)
            {
                //Debug.Log(moveDir.z + ", " + this.rb.linearVelocity.y);
            }
            this.speedSet = true;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Player"))
        {
            Character hitEntity = collider.gameObject.GetComponent<Character>();
            if (hitEntity != null && !hitEntity.TakeDamage(this.damage))
            {
                Destroy(this.gameObject);
                return;
            }
        }
        if (collider.CompareTag("Enemy"))
        {
            Enemy hitEntity = collider.gameObject.GetComponent<Enemy>();
            hitEntity.TakeDamage(2);
        }

        if (collider.attachedRigidbody != null)
        {
            Vector3 direction = collider.transform.position - this.transform.position;
            collider.attachedRigidbody.AddForce(direction.normalized * this.knockbackScale, ForceMode.Impulse);
        }
        Destroy(this.gameObject);
    }
}