using UnityEngine;
using UnityEngine.InputSystem;

public class TestPlayerScript : MonoBehaviour, ICharacter
{
    //All floats, determine the player's speed, jump force, and time it takes to attack. Currently changed in the editor
    public float speed, jumpForce, chargeForce;
    private float attackDuration = 0.25f, chargeDuration = 0.5f, attackTimer = 0f, chargeTimer = 0f;

    //Needed for Interface, does nothing rn
    public int currentHealth { get; set; }
    public int specialMeter { get; set; }
    private int attackMod;

    //y only jump vector
    public Vector3 jump, charge;

    //Random components
    public Rigidbody rb;
    public SpriteRenderer sr;
    public InputAction moveP, jumpP, weakAttackP, strongAttackP, specialAttackP;
    public Animator anim;
    private GameObject attackArea = default, chargeArea = default;
    private AttackArea attackScript, chargeScript;

    //Bools for animation, attacking, and jumping
    public bool isMove;
    private bool isAttacking, isGrounded, isCharging;
    private bool direction; //true = right, false = left

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.attackArea = this.transform.GetChild(0).gameObject;
        this.chargeArea = this.transform.GetChild(1).gameObject;
        this.attackScript = this.attackArea.GetComponent<AttackArea>();
        this.chargeScript = this.chargeArea.GetComponent<AttackArea>();
        this.jump = new Vector3(0.0f, 2.0f, 0.0f);
        this.charge = new Vector3(2.0f, 0.0f, 0.0f);
    }

    private void OnEnable()
    {
        //Enables all the inbuild inputActions, will change to an imported system later
        this.moveP.Enable();
        this.jumpP.Enable();
        this.weakAttackP.Enable();
        this.strongAttackP.Enable();
        this.specialAttackP.Enable();
        this.weakAttackP.performed += this.WeakAttack;
        this.strongAttackP.performed += this.StrongAttack;
        this.specialAttackP.performed += this.SpecialAttack;
        this.jumpP.performed += this.JumpCommand;
    }

    private void OnDisable()
    {
        //Disables input actions when done
        this.moveP.Disable();
        this.jumpP.Disable();
        this.weakAttackP.Disable();
        this.strongAttackP.Disable();
        this.specialAttackP.Disable();
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        //Only adds gravity if in air
        this.isGrounded = this.CheckIsGrounded();

        if (!this.isGrounded && !this.isCharging)
        {
            this.rb.AddForce(Physics.gravity * 1f, ForceMode.Acceleration);
        }

        //Reads in a Vector2, converts it to a Vector3, and flips sprite based on direction

        if (!this.isCharging)
        {
            Vector2 tempMov = this.moveP.ReadValue<Vector2>();

            Vector3 moveDir = new Vector3(tempMov.x * this.speed, this.rb.linearVelocity.y, tempMov.y * this.speed);
            this.rb.linearVelocity = moveDir;

            if (moveDir.x != 0 && moveDir.x < 0)
            {
                this.sr.flipX = true;
                this.isMove = true;
                this.direction = false;
            }
            else if (moveDir.x != 0 && moveDir.x > 0)
            {
                this.sr.flipX = false;
                this.isMove = true;
                this.direction = true;
            }
            else if (Mathf.Abs(moveDir.z) > 0)
            {
                this.isMove = true;
            }
            else if (moveDir.x == 0)
            {
                this.isMove = false;
            }
        }

        this.anim.SetBool("IsMove", this.isMove);

        //Runs timer so player cant attack more than once (may become an IEnumerator later if more effective)
        if (this.isAttacking && !this.isCharging)
        {
            this.attackTimer += Time.deltaTime;

            if (this.attackTimer >= (this.attackDuration * this.attackMod)) 
            {
                this.attackTimer = 0;
                this.isAttacking = false;
                this.attackArea.SetActive(this.isAttacking);
            }
        }

        if (this.isCharging)
        {
            this.chargeTimer += Time.deltaTime;

            if (this.chargeTimer >= (this.chargeDuration))
            {
                this.chargeTimer = 0;
                this.isCharging = false;
                this.chargeArea.SetActive(this.isCharging);
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
        this.isAttacking = true;
        this.attackMod = 1;
        this.attackScript.damage = 3;
        this.anim.SetTrigger("WeakAttack");
        this.attackArea.SetActive(this.isAttacking);
    }
    public void StrongAttack(InputAction.CallbackContext context)
    {
        this.isAttacking = true;
        this.attackMod = 2;
        this.attackScript.damage = 6;
        this.anim.SetTrigger("StrongAttack");
        this.attackArea.SetActive(this.isAttacking);
    }
    public void SpecialAttack(InputAction.CallbackContext context)
    {
        if (this.direction == true)
        {
            this.chargeForce = Mathf.Abs(this.chargeForce);
        }
        else if (this.direction == false)
        {
            this.chargeForce = -Mathf.Abs(this.chargeForce);
        }
        this.isCharging = true;
        this.chargeScript.damage = 10;
        this.chargeArea.SetActive(this.isCharging);
        this.rb.linearVelocity = new Vector3(0.0f, 0.0f, 0.0f);
        this.rb.AddForce(this.charge * this.chargeForce, ForceMode.VelocityChange);
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
