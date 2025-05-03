using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// HTN task for searching a sequence of points after losing sight of the thief.
/// Executed when a police agent enters search mode.
/// </summary>
public class TareaBuscar : TareaHTN
{
    private readonly List<Transform> _searchPoints;
    private readonly float _delayBeforeSearch;
    private readonly float _delayAtPoint;

    /// <param name="searchPoints">Locations to visit during search.</param>
    /// <param name="delayBeforeSearch">Time to wait before starting the search.</param>
    /// <param name="delayAtPoint">Time to wait at each search point.</param>
    public TareaBuscar(List<Transform> searchPoints, float delayBeforeSearch = 2f, float delayAtPoint = 2f)
    {
        _searchPoints = searchPoints;
        _delayBeforeSearch = delayBeforeSearch;
        _delayAtPoint = delayAtPoint;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Only executable if there are points to search
        return _searchPoints != null && _searchPoints.Count > 0;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.Log($"[HTN] TareaBuscar: {policia.AgentId} waiting {_delayBeforeSearch}s before searching");
        yield return new WaitForSeconds(_delayBeforeSearch);

        var nav = policia.GetComponent<NavMeshAgent>();
        if (nav == null)
        {
            Debug.LogError($"[HTN] TareaBuscar: {policia.AgentId} missing NavMeshAgent");
            yield break;
        }

        foreach (Transform point in _searchPoints)
        {
            if (point == null) continue;
            Debug.Log($"[HTN] TareaBuscar: {policia.AgentId} moving to search point {point.name}");
            nav.SetDestination(point.position);

            while (nav.pathPending || nav.remainingDistance > 0.5f)
                yield return null;

            Debug.Log($"[HTN] TareaBuscar: {policia.AgentId} arrived at {point.name}, waiting {_delayAtPoint}s");
            yield return new WaitForSeconds(_delayAtPoint);
        }

        Debug.Log($"[HTN] TareaBuscar: {policia.AgentId} search complete, resuming patrol");
        if (policia.destinos == null || policia.destinos.Length == 0) yield return null;
        if (policia.ocupado) yield return null;  // Can't patrol if busy

        policia.isPatrolling = true;
        if (policia.destinos == null || policia.destinos.Length == 0) yield return null;
        policia._currentWaypointIndex = (policia._currentWaypointIndex + 1) % policia.destinos.Length;
        var next = policia.destinos[policia._currentWaypointIndex];
        if (next != null && policia._navAgent != null && policia._navAgent.isOnNavMesh)
        {
            policia._navAgent.SetDestination(next.position);
            Debug.Log($"Policia {policia.AgentId}: Moving to waypoint {policia._currentWaypointIndex}");
        }
        Debug.Log($"Policia {policia.AgentId}: Comenzando patrulla");

    }
}