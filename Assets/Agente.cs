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
    public bool ladronDetectado = false;
    public Transform ladronTransform;

    public string AgentId;
    private Queue<FipaAclMessage> _messageQueue = new Queue<FipaAclMessage>();

    private PlaneadorHTN planeador;

    void Start()
    {
        trans = GetComponent<Transform>();
        agent = GetComponent<NavMeshAgent>();
        IrAlSiguienteDestino();
        MessageService.Instance.RegisterAgent(AgentId, this);
        Debug.Log($"Agente: {AgentId} registrado");

        // 🧠 Iniciar el planificador HTN
        planeador = new PlaneadorHTN();
        StartCoroutine(EjecutarPlanPeriodicamente());
    }

    private IEnumerator EjecutarPlanPeriodicamente()
    {
        while (true)
        {
            planeador.Planificar(this);
            yield return StartCoroutine(planeador.EjecutarPlan(this, this));
            yield return new WaitForSeconds(1f); // Esperar un poco antes de volver a planificar
        }
    }

    void Update()
    {
        ProcesarMensajes();
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
        if (destinos == null || destinos.Length == 0)
        {
            Debug.LogWarning($"Agente {AgentId}: No hay destinos configurados");
            return;
        }

        if (destinos[indiceDestino] == null)
        {
            Debug.LogWarning($"Agente {AgentId}: El destino en índice {indiceDestino} es nulo");
            return;
        }

        agent.SetDestination(destinos[indiceDestino].position);
    }

    public void VerLadron(Transform ladron)
    {
        PausarPatrulla();
        ladronDetectado = true;
        ladronTransform = ladron;
        agent.SetDestination(ladron.position);
        Debug.Log("Agente: Ladrón detectado.");
        EnviarMensajeLadronDetectado(ladron.position);
    }

    public void PerderLadron(List<Transform> puntosBusqueda, float espera)
    {
        if (!enBusqueda)
        {
            EnviarMensajeLadronPerdido();
            StartCoroutine(BuscarYLuegoPatrullar(puntosBusqueda, espera));
        }
    }

    public void DetenerLadron()
    {
        agent.isStopped = true;
        EnviarMensajeLadronDetenido();
        Debug.Log("Agente: Ladrón detenido.");
    }

    private IEnumerator BuscarYLuegoPatrullar(List<Transform> puntosBusqueda, float espera)
    {
        enBusqueda = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(espera);

        agent.isStopped = false;

        foreach (Transform punto in puntosBusqueda)
        {
            if (punto != null)
            {
                agent.SetDestination(punto.position);
                while (agent.pathPending || agent.remainingDistance > 0.5f)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(2f);
            }
        }

        ReanudarPatrulla();
        enBusqueda = false;
        ladronDetectado = false;
    }

    public void ReceiveMessage(FipaAclMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    private void ProcesarMensajes()
    {
        int mensajesProcesados = 0;
        int maximoMensajesPorFrame = 3;

        while (_messageQueue.Count > 0 && mensajesProcesados < maximoMensajesPorFrame)
        {
            ProcessMessage(_messageQueue.Dequeue());
            mensajesProcesados++;
        }
    }

    private void ProcessMessage(FipaAclMessage message)
    {
        switch (message.Content.Split(':')[0])
        {
            case "LADRON_DETECTADO":
                if (message.Content.Contains(":"))
                {
                    string[] coordenadas = message.Content.Split(':')[1].Split(',');
                    float x = float.Parse(coordenadas[0]);
                    float y = float.Parse(coordenadas[1]);
                    float z = float.Parse(coordenadas[2]);
                    Vector3 posicionLadron = new Vector3(x, y, z);

                    if (!ladronDetectado && Vector3.Distance(transform.position, posicionLadron) < 50f)
                    {
                        PausarPatrulla();
                        agent.SetDestination(posicionLadron);
                    }
                }
                break;

            case "LADRON_PERDIDO":
                Debug.Log(AgentId + ": Recibido mensaje de ladrón perdido.");
                break;

            case "LADRON_DETENIDO":
                if (ladronDetectado)
                {
                    ladronDetectado = false;
                    ReanudarPatrulla();
                }
                break;
        }
    }

    private void EnviarMensajeLadronDetectado(Vector3 posicion)
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = FipaPerformatives.INFORM;
        mensaje.Sender = AgentId;
        mensaje.Content = "LADRON_DETECTADO:" + posicion.x + "," + posicion.y + "," + posicion.z;
        mensaje.ConversationId = System.Guid.NewGuid().ToString();

        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId)
                mensaje.Receivers.Add(agente);
        }

        MessageService.Instance.SendMessage(mensaje);
    }

    private void EnviarMensajeLadronPerdido()
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = FipaPerformatives.INFORM;
        mensaje.Sender = AgentId;
        mensaje.Content = "LADRON_PERDIDO";
        mensaje.ConversationId = System.Guid.NewGuid().ToString();

        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId)
                mensaje.Receivers.Add(agente);
        }

        MessageService.Instance.SendMessage(mensaje);
    }

    private void EnviarMensajeLadronDetenido()
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = FipaPerformatives.INFORM;
        mensaje.Sender = AgentId;
        mensaje.Content = "LADRON_DETENIDO";
        mensaje.ConversationId = System.Guid.NewGuid().ToString();

        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId)
                mensaje.Receivers.Add(agente);
        }

        MessageService.Instance.SendMessage(mensaje);
    }
}