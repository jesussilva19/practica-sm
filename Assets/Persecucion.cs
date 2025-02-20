using UnityEngine;
using UnityEngine.AI;

public class Persecucion : MonoBehaviour
{
    public Transform ladron;
    private NavMeshAgent agentePolicia;
    private Agente patrullaPolicia;

    private void Start()
    {
        agentePolicia = GetComponentInParent<NavMeshAgent>();
        patrullaPolicia = GetComponentInParent<Agente>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            patrullaPolicia.PausarPatrulla();
            agentePolicia.SetDestination(ladron.position);
            Debug.Log("�Veo al ladr�n! Iniciando persecuci�n.");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform == ladron)
        {
            if (TieneLineaDeVision())
            {
                agentePolicia.SetDestination(ladron.position);
            }
            else
            {
                agentePolicia.ResetPath();
                patrullaPolicia.ReanudarPatrulla();
                Debug.Log("Perd� al ladr�n tras pared. Vuelvo a patrullar.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == ladron)
        {
            agentePolicia.ResetPath();
            patrullaPolicia.ReanudarPatrulla();
            Debug.Log("El ladr�n sali� del �rea. Retomando patrulla.");
        }
    }

    private bool TieneLineaDeVision()
    {
        Vector3 origen = agentePolicia.transform.position + Vector3.up * 1.0f;
        Vector3 direccion = ladron.position - agentePolicia.transform.position;
        float distancia = direccion.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(origen, direccion.normalized, out hit, distancia))
        {
            return hit.transform == ladron;
        }

        return false;
    }
}
