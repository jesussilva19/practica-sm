
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

    public override bool EsEjecutable(Policia policia)
    {
        // Siempre se puede intentar acudir si no est� ocupado
        return true;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        policia.PausarPatrulla();
        policia.GetComponent<NavMeshAgent>().SetDestination(posicionRefuerzo);

        while (policia.GetComponent<NavMeshAgent>().pathPending || policia.GetComponent<NavMeshAgent>().remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log($"{policia.AgentId}: Lleg� a la zona de refuerzo.");
        yield return new WaitForSeconds(3f); // Simular espera

        policia.IniciarPatrulla();
    }
}
