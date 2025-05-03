using System.Collections;
using UnityEngine;

public class TareaPatrullar : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        // The task is executable only if the police officer is not seeing the thief and is not busy
        return !policia.ladronViendo && !policia.ocupado && policia.isPatrolling && !policia.isSearching;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        // Ensure the police officer is patrolling and not busy
        if (!EsEjecutable(policia)) yield break;

        // Check if the NavMeshAgent is valid and ready to move
        if (policia._navAgent == null || !policia._navAgent.isOnNavMesh) yield break;

        // Check if the police officer has reached the current destination
        if (!policia._navAgent.pathPending && policia._navAgent.remainingDistance <= 0.2f)
        {
            // Ensure there are patrol waypoints
            if (policia.destinos == null || policia.destinos.Length == 0) yield break;

            // Move to the next waypoint
            policia._currentWaypointIndex = (policia._currentWaypointIndex + 1) % policia.destinos.Length;
            var next = policia.destinos[policia._currentWaypointIndex];

            if (next != null)
            {
                policia._navAgent.SetDestination(next.position);
                Debug.Log($"Policia {policia.AgentId}: Moving to waypoint {policia._currentWaypointIndex}");
            }
        }

        yield return null;
    }
}