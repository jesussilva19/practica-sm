using System.Collections.Generic;
using UnityEngine;


public class Persecucion : MonoBehaviour
{
    public Transform ladron;
    public Transform[] puntosBusqueda;

    private Policia agente;

    private void Start()
    {
        agente = GetComponentInParent<Policia>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            agente.LadronVisto(ladron);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform == ladron)
        {
            if (TieneLineaDeVision())
                agente.LadronVisto(ladron);
            else
                agente.LadronPerdido(new List<Transform>(puntosBusqueda));
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
