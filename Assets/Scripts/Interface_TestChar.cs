using UnityEngine;

public class Interface_TestChar : MonoBehaviour, ICharacter
{

    public int currentHealth { get; set; }
    public int specialMeter { get; set; }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = 100;
        specialMeter = 0;
        Debug.Log("Test script works");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void Move(Vector2 input)
    {

    }

    //Changed the parameters because this lets the character directly call the weak command out of an input action, lmk if you have any questions
    // - Cole
    public void WeakAttack()
    {

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
