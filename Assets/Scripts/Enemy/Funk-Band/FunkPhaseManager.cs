using Cadenza;
using System;
using UnityEngine;

public class FunkPhaseManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject gruntEnemy;
    [SerializeField] private GameObject mainBoss;
    [SerializeField] private GameObject BossOne;
    [SerializeField] private GameObject BossTwo;

    private GameObject bigBoss;
    public int phase;
    private bool combatStart = false;

    void Start()
    {
        this.phase = 0;
        GameManager.CombatStarted += this.OnCombatStart;
        this.bigBoss = Instantiate(this.mainBoss, new Vector3(0.4f, 2f, 10f), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.combatStart)
        {
            this.PhaseCheck();
        }
    }

    void OnCombatStart()
    {
        this.combatStart = true;
    }

    void PhaseZeroStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 8f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 8f), Quaternion.identity);
        this.phase++;
    }

    void PhaseOneStart()
    {
        Instantiate(this.BossOne, new Vector3(0f, 0f, 7f), Quaternion.identity);
        this.phase++;
    }

    void PhaseTwoStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, 0f, 8f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, 0f, 8f), Quaternion.identity);
        this.phase++;
    }

    void PhaseThreeStart()
    {
        Instantiate(this.BossTwo, new Vector3(0f, 0f, 7f), Quaternion.identity);
        this.phase++;
    }

    void PhaseCheck()
    {
        if(EnemyManager.EnemyCount <= 1)
        {
            Debug.Log("Moving to Phase " + this.phase);
            switch (this.phase)
            {
                case (0):
                    this.PhaseZeroStart();
                    break;

                case (1):
                    this.PhaseOneStart();
                    break;

                case (2):
                    this.PhaseTwoStart();
                    break;

                case (3):
                    this.PhaseThreeStart();
                    break;

                case (4):
                    this.bigBoss.GetComponent<MainFunkEnemy>().currentHealth = 0;
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        GameManager.CombatStarted -= this.OnCombatStart;
    }

}
