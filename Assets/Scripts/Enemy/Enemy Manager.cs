using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    //Initial enemies in a scene will be placed in editor
    [SerializeField] private List<GameObject> enemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    //Removes an enemy as being in the scene once they die. Death logic will be placed in another script
    //May want to put an equals operator for enemy to not rely on references (?)
    bool EnemyDeath(GameObject enemy)
    {
        for (int i = 0; i < this.enemies.Count; i++)
        {
            if (ReferenceEquals(this.enemies[i], enemy))
            {
                this.enemies.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    
    void AddEnemy(GameObject enemy)
    {
        this.enemies.Add(enemy);
    }
}
