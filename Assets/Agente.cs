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
    

    void Start()
    {
        trans = GetComponent<Transform>();
        agent = GetComponent<NavMeshAgent>();
        IrAlSiguienteDestino();
        MessageService.Instance.RegisterAgent(AgentId, this);

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
            // Procesar mensajes en cada frame
            while (_messageQueue.Count > 0)
            {
                ProcessMessage(_messageQueue.Dequeue());
            }
        }
        // Procesar mensajes en cada frame
        while (_messageQueue.Count > 0)
        {
            ProcessMessage(_messageQueue.Dequeue());
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
            ladronDetectado = true;
            ladronTransform = ladron;
            agent.SetDestination(ladron.position);
            Debug.Log("Agente: Ladrón detectado.");
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
        ladronDetectado = false;
        Debug.Log(" Reanudando patrulla.");
    }

    public void ReceiveMessage(FipaAclMessage message)
    {
        _messageQueue.Enqueue(message);
    }
    
    
    private void ProcessMessage(FipaAclMessage message)
    {
        switch (message.Performative)
        {
            case FipaPerformatives.INFORM:
                HandleInform(message);
                break;
            case FipaPerformatives.REQUEST:
                HandleRequest(message);
                break;
            // Maneja otros performatives según sea necesario
            default:
                SendNotUnderstood(message.Sender, message.ConversationId);
                break;
        }
    }
    
    private void HandleInform(FipaAclMessage message)
    {
        // Implementa la lógica para manejar un mensaje INFORM
        Debug.Log($"Agent {AgentId} received INFORM: {message.Content}");
        
        // Ejemplo: Si un policía informa sobre un sospechoso
        if (message.Content.Contains("suspect"))
        {
            // Actualizar conocimiento interno sobre sospechosos
        }
    }
    
    private void HandleRequest(FipaAclMessage message)
    {
        // Implementa la lógica para manejar un mensaje REQUEST
        Debug.Log($"Agent {AgentId} received REQUEST: {message.Content}");
        
        // Ejemplo: Si un policía pide respaldo
        if (message.Content.Contains("backup"))
        {
            // Decidir si proporcionar respaldo y enviar AGREE o REFUSE
            SendAgree(message.Sender, message.ConversationId);
            
            // Iniciar comportamiento de respaldo
            // MoveToPosition(extractPositionFromMessage(message.Content));
        }
    }
    
    public void SendInform(string receiver, string content, string conversationId = null)
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.INFORM,
            Sender = AgentId,
            Content = content,
            ConversationId = conversationId ?? System.Guid.NewGuid().ToString()
        };
        message.Receivers.Add(receiver);
        
        MessageService.Instance.SendMessage(message);
    }
    
    public void SendRequest(string receiver, string content, string conversationId = null)
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.REQUEST,
            Sender = AgentId,
            Content = content,
            ConversationId = conversationId ?? System.Guid.NewGuid().ToString()
        };
        message.Receivers.Add(receiver);
        
        MessageService.Instance.SendMessage(message);
    }
    
    public void SendAgree(string receiver, string conversationId)
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.AGREE,
            Sender = AgentId,
            Content = "I agree to your request",
            ConversationId = conversationId,
        };
        message.Receivers.Add(receiver);
        
        MessageService.Instance.SendMessage(message);
    }
    
    public void SendNotUnderstood(string receiver, string conversationId)
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.NOT_UNDERSTOOD,
            Sender = AgentId,
            Content = "I did not understand your message",
            ConversationId = conversationId,
        };
        message.Receivers.Add(receiver);
        
        MessageService.Instance.SendMessage(message);
    }

}