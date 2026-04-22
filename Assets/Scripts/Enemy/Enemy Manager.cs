using UnityEngine;
using System.Collections.Generic;

namespace Cadenza
{
    public class EnemyManager : MonoBehaviour
    {
        private static EnemyManager singleton;

        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject gruntPrefab;

        private readonly HashSet<Enemy> enemies = new();

        public static int EnemyCount => singleton.enemies.Count;

        void Awake()
        {
            Debug.Assert(singleton == null);
            singleton = this;

            if (GameManager.IsCombatActive)
                this.OnCombatStart();
            else
                GameManager.CombatStarted += this.OnCombatStart;
        }

        private void OnCombatStart()
        {
            foreach (var enemy in this.enemies)
                enemy.Initialize();
        }

        public static bool RemoveEnemy(Enemy enemy)
        {
            if (singleton.enemies.Remove(enemy))
            {
                Debug.Log("Deleted Enemy");
                Destroy(enemy.gameObject);
                return true;
            }
            return false;
        }

        public static bool AddEnemy(Enemy enemy)
        {
            if (!singleton.enemies.Contains(enemy))
            {
                singleton.enemies.Add(enemy);
                return true;
            }
            return false;
        }

        public static void GroupAttack()
        {
            Debug.Log("Starting Group Attack");
            int numChosen = 0;
            Vector2 location = Vector2.zero;
            Player follow = null;
            foreach(var enemy in singleton.enemies)
            {
                if(enemy is EnemyGrunt)
                {
                    float chance = Random.Range(0f, 1f);
                    if(chance < 0.75)
                    {
                        if(location == Vector2.zero)
                        {
                            location = enemy.GetTargetLocation();
                            follow = enemy.GetFollow();
                        }
                        EnemyGrunt curEnemy = (EnemyGrunt)enemy;
                        curEnemy.SetFollow(follow);
                        curEnemy.GroupAttack(location);
                        numChosen++;
                    }
                }
                if(numChosen >= 12)
                {
                    return;
                }
            }
        }

        public static void GruntPhase()
        {
            //chooses a random number between 8 and 15 for the number of grunts spawned
            for(int i = 0; i < Random.Range(1, 2); i++)
            {
                Vector3 position = new Vector3(Random.Range(-7.0f, 7.0f), 6, Random.Range(1.0f, 10.0f));
                Instantiate(singleton.gruntPrefab, position, Quaternion.identity);
            }
        }

        public static bool CheckGrunts(EnemyGrunt g)
        {
            foreach(var enemy in singleton.enemies)
            {
                if(enemy is EnemyGrunt && enemy != g)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
