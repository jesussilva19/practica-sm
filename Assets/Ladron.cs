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

        Vector3 movimiento = new Vector3(horizontal, 0, vertical);

        if (movimiento.magnitude > 1)
        {
            movimiento.Normalize(); // Normalizar el vector para evitar velocidad mayor en diagonales
        }

        transform.Translate(movimiento * velocidad * Time.deltaTime);
    }
}
