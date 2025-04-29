using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Comunicacion : MonoBehaviour
{
    public string AgentId;
    protected Queue<FipaAclMessage> _messageQueue = new Queue<FipaAclMessage>();

    protected virtual void Awake()
    {
        // Asegurarse de que el ID del agente esté configurado
        if (string.IsNullOrEmpty(AgentId))
        {
            AgentId = gameObject.name + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }

    protected virtual void Start()
    {
        // Registrar el agente con el servicio de mensajería
        //MessageService.Instance.RegisterAgent(AgentId, this);
        //Debug.Log($"Agente: {AgentId} registrado");
    }

    protected virtual void Update()
    {
        ProcesarMensajes();
    }

    public void ReceiveMessage(FipaAclMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    protected virtual void ProcesarMensajes()
    {
        int mensajesProcesados = 0;
        int maximoMensajesPorFrame = 3; // Limitar el número de mensajes procesados por frame

        while (_messageQueue.Count > 0 && mensajesProcesados < maximoMensajesPorFrame)
        {
            ProcessMessage(_messageQueue.Dequeue());
            mensajesProcesados++;
        }
    }

    protected virtual void ProcessMessage(FipaAclMessage message)
    {
        // Este método debe ser sobrescrito por las clases derivadas para manejar mensajes específicos
        switch (message.Performative)
        {
            case FipaPerformatives.INFORM:
                HandleInform(message);
                break;
            case FipaPerformatives.REQUEST:
                HandleRequest(message);
                break;
            default:
                Debug.Log(AgentId + ": Performativo no manejado: " + message.Performative);
                break;
        }
    }

    protected virtual void HandleInform(FipaAclMessage message)
    {
        // Método base para manejar mensajes INFORM
        Debug.Log($"Agent {AgentId} received INFORM: {message.Content}");
    }

    protected virtual void HandleRequest(FipaAclMessage message)
    {
        // Método base para manejar mensajes REQUEST
        Debug.Log($"Agent {AgentId} received REQUEST: {message.Content}");
    }

    // Métodos para enviar diferentes tipos de mensajes FIPA-ACL
    public void SendInform(string receiver, string content, string conversationId = null)
    {
        var message = new FipaAclMessage
        {
            Performative = "INFORM",
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
            Performative = "REQUEST",
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
            Performative = "AGREE",
            Sender = AgentId,
            Content = "I agree to your request",
            ConversationId = conversationId,
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    public void SendRefuse(string receiver, string conversationId, string reason = "Request refused")
    {
        var message = new FipaAclMessage
        {
            Performative = "REFUSE",
            Sender = AgentId,
            Content = reason,
            ConversationId = conversationId,
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    public void SendNotUnderstood(string receiver, string conversationId)
    {
        var message = new FipaAclMessage
        {
            Performative = "NOT_UNDERSTOOD",
            Sender = AgentId,
            Content = "I did not understand your message",
            ConversationId = conversationId,
        };
        message.Receivers.Add(receiver);

        MessageService.Instance.SendMessage(message);
    }

    // Método para enviar un mensaje a todos los otros agentes
    protected void EnviarMensajeATodos(string content, string performative = "INFORM")
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = performative;
        mensaje.Sender = AgentId;
        mensaje.Content = content;
        mensaje.ConversationId = System.Guid.NewGuid().ToString();

        // Añadir todos los otros agentes como receptores
        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId) // No enviarse el mensaje a sí mismo
            {
                mensaje.Receivers.Add(agente);
            }
        }

        MessageService.Instance.SendMessage(mensaje);
    }

    // Métodos específicos para la coordinación entre agentes de seguridad
    protected void EnviarMensajeLadronDetectado(Vector3 posicion)
    {
        string contenido = "LADRON_DETECTADO:" + posicion.x + "," + posicion.y + "," + posicion.z;
        EnviarMensajeATodos(contenido);
    }

    protected void EnviarMensajeLadronPerdido()
    {
        EnviarMensajeATodos("LADRON_PERDIDO");
    }

    protected void EnviarMensajeLadronDetenido()
    {
        EnviarMensajeATodos("LADRON_DETENIDO");
    }
}