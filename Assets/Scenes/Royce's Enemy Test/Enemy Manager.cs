using UnityEngine;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{

    public List<GameObject> enemies;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    void EnemyDeath(GameObject enemy)
    {
        for(int i = 0; i < enemies.Count; i++)
        {
            if(ReferenceEquals(enemies[i], enemy))
            {
                enemies.RemoveAt(i);
            }
        }
    }
}
