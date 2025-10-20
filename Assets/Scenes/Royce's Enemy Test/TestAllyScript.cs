using UnityEngine;


public enum AllyState
{
    Idle,
    Moving,
    Fighting,
    LowHealth
}
public class TestAllyScript : MonoBehaviour
{
    public AllyState currentState;
    public float speed;

    public float currentHealth;
    public float maxHealth;

    public LayerMask floorLayer;
    public Rigidbody rb;
    public SpriteRenderer sr;
    public Animator anim;

    public bool isMove;

    private GameObject attackArea = default;

    private bool attacking = false;

    private float timeToAttack = 0.25f;
    private float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        attackArea = transform.GetChild(0).gameObject;
        currentState = AllyState.Idle;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (currentState)
        {
            case AllyState.Idle:
                IdleState();
                return;
        }
    }

    void IdleState()
    {
        
    }

    void WalkingState()
    {

    }

    void AttackingState()
    {

    }
    
    void LowHealthState()
    {
        
    }
}
