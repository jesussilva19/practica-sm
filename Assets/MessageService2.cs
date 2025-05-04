using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Clase que gestiona el envío y recepción de mensajes entre agentes
public class MessageService : MonoBehaviour
{
    private static MessageService _instance;
    private Dictionary<string, ICommunicationAgent> _agents = new Dictionary<string, ICommunicationAgent>();

    public static MessageService Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MessageService");
                _instance = go.AddComponent<MessageService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // Regsitrar un agente en el servicio de mensajes
    public void RegisterAgent(string agentId, ICommunicationAgent agent)
    {
        if (!_agents.ContainsKey(agentId))
        {
            _agents.Add(agentId, agent);
            Debug.Log($"Agent {agentId} registered with MessageService");
        }
        else
        {
            Debug.LogWarning($"Agent with ID {agentId} is already registered");
        }
    }

    // Desregistrar un agente del servicio de mensajes
    public void UnregisterAgent(string agentId)
    {
        if (_agents.ContainsKey(agentId))
        {
            _agents.Remove(agentId);
            Debug.Log($"Agent {agentId} unregistered from MessageService");
        }
    }

    // Obtener la lista de IDs de todos los agentes registrados
    public List<string> GetAllAgentIds()
    {
        return new List<string>(_agents.Keys);
    }

    // Envia un mensaje a los agentes destinatarios
    public void SendMessage(FipaAclMessage message)
    {
        if (string.IsNullOrEmpty(message.Sender))
        {
            Debug.LogWarning("Se ha intentado enviar un mensaje sin destinatario");
            return;
        }

        if (message.Receivers == null || message.Receivers.Count == 0)
        {
            Debug.LogWarning($"Mensaje de {message.Sender} no tiene destinatarios");
            return;
        }

        foreach (var receiver in message.Receivers)
        {
            if (_agents.TryGetValue(receiver, out ICommunicationAgent agent))
            {
                agent.ReceiveMessage(message);
            }
            else
            {
                Debug.LogWarning($"No se pudo enviar el mensaje a {receiver}: agente no encontrado");
            }
        }
    }

    // Envia un mensaje a todos los agentes registrados (Broadcast), menos al remitente
    // Se utiliza para enviar mensajes a todos los agentes, excepto al remitente
    public void BroadcastMessage(FipaAclMessage message)
    {
        if (string.IsNullOrEmpty(message.Sender))
        {
            Debug.LogWarning("Se ha intentado enviar un mensaje sin remitente");
            return;
        }

        foreach (var agentEntry in _agents)
        {
            if (agentEntry.Key != message.Sender)
            {
                message.Receivers.Clear();
                message.Receivers.Add(agentEntry.Key);
                agentEntry.Value.ReceiveMessage(message);
                Debug.Log($"Mensaje Broadcast de {message.Sender} enviado a {agentEntry.Key}: {message.Performative} - {message.Content}");
            }
        }
    }
}

