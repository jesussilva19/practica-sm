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

        if (other.CompareTag("Player") && TieneLineaDeVision(other.transform))
        {
            agente.thiefTransform = other.transform;

            agente.ladronVisto = true;
            agente.ladronViendo = true;
            agente.LadronVisto(other.transform);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.LogError($"AAAAAAAA {agente.AgentId}");
            agente.thiefTransform = other.transform;

            if (!TieneLineaDeVision(other.transform))
            {
                agente.ladronViendo = false;
                agente.ladronPerdido = true;
                agente.ladronVisto = false;
            }
            else
            {
                agente.ladronPerdido = false;
                agente.ladronViendo = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            agente.ladronViendo = false;
            agente.ladronPerdido = true;
            agente.ladronVisto = false;
            List<Transform> puntosBusqueda = new List<Transform>(other.transform.GetComponentsInChildren<Transform>());
            puntosBusqueda.AddRange(agente.destinos);
            agente.LadronPerdido(puntosBusqueda);


        }
    }


    public bool TieneLineaDeVision(Transform ladron)
    {
        Vector3 origen = agente.transform.position + Vector3.up * 1.0f;
        Vector3 direccion = (ladron.position - origen).normalized;
        float distancia = Vector3.Distance(agente.transform.position, ladron.position);
        float angulo = Vector3.Angle(agente.transform.forward, direccion);

        if (angulo > 180f) return false;
        
        float distanciaMaxima = distancia * 1.2f; 

        int mascara = LayerMask.GetMask("Obstaculos", "Player");
        if (Physics.Raycast(origen, direccion, out RaycastHit hit, distanciaMaxima, mascara))
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstaculos"))
                return false;

            return hit.transform == ladron;
        }

        return true;
    }
}
