using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Agente : MonoBehaviour
{
    private Transform trans;
    private NavMeshAgent agent;

    public Transform[] destinos; // Puntos de patrulla
    private int indiceDestino = 0;

    public bool patrullando = true;
    private bool enBusqueda = false;

    void Start()
    {
        trans = GetComponent<Transform>();
        agent = GetComponent<NavMeshAgent>();
        IrAlSiguienteDestino();
    }

    void Update()
    {
        if (patrullando)
        {
            if (!agent.pathPending && agent.remainingDistance <= 0.2f)
            {
                indiceDestino = (indiceDestino + 1) % destinos.Length;
                IrAlSiguienteDestino();
            }
        }
    }

    public void PausarPatrulla()
    {
        patrullando = false;
        agent.ResetPath();
    }

    public void ReanudarPatrulla()
    {
        patrullando = true;
        IrAlSiguienteDestino();
    }

    public void IrAlSiguienteDestino()
    {
        if (destinos.Length == 0) return;
        agent.destination = destinos[indiceDestino].position;
    }


    public void VerLadron(Transform ladron)
    {
        if (!agent.isStopped)
        {
            PausarPatrulla();
            agent.SetDestination(ladron.position);
            Debug.Log("Agente: Persiguiendo al ladrón.");
        }
    }

    public void PerderLadron(List<Transform> puntosBusqueda, float espera)
    {
        Debug.Log("Agente: Perdiendo al ladrón.");
        if (!enBusqueda)
        {
            StartCoroutine(BuscarYLuegoPatrullar(puntosBusqueda, espera));
        }
    }

    public void DetenerLadron()
    {
        agent.isStopped = true;
        Debug.Log(" Agente: Ladrón detenido.");
    }

    private IEnumerator BuscarYLuegoPatrullar(List<Transform> puntosBusqueda, float espera)
    {
        enBusqueda = true;
        agent.isStopped = true;

        Debug.Log(" Esperando antes de buscar...");
        yield return new WaitForSeconds(espera);

        agent.isStopped = false;

        foreach (Transform punto in puntosBusqueda)
        {
            if (punto != null)
            {
                agent.SetDestination(punto.position);
                Debug.Log("🔎 Buscando en: " + punto.name);

                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(2f);
            }
        }

        ReanudarPatrulla();
        enBusqueda = false;
        Debug.Log(" Reanudando patrulla.");
    }
}
