using System.Collections.Generic;
using UnityEngine;


public class Persecucion : MonoBehaviour
{
    public Transform ladron;
    public Transform[] puntosBusqueda;

    private Agente agente;

    private void Start()
    {
        agente = GetComponentInParent<Agente>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            agente.VerLadron(ladron);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform == ladron)
        {
            if (TieneLineaDeVision())
                agente.VerLadron(ladron);
            else
                agente.PerderLadron(new List<Transform>(puntosBusqueda), 3f);
        }
    }

    private bool TieneLineaDeVision()
    {
        Vector3 origen = agente.transform.position + Vector3.up * 1f;
        Vector3 direccion = (ladron.position - origen).normalized;
        float distancia = Vector3.Distance(agente.transform.position, ladron.position);
        float angulo = Vector3.Angle(agente.transform.forward, direccion);

        if (angulo > 60f) return false;

        int mascara = LayerMask.GetMask("Obstaculos", "Ladron");
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distancia, mascara))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstaculos"))
                return false;

            return hit.transform == ladron;
        }

        return true;
    }
}
