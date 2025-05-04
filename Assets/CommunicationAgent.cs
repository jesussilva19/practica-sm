using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Interfaz para los agentes de comunicación
public interface ICommunicationAgent
{
    // Recibe un mensaje y lo añade a la cola
    // Este método es llamado por el servicio de mensajes
    void ReceiveMessage(FipaAclMessage message);
}

// Clase base para los agentes de comunicación.
public class CommunicationAgent : MonoBehaviour, ICommunicationAgent
{
    public string AgentId;

    protected Queue<FipaAclMessage> _messageQueue = new Queue<FipaAclMessage>();
    protected int _maxMessagesPerFrame = 3; // Límite de mensajes a procesar por frame
    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(AgentId))
        {
            AgentId = gameObject.name + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }

    protected virtual void Start()
    {
        RegisterWithMessageService(); //Regsistrar el agente en el servicio de mensajes
    }


    protected virtual void Update()
    {
        ProcessMessageQueue(); //Procesar la cola de mensajes
    }

    protected void RegisterWithMessageService()
    {
        if (MessageService.Instance != null)
        {
            MessageService.Instance.RegisterAgent(AgentId, this);
        }
        else
        {
            Debug.LogError($"MessageService instance not found when registering agent {AgentId}");
        }
    }

    /// Recibe un mensaje y lo añade a la cola
    public void ReceiveMessage(FipaAclMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    /// Procesa los mensajes de la cola, limitando el número de mensajes procesados por frame al establecido
    protected virtual void ProcessMessageQueue()
    {
        int processedCount = 0;

        while (_messageQueue.Count > 0 && processedCount < _maxMessagesPerFrame)
        {
            ProcessMessage(_messageQueue.Dequeue());
            processedCount++;
        }
    }

    /// Procesa el mensaje en función de su performativo
    protected virtual void ProcessMessage(FipaAclMessage message)
    {
        // Implementacion base para el procesamiento de los mensajes recibidos
        switch (message.Performative)
        {
            case FipaPerformatives.INFORM:
                HandleInform(message);
                break;

            case FipaPerformatives.REQUEST:
                HandleRequest(message);
                break;

            case FipaPerformatives.AGREE:
                HandleAgree(message);
                break;

            case FipaPerformatives.REFUSE:
                HandleRefuse(message);
                break;

            case FipaPerformatives.NOT_UNDERSTOOD:
                HandleNotUnderstood(message);
                break;

            default:
                Debug.Log($"Agent {AgentId}: Unhandled performative {message.Performative} from {message.Sender}");
                SendNotUnderstood(message.Sender, message.ConversationId);
                break;
        }
    }


    // Gestiona los mensajes INFORM (debe ser sobreescrito por clases derivadas)
    protected virtual void HandleInform(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received INFORM from {message.Sender}: {message.Content}");
    }

    // Gestiona los mensajes REQUEST (debe ser sobreescrito por clases derivadas)
    protected virtual void HandleRequest(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received REQUEST from {message.Sender}: {message.Content}");
        // Default behavior is to not understand the request
        SendNotUnderstood(message.Sender, message.ConversationId);
    }

    // Gestiona los mensajes AGREE (debe ser sobreescrito por clases derivadas)
    protected virtual void HandleAgree(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received AGREE from {message.Sender}: {message.Content}");
    }

    // Gestiona los mensajes REFUSE (debe ser sobreescrito por clases derivadas)
    protected virtual void HandleRefuse(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received REFUSE from {message.Sender}: {message.Content}");
    }

    // Gestiona los mensajes NOT_UNDERSTOOD (debe ser sobreescrito por clases derivadas)
    protected virtual void HandleNotUnderstood(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received NOT_UNDERSTOOD from {message.Sender}: {message.Content}");
    }

    // Envia un mensaje INFORM a un agente específico
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

    // Envia un mensaje REQUEST a un agente específico
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

    // Envia un mensaje AGREE a un agente específico
    public void SendAgree(string receiver, string conversationId, string content = "I agree to your request")
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.AGREE,
            Sender = AgentId,
            Content = content,
            ConversationId = conversationId
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    // Envia un mensaje REFUSE a un agente específico
    public void SendRefuse(string receiver, string conversationId, string reason = "Request refused")
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.REFUSE,
            Sender = AgentId,
            Content = reason,
            ConversationId = conversationId
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    // Envia un mensaje NOT_UNDERSTOOD a un agente específico
    public void SendNotUnderstood(string receiver, string conversationId)
    {
        var message = new FipaAclMessage
        {
            Performative = FipaPerformatives.NOT_UNDERSTOOD,
            Sender = AgentId,
            Content = "I did not understand your message",
            ConversationId = conversationId
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    // Envia un mensaje con performativo personalizado a un agente específico
    public void SendPerformative(string receiver, string content, string performative, string conversationId = null)
    {
        var msg = new FipaAclMessage
        {
            Sender = AgentId,
            Receivers = new List<string> { receiver },
            Content = content,
            Performative = performative,
            ConversationId = conversationId ?? System.Guid.NewGuid().ToString()
        };
        MessageService.Instance.SendMessage(msg);
    }


    // Envia un mensaje a todos los agentes registrados (Broadcast)
    protected void BroadcastMessage(string content, string performative = FipaPerformatives.INFORM)
    {
        var message = new FipaAclMessage
        {
            Performative = performative,
            Sender = AgentId,
            Content = content,
            ConversationId = System.Guid.NewGuid().ToString()
        };

        // Añadir todos los agentes registrados como receptores
        foreach (var agentId in MessageService.Instance.GetAllAgentIds())
        {
            if (agentId != AgentId) // Don't send to self
            {
                message.Receivers.Add(agentId);
            }
        }

        if (message.Receivers.Count > 0)
        {
            MessageService.Instance.SendMessage(message);
        }
    }

}