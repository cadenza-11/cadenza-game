using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour, ICharacter
{
    //All floats, determine the player's speed, jump force, and time it takes to attack. Currently changed in the editor
    public float speed;
    public float jumpForce;
    private float timeToAttack = 0.25f;
    private float timer = 0f;

    //Needed for Interface, does nothing rn
    public int currentHealth { get; set; }
    public int specialMeter { get; set; }

    //y only jump vector
    public Vector3 jump;

    //Random components
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP, attackP;
    public Animator anim;
    private GameObject attackArea = default;

    //Bools for animation, attacking, and jumping
    public bool isMove;
    public bool isGrounded;
    private bool attacking;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackArea = transform.GetChild(0).gameObject;
        jump = new Vector3(0.0f, 2.0f, 0.0f);
    }

    private void OnEnable()
    {
        //Enables all the inbuild inputActions, will change to an imported system later
        moveP.Enable();
        jumpP.Enable();
        attackP.Enable();
        attackP.performed += AttackCommand;
        jumpP.performed += JumpCommand;
    }

    private void OnDisable()
    {
        //Disables input actions when done
        moveP.Disable();
        jumpP.Disable();
        attackP.Disable();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Only adds gravity if in air
        isGrounded = CheckIsGrounded();

        if (isGrounded == false)
        {
            rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
        }

        //Reads in a Vector2, converts it to a Vector3, and flips sprite based on direction
        Vector2 tempMov = moveP.ReadValue<Vector2>();

        Vector3 moveDir = new Vector3(tempMov.x * speed, rb.linearVelocity.y, tempMov.y * speed);
        rb.linearVelocity = moveDir;

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

        //Runs timer so player cant attack more than once (may become an IEnumerator later if more effective)
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

    bool CheckIsGrounded()
    {
        //Returns a raycast result to determine if on the ground
        return Physics.Raycast(transform.position, -Vector3.up, 0.5f);
    }

    private void AttackCommand(InputAction.CallbackContext context)
    {
        //Attack Input Action Command
        anim.SetTrigger("PlayerAttack");
        WeakAttack();
    }

    private void JumpCommand(InputAction.CallbackContext context)
    {
        //Jump input action command, only jumps if on the ground
        if (isGrounded)
        {
            rb.AddForce(jump * jumpForce, ForceMode.Impulse);
        }
    }

    public void WeakAttack()
    {
        //Sets attacking to true and activated the hitbox for the attack
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
