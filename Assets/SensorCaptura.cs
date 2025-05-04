using UnityEngine;

public class Detenido : MonoBehaviour
{
    public Ladron scriptLadron;
    private Policia agente;

    private void Start()
    {
        agente = GetComponentInParent<Policia>();
    }

    // Se activa cuando el ladrón entra en el trigger
    // y se detiene al ladrón
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            agente.DetenerLadron();
            scriptLadron.enabled = false;
            Debug.Log("¡El ladrón ha sido capturado! Fin del juego.");
            UnityEditor.EditorApplication.isPlaying = false;
            Application.Quit();
        }
    }
}
