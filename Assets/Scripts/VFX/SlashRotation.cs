using Cadenza;
using UnityEngine;

public class SlashRotation : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private Character character;
    private float sX;
    private float sY;
    private float sZ;
    void Start()
    {
        this.sX = this.transform.localPosition.x;
        this.sY = this.transform.localPosition.y;
        this.sZ = this.transform.localPosition.z;
    }

    // Update is called once per frame
    void Update()
    {
        if (this.character.FacingRight)
        {
            // No rotation needed.
            this.transform.rotation = Quaternion.Euler(90, 0, 0);
            this.transform.localPosition = new Vector3(this.sX, this.sY, this.sZ);
        }
        else
        {
            // Turn character to the left.
            this.transform.rotation = Quaternion.Euler(90, 180, 0);
            this.transform.localPosition = new Vector3(-this.sX, this.sY, this.sZ);
        }
    }
}
