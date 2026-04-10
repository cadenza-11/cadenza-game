using UnityEngine;

public class RockBassShockwaveLightController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] private GameObject lightS;
    [SerializeField] private GameObject lightM;
    [SerializeField] private GameObject lightL;
    [SerializeField] private GameObject Base;
    void Start()
    {
        if(this.Base.transform.localScale.x == 1.0f)
        {
            this.lightS.SetActive(true);
        }
        else if (this.Base.transform.localScale.x == 1.25f)
        {
            this.lightM.SetActive(true);
        }
        else if (this.Base.transform.localScale.x == 1.5f)
        {
            this.lightL.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
