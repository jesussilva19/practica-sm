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
    // Start is called before the first frame update
    void Start()
    {
        trans = GetComponent<Transform>();
        agent = GetComponent<NavMeshAgent>();
        {
            agent.destination = destinos[indiceDestino].position;
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (!agent.pathPending && agent.remainingDistance <= 0.2f)
        {
            indiceDestino = (indiceDestino + 1) % destinos.Length;
            agent.destination = destinos[indiceDestino].position;
        }
    }
}


