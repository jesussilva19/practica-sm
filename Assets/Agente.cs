using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Agente : MonoBehaviour
{
    Transform trans;
    NavMeshAgent agent;
    public Transform[] destinos;
    private int indiceDestino = 0;

    public bool patrullando = true; // Variable de estado

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
        agent.ResetPath(); // Detiene el movimiento
    }

    public void ReanudarPatrulla()
    {
        patrullando = true;
        IrAlSiguienteDestino();
    }

    public void IrAlSiguienteDestino()
    {
        if (destinos.Length == 0)
        {
            Debug.LogWarning("No hay puntos de patrulla asignados.");
            return;
        }

        agent.destination = destinos[indiceDestino].position;
    }
}
