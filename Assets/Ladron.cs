using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Ladron : MonoBehaviour
{
    public float velocidad = 5f;
    public bool oro = false;

    void Update()
    {
        
        float horizontal = Input.GetAxis("Horizontal"); 
        float vertical = Input.GetAxis("Vertical");     

        Vector3 movimiento = new Vector3(horizontal, 0, vertical);

        if (movimiento.magnitude > 1)
        {
            movimiento.Normalize(); 
        }

        transform.Translate(movimiento * velocidad * Time.deltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto con el que colisiona es el oro
        if (other.GetComponent<Oro>() != null)
        {
            oro = true; // Actualiza el estado del ladrón para indicar que lleva el oro
        }
    }
}
