using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour
{
    //This is just taken fron the tutorial, can be adapted later
    public float speed;

    public LayerMask floorLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP, attackP;
    public Animator anim;

    public bool isMove;
    public bool isAttacking;
    public int attackEnd;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Find player Rigidbody
    }

    private void OnEnable()
    {
        moveP.Enable();
        jumpP.Enable();
        attackP.Enable();
        attackP.performed += Attack;
    }

    private void OnDisable()
    {
        moveP.Disable();
        jumpP.Disable();
        attackP.Disable();
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

    public void nextAnim(int next)
    {
        attackEnd = next;
    }

    private void Attack(InputAction.CallbackContext context)
    {
        anim.SetTrigger("PlayerAttack");
    }
}
