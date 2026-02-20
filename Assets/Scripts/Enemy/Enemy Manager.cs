using UnityEngine;
using System.Collections.Generic;

namespace Cadenza
{
    public class EnemyManager : MonoBehaviour
    {
        public static EnemyManager singleton;
        //Initial enemies in a scene will be placed in editor
        [SerializeField] private List<GameObject> enemies;
        [SerializeField] private GameObject enemyPrefab;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void FixedUpdate()
        {
            /*if(this.enemies.Count < PlayerSystem.PlayerCount)
            {
                Debug.Log("Need more enemies");
                for(int i = this.enemies.Count; i <= PlayerSystem.PlayerCount; i++) {
                    GameObject newEnemy = Instantiate(this.enemyPrefab, Vector3.zero, Quaternion.identity);
                    this.AddEnemy(newEnemy);
                }
            }*/
            if (this.enemies.Count == 0)
            {
                Debug.Log("No enemies");
                GameObject newEnemy = Instantiate(this.enemyPrefab, Vector3.zero, Quaternion.identity);
                this.AddEnemy(newEnemy);
            }
            else if (this.enemies[0] == null)
            {
                Debug.Log("First enemy is null");
                GameObject newEnemy = Instantiate(this.enemyPrefab, Vector3.zero, Quaternion.identity);
                this.AddEnemy(newEnemy);
                this.enemies.RemoveAt(0);
            }
        }

        //Removes an enemy as being in the scene once they die. Death logic will be placed in another script
        //May want to put an equals operator for enemy to not rely on references (?)
        public bool RemoveEnemy(GameObject enemy)
        {
            Debug.Log("Goes into Remove Enemy");
            /*for (int i = 0; i < this.enemies.Count; i++)
            {
                if (ReferenceEquals(this.enemies[i], enemy))
                {
                    Debug.Log("Tries to Remove Enemy");
                    GameObject enemyToDestroy = this.enemies[i];
                    this.enemies.RemoveAt(i);
                    Destroy(enemyToDestroy);
                    return true;
                }
            }*/
            this.enemies.Remove(enemy);
            Destroy(enemy);
            this.enemies.RemoveAll(x => !x);
            return false;
        }

        public void AddEnemy(GameObject enemy)
        {
            Debug.Log("Adds Enemy");
            this.enemies.Add(enemy);
        }
    }
}
