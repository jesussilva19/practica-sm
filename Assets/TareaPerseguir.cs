
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TareaPerseguir : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        return policia.thiefDetected && policia.thiefTransform != null;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        policia.PausarPatrulla();
        Debug.Log($"[HTN] Ejecutando TareaPerseguir para {policia.AgentId}");

        NavMeshAgent agent = policia.GetComponent<NavMeshAgent>();

        while (policia.thiefDetected && policia.thiefTransform != null)
        {
            if (agent != null && agent.isOnNavMesh)
            {
                agent.SetDestination(policia.thiefTransform.position);
            }

            yield return new WaitForSeconds(0.2f); // Actualiza cada 0.2s
        }

        Debug.Log($"[HTN] {policia.AgentId} dejó de perseguir");
    }
}
