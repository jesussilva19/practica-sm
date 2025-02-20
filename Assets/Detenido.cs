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
            Debug.Log("¡Ladrón atrapado! Deteniendo a ambos.");
            agentePolicia.isStopped = true;
            scriptLadron.enabled = false; 
        }
    }
}
