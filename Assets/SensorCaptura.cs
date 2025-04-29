using UnityEngine;

public class Detenido : MonoBehaviour
{
    public Ladron scriptLadron;
    private Policia agente;

    private void Start()
    {
        agente = GetComponentInParent<Policia>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            agente.DetenerLadron();
            scriptLadron.enabled = false;
            Debug.Log("¡El ladrón ha sido capturado! Fin del juego.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
