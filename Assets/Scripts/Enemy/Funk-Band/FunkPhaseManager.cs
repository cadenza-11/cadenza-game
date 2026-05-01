using Cadenza;
using UnityEngine;

public class FunkPhaseManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject gruntEnemy;
    [SerializeField] private GameObject mainBoss;
    [SerializeField] private GameObject Boss;
    private GameObject bigBoss;
    private int phase;
    private bool combatStart = false;
    private float timer;

    void Start()
    {
        this.timer = 2f;
        this.phase = 0;
        GameManager.CombatStarted += this.OnCombatStart;
        this.bigBoss = Instantiate(this.mainBoss, new Vector3(0.4f, 1.04f, 10.2f), Quaternion.identity);
    }

    // Update is called once per frame
    void Update()
    {
        if (this.combatStart)
        {
            this.timer += Time.deltaTime;
            this.PhaseCheck();
        }
    }

    void OnCombatStart()
    {
        this.combatStart = true;
    }

    void PhaseZeroStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 8f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 8f), Quaternion.identity);
        this.phase++;
    }

    void PhaseOneStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(4.4f, -0.83f, 7f), Quaternion.identity);
        this.phase++;
    }

    void PhaseTwoStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 8f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 2f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 8f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(0.4f, -0.83f, 7f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(0.4f, -0.83f, 7f), Quaternion.identity);
        this.phase++;
    }

    void PhaseThreeStart()
    {
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 5f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(8f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 4f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 5f), Quaternion.identity);
        Instantiate(this.gruntEnemy, new Vector3(-7.5f, -0.83f, 6f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(0.4f, -0.83f, 7f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(4.4f, -0.83f, 7f), Quaternion.identity);
        Instantiate(this.Boss, new Vector3(-3.6f, -0.83f, 7f), Quaternion.identity);
        this.phase++;
    }

    void PhaseCheck()
    {
        if (EnemyManager.EnemyCount <= 1 && this.timer >= 3.0f)
        {
            Debug.Log("Moving to Phase " + this.phase);
            this.timer = 0;
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
                    Destroy(this.gameObject);
                    break;
            }
        }
    }

    private void OnDestroy()
    {
        GameManager.CombatStarted -= this.OnCombatStart;
    }

}
