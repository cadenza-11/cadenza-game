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

        private void GroupAttack()
        {
            Vector2 location = Vector2.zero;
            foreach(var enemy in this.enemies)
            {
                if(enemy is EnemyGrunt)
                {
                    float chance = UnityEngine.Random.Range(0f, 1f);
                    if(chance < 0.5)
                    {
                        if(location == Vector2.zero)
                        {
                            location = enemy.GetTargetLocation();
                        }
                        EnemyGrunt curEnemy = (EnemyGrunt)enemy;
                        curEnemy.GroupAttack(location);
                    }
                }
            }
        }
    }
}
