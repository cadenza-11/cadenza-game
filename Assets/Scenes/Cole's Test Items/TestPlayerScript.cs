using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour, ICharacter
{
    //This is just taken fron the tutorial, can be adapted later
    public float speed;

    public int currentHealth { get; set; }
    public int specialMeter { get; set; }


    public LayerMask floorLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP, attackP;
    public Animator anim;

    public bool isMove;
    public bool isGrounded;

    private GameObject attackArea = default;

    private bool attacking = false;

    private float timeToAttack = 0.25f;
    private float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackArea = transform.GetChild(0).gameObject;
    }

    private void OnEnable()
    {
        moveP.Enable();
        jumpP.Enable();
        attackP.Enable();
        attackP.performed += AttackCommand;
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

        rb.AddForce(Physics.gravity * 10f, ForceMode.Acceleration);

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

        if (attacking)
        {
            timer += Time.deltaTime;

            if (timer >= timeToAttack)
            {
                timer = 0;
                attacking = false;
                attackArea.SetActive(attacking);
            }
        }
    }

    private void AttackCommand(InputAction.CallbackContext context)
    {
        anim.SetTrigger("PlayerAttack");
        WeakAttack();
    }

    public void WeakAttack()
    {
        attacking = true;
        attackArea.SetActive(attacking);
    }
    public void StrongAttack()
    {

    }
    public void SpecialAttack()
    {

    }
    public void StartTeamAttk()
    {

    }
    public void JoinTeamAttk()
    {

    }
    public void DoDamage()
    {

    }
}
