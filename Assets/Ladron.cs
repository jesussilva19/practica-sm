using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Ladron : MonoBehaviour
{
    public float velocidad = 5f;

    void Update()
    {
        
        float horizontal = Input.GetAxis("Horizontal"); 
        float vertical = Input.GetAxis("Vertical");     

        Vector3 movimiento = new Vector3(horizontal, 0, vertical) * velocidad * Time.deltaTime;

        
        transform.Translate(movimiento);
    }
}
