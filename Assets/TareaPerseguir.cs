using System.Collections;
using UnityEngine.AI;

public class TareaPerseguir : TareaHTN
{
    public override bool EsEjecutable(Agente agente)
    {
        return agente.ladronDetectado && agente.ladronTransform != null;
    }

    public override IEnumerator Ejecutar(Agente agente)
    {
        agente.PausarPatrulla();
        agente.GetComponent<NavMeshAgent>().SetDestination(agente.ladronTransform.position);

        while (agente.ladronDetectado)
        {
            agente.GetComponent<NavMeshAgent>().SetDestination(agente.ladronTransform.position);
            yield return null;
        }

        agente.ReanudarPatrulla();
    }
}
