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
        policia.GetComponent<NavMeshAgent>().SetDestination(policia.thiefTransform.position);
        yield return null;
    }
}