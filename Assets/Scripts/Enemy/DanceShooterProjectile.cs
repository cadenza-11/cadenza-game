using Cadenza;
using UnityEngine;

public class DanceShooterProjectile : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float timer = 0.0f;
    private Rigidbody rb;
    private Player target;
    private Vector3 p0;
    private Vector3 p1;
    private Vector3 p2;
    private Vector3 p3;
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
        this.timer += Time.deltaTime;
        if(this.timer <= 1.5f)
        {
            this.p3 = this.target.Character.transform.position;
            this.CalculateCurvePoints(this.p0, this.p3, out this.p1, out this.p2);
        }
        this.rb.transform.position = this.BezierCurve(this.timer, this.p0, this.p1, this.p2, this.p3);
        if(this.rb.transform.position == this.p3)
        {
            Destroy(this.gameObject);
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        float knockbackMultiplier = 1f;

        if (collider.CompareTag("Player"))
        {
            Character hitEntity = collider.gameObject.GetComponent<Character>();
            if (hitEntity != null && !hitEntity.TakeDamage(this.damage, out knockbackMultiplier))
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
            collider.attachedRigidbody.AddForce(direction.normalized * this.knockbackScale * knockbackMultiplier, ForceMode.Impulse);
        }
        Destroy(this.gameObject);
    }

    Vector3 BezierCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        return ((1-t) * (1-t) * (1-t)*p0) + (3 * (1-t) * (1-t) * t * p1) + (3 * (1-t) * t * t * p2) + (t * t * t * p3);
    }

    void CalculateCurvePoints(Vector3 start, Vector3 end, out Vector3 p1, out Vector3 p2)
    {
        Vector3 difference = end - start;
        p1 = new Vector3(start.x + difference.x/3.0f, start.y + difference.y/3.0f, start.z + difference.y/3.0f);
        p2 = new Vector3(start.x + difference.x * 2.0f/3.0f, start.y + difference.y * 2.0f/3.0f, start.z + difference.z * 2.0f/3.0f);
    }

    public void SetP0(Vector3 start)
    {
        this.p0 = start;
    }

    public void SetPlayer(Player p)
    {
        this.target = p;
    }
}
