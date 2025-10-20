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
    private int attackMod;

    //y only jump vector
    public Vector3 jump;

    //Random components
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP, weakAttackP, strongAttackP;
    public Animator anim;
    private GameObject attackArea = default;
    private AttackArea attackScript;

    //Bools for animation, attacking, and jumping
    public bool isMove;
    public bool isGrounded;
    private bool attacking;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.attackArea = this.transform.GetChild(0).gameObject;
        this.attackScript = this.attackArea.GetComponent<AttackArea>();
        this.jump = new Vector3(0.0f, 2.0f, 0.0f);
    }

    private void OnEnable()
    {
        //Enables all the inbuild inputActions, will change to an imported system later
        this.moveP.Enable();
        this.jumpP.Enable();
        this.weakAttackP.Enable();
        this.strongAttackP.Enable();
        this.weakAttackP.performed += this.WeakAttack;
        this.strongAttackP.performed += this.StrongAttack;
        this.jumpP.performed += this.JumpCommand;
    }

    private void OnDisable()
    {
        //Disables input actions when done
        this.moveP.Disable();
        this.jumpP.Disable();
        this.weakAttackP.Disable();
        this.strongAttackP.Disable();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Only adds gravity if in air
        this.isGrounded = this.CheckIsGrounded();

        if (this.isGrounded == false)
        {
            this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
        }

        //Reads in a Vector2, converts it to a Vector3, and flips sprite based on direction
        Vector2 tempMov = this.moveP.ReadValue<Vector2>();

        Vector3 moveDir = new Vector3(tempMov.x * this.speed, this.rb.linearVelocity.y, tempMov.y * this.speed);
        this.rb.linearVelocity = moveDir;

        if (moveDir.x != 0 && moveDir.x < 0)
        {
            this.sr.flipX = true;
            this.isMove = true;
        }
        else if (moveDir.x != 0 && moveDir.x > 0)
        {
            this.sr.flipX = false;
            this.isMove = true;
        }
        else if (Mathf.Abs(moveDir.z) > 0)
        {
            this.isMove = true;
        }
        else if (moveDir.x == 0)
        {
            this.isMove = false;
        }

        this.anim.SetBool("IsMove", this.isMove);

        //Runs timer so player cant attack more than once (may become an IEnumerator later if more effective)
        if (this.attacking)
        {
            this.timer += Time.deltaTime;

            if (this.timer >= (this.timeToAttack * this.attackMod)) 
            {
                this.timer = 0;
                this.attacking = false;
                this.attackArea.SetActive(this.attacking);
            }
        }


    }

    bool CheckIsGrounded()
    {
        //Returns a raycast result to determine if on the ground
        return Physics.Raycast(this.transform.position, -Vector3.up, 0.5f);
    }

    private void JumpCommand(InputAction.CallbackContext context)
    {
        //Jump input action command, only jumps if on the ground
        if (this.isGrounded)
        {
            this.rb.AddForce(this.jump * this.jumpForce, ForceMode.Impulse);
        }
    }

    public void WeakAttack(InputAction.CallbackContext context)
    {
        //Sets attacking to true and activated the hitbox for the attack
        this.attacking = true;
        this.attackMod = 1;
        this.attackScript.damage = 3;
        this.anim.SetTrigger("WeakAttack");
        this.attackArea.SetActive(this.attacking);
    }
    public void StrongAttack(InputAction.CallbackContext context)
    {
        this.attacking = true;
        this.attackMod = 2;
        this.attackScript.damage = 6;
        this.anim.SetTrigger("StrongAttack");
        this.attackArea.SetActive(this.attacking);
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
