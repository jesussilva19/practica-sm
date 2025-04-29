using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton service for handling message passing between agents
/// </summary>
public class MessageService2 : MonoBehaviour
{
    private static MessageService2 _instance;
    public static MessageService2 Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MessageService");
                _instance = go.AddComponent<MessageService2>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Dictionary<string, ICommunicationAgent> _agents = new Dictionary<string, ICommunicationAgent>();

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

    /// <summary>
    /// Register an agent with the message service
    /// </summary>
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

    /// <summary>
    /// Unregister an agent from the message service
    /// </summary>
    public void UnregisterAgent(string agentId)
    {
        if (_agents.ContainsKey(agentId))
        {
            _agents.Remove(agentId);
            Debug.Log($"Agent {agentId} unregistered from MessageService");
        }
    }

    /// <summary>
    /// Get a list of all registered agent IDs
    /// </summary>
    public List<string> GetAllAgentIds()
    {
        return new List<string>(_agents.Keys);
    }

    /// <summary>
    /// Send a message to all receivers specified in the message
    /// </summary>
    public void SendMessage(FipaAclMessage message)
    {
        if (string.IsNullOrEmpty(message.Sender))
        {
            Debug.LogWarning("Attempted to send message with no sender");
            return;
        }

        if (message.Receivers == null || message.Receivers.Count == 0)
        {
            Debug.LogWarning($"Message from {message.Sender} has no receivers");
            return;
        }

        foreach (var receiver in message.Receivers)
        {
            if (_agents.TryGetValue(receiver, out ICommunicationAgent agent))
            {
                agent.ReceiveMessage(message);
                Debug.Log($"Message from {message.Sender} delivered to {receiver}: {message.Performative} - {message.Content}");
            }
            else
            {
                Debug.LogWarning($"Could not deliver message to {receiver}: agent not found");
            }
        }
    }

    /// <summary>
    /// Broadcast a message to all registered agents except the sender
    /// </summary>
    public void BroadcastMessage(FipaAclMessage message)
    {
        if (string.IsNullOrEmpty(message.Sender))
        {
            Debug.LogWarning("Attempted to broadcast message with no sender");
            return;
        }

        foreach (var agentEntry in _agents)
        {
            if (agentEntry.Key != message.Sender)
            {
                message.Receivers.Clear();
                message.Receivers.Add(agentEntry.Key);
                agentEntry.Value.ReceiveMessage(message);
                Debug.Log($"Broadcast message from {message.Sender} delivered to {agentEntry.Key}: {message.Performative} - {message.Content}");
            }
        }
    }
}

