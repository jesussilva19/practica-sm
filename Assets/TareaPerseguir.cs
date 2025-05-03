using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class TareaPerseguir : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        // La tarea es ejecutable si el policía está viendo al ladrón, tiene su referencia y no está ocupado
        return policia.ladronViendo && policia.thiefTransform != null && !policia.ocupado;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        // Pausar la patrulla
        policia.isPatrolling = false;

        if (policia._navAgent == null || !policia._navAgent.isOnNavMesh)
        {
            Debug.LogError($"Policia {policia.AgentId}: NavMeshAgent no está disponible o no está en el NavMesh.");
            yield break;
        }

        // Reiniciar el camino actual
        policia._navAgent.ResetPath();
        Debug.Log($"Policia {policia.AgentId}: Pausando patrulla y comenzando persecución.");

        // Bucle de persecución
        while (policia.ladronViendo && policia.thiefTransform != null)
        {
            // Actualizar el destino del NavMeshAgent hacia la posición del ladrón
            if (policia._navAgent.isOnNavMesh)
            {
                policia._navAgent.SetDestination(policia.thiefTransform.position);
                Debug.Log($"Policia {policia.AgentId}: Persiguiendo al ladrón en {policia.thiefTransform.position}");
            }

            // Verificar si el policía está lo suficientemente cerca del ladrón para detenerlo
            /*if (Vector3.Distance(policia.transform.position, policia.thiefTransform.position) <= 1.5f)
            {
                Debug.Log($"Policia {policia.AgentId}: Ladrón alcanzado. Deteniendo persecución.");
                policia.DetenerLadron();
                yield break;
            }*/

            yield return new WaitForSeconds(0.2f); // Actualiza cada 0.2 segundos
        }

    }
}