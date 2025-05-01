
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TareaDarRefuerzo : TareaHTN
{
    private Vector3 posicionRefuerzo;

    public TareaDarRefuerzo(Vector3 posicion)
    {
        posicionRefuerzo = posicion;
    }

    public override bool EsEjecutable(Agente agente)
    {
        // Siempre se puede intentar acudir si no está ocupado
        return true;
    }

    public override IEnumerator Ejecutar(Agente agente)
    {
        agente.PausarPatrulla();
        agente.GetComponent<NavMeshAgent>().SetDestination(posicionRefuerzo);

        while (agente.GetComponent<NavMeshAgent>().pathPending || agente.GetComponent<NavMeshAgent>().remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log($"{agente.AgentId}: Llegó a la zona de refuerzo.");
        yield return new WaitForSeconds(3f); // Simular espera

        agente.ReanudarPatrulla();
    }
}
