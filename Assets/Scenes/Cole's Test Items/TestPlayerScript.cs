using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour
{
    //This is just taken fron the tutorial, can be adapted later
    public float speed;
    public float groundDist;

    public LayerMask floorLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction pc;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player Rigidbody
        rb = gameObject.GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        pc.Enable();
    }

    private void OnDisable()
    {
        pc.Disable();
    }
    // Update is called once per frame
    void Update()
    {
        //Shoot a line down to find the terrain, then set height just above terrain
        RaycastHit hit;
        Vector3 castPos = transform.position;
        castPos.y += 1;
        if(Physics.Raycast(castPos, -transform.up, out hit, Mathf.Infinity, floorLayer))
        {
            if(hit.collider != null)
            {
                Vector3 movePos = transform.position;
                movePos.y = hit.point.y + groundDist;
                transform.position = movePos;
            }
        }

        //Move player and flip sprite
        Vector3 moveDir = pc.ReadValue<Vector3>();
        rb.linearVelocity = moveDir * speed;

        /*if (x != 0 &&  x < 0)
        {
            sr.flipX = true;
        }
        else if (x != 0 && x > 0)
        {
            sr.flipX = false;
        }
        */
    }
}
