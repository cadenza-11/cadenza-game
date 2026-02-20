using UnityEngine;
using System.Collections.Generic;

namespace Cadenza
{
    public class EnemyManager : MonoBehaviour
    {
        private static EnemyManager singleton;

        [SerializeField] private GameObject enemyPrefab;

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
    }
}
