using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour
{
    //This is just taken fron the tutorial, can be adapted later
    public float speed;

    public LayerMask floorLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP;
    public Animator anim;

    public bool isMove;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player Rigidbody
    }

    private void OnEnable()
    {
        moveP.Enable();
    }

    private void OnDisable()
    {
        moveP.Disable();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Shoot a line down to find the terrain, then set height just above terrain

        //Move player and flip sprite
        Vector3 moveDir = moveP.ReadValue<Vector3>();
        rb.linearVelocity = moveDir * speed;

        if (moveDir.x != 0 && moveDir.x < 0)
        {
            sr.flipX = true;
            isMove = true;
        }
        else if (moveDir.x != 0 && moveDir.x > 0)
        {
            sr.flipX = false;
            isMove = true;
        }
        else if (Mathf.Abs(moveDir.z) > 0)
        {
            isMove = true;
        }
        else if (moveDir.x == 0)
        {
            isMove = false;
        }

        anim.SetBool("IsMove", isMove);
    }
}
