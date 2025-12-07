using UnityEngine;
using System.Collections.Generic;
using Cadenza;

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
    void Update()
    {
        /*if(this.enemies.Count == 0)
        {
            GameObject newEnemy = Instantiate(this.enemyPrefab, Vector3.zero, Quaternion.identity);
            this.AddEnemy(newEnemy);
        }*/ 
        //Add later
    }

    //Removes an enemy as being in the scene once they die. Death logic will be placed in another script
    //May want to put an equals operator for enemy to not rely on references (?)
    public bool RemoveEnemy(GameObject enemy)
    {
        Debug.Log("Goes into Remove Enemy");
        for (int i = 0; i < this.enemies.Count; i++)
        {
            if (ReferenceEquals(this.enemies[i], enemy))
            {
                Debug.Log("Tries to Remove Enemy");
                Destroy(this.enemies[i]);
                this.enemies.RemoveAt(i);
                return true;
            }
        }
        return false;
    }
    
    public void AddEnemy(GameObject enemy)
    {
        Debug.Log("Adds Enemy");
        this.enemies.Add(enemy);
    }
}
