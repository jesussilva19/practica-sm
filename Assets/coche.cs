using UnityEngine;

public class Coche : MonoBehaviour
{
    public GameObject textoVictoria; // Referencia al texto de victoria

    private void OnTriggerEnter(Collider other)
    {
        // Verifica si el objeto que colisiona es el ladrón
        Ladron ladron = other.GetComponent<Ladron>();
        if (ladron != null && ladron.oro) // Comprueba si el ladrón lleva el oro
        {
            Debug.Log("¡Has ganado el juego!");
            Time.timeScale = 0; // Detiene el juego
            textoVictoria.SetActive(true); // Muestra el texto de victoria

        }
    }
}