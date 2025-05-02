using System.Collections;
using UnityEngine;
using UnityEngine.AI;
public class TareaPuerta : TareaHTN
{
    private Vector3 _target;
    public TareaPuerta(Vector3 target) { _target = target; }

    public override bool EsEjecutable(Policia policia) => true;

    public override IEnumerator Ejecutar(Policia policia)
    {
        var nav = policia.GetComponent<NavMeshAgent>();
        nav.SetDestination(_target);
        while (nav.pathPending || nav.remainingDistance > 0.5f) yield return null;
    }
}
