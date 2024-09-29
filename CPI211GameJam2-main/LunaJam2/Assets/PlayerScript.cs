using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    // Start is called before the first frame update
    const int MAXHEALTH = 3;
    [SerializeField] int CurrHealth = 3;

    void Start()
    {
        
    }

    public void takeDamage()
    {
        if (CurrHealth > 0)
        {
            CurrHealth--;
            checkHealth();
        }
        else
        {
            return;
        }
    }

    private void checkHealth()
    {
        if(CurrHealth == 0)
        {
            Debug.Log("GAME OVER!");
            SceneManager.LoadScene("SampleScene");
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Trap"))
        {
            takeDamage();
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
