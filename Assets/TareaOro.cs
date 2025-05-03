using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// HTN task for moving a police agent to the gold point.
/// Executed by the winner of the gold auction.
/// </summary>
public class TareaOro : TareaHTN
{
    private Vector3 _target;

    /// <param name="target">Position of the gold point.</param>
    public TareaOro(Vector3 target)
    {
        _target = target;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Task is always executable when assigned
        return  true;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.Log($"[HTN] TareaOro: {policia.AgentId} moving to gold at {_target}");
        // Ensure NavMeshAgent is available
        var nav = policia.GetComponent<NavMeshAgent>();
        if (nav == null)
        {
            Debug.LogError($"[HTN] TareaOro: {policia.AgentId} missing NavMeshAgent component");
            yield break;
        }

        nav.SetDestination(_target);
        // Wait until agent reaches the gold location
        while (nav.pathPending || nav.remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log($"[HTN] TareaOro: {policia.AgentId} reached gold");
        yield return new WaitForSeconds(10f);
        yield break;
    }
}
