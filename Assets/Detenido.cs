using UnityEngine;
using UnityEngine.AI;

public class Detenido : MonoBehaviour
{
    public Ladron scriptLadron; 
    private NavMeshAgent agentePolicia;

    private void Start()
    {
        agentePolicia = GetComponentInParent<NavMeshAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == scriptLadron.transform)
        {
            Debug.Log("❌ ¡Perdiste! El policía atrapó al ladrón.");

            agentePolicia.isStopped = true; 
            scriptLadron.enabled = false;   

            
            TerminarJuego();
        }
    }

    private void TerminarJuego()
    {
        Debug.Log("🛑 El juego ha terminado.");

        UnityEditor.EditorApplication.isPlaying = false;
    }
}
