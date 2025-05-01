using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for agents that need to communicate via FIPA ACL messages
/// </summary>
public class CommunicationAgent : MonoBehaviour, ICommunicationAgent
{
    [Tooltip("Unique identifier for this agent")]
    public string AgentId;

    protected Queue<FipaAclMessage> _messageQueue = new Queue<FipaAclMessage>();
    protected int _maxMessagesPerFrame = 3; // Limit messages processed per frame

    protected virtual void Awake()
    {
        // Ensure the agent has a unique ID
        if (string.IsNullOrEmpty(AgentId))
        {
            AgentId = gameObject.name + "_" + System.Guid.NewGuid().ToString().Substring(0, 8);
        }
    }

    protected virtual void Start()
    {
        // Register with the message service
        RegisterWithMessageService();
    }

    protected virtual void OnEnable()
    {
        RegisterWithMessageService();
    }

    protected virtual void OnDisable()
    {
        // Unregister from message service when disabled
        if (MessageService.Instance != null)
        {
            MessageService.Instance.UnregisterAgent(AgentId);
        }
    }

    protected virtual void OnDestroy()
    {
        // Unregister from message service when destroyed
        if (MessageService.Instance != null)
        {
            MessageService.Instance.UnregisterAgent(AgentId);
        }
    }

    protected virtual void Update()
    {
        // Process messages from the queue
        ProcessMessageQueue();
    }

    protected void RegisterWithMessageService()
    {
        if (MessageService.Instance != null)
        {
            MessageService.Instance.RegisterAgent(AgentId, this);
            Debug.Log($"Agent {AgentId} registered with MessageService");
        }
        else
        {
            Debug.LogError($"MessageService instance not found when registering agent {AgentId}");
        }
    }

    /// Receives a message and adds it to the queue for processing
    public void ReceiveMessage(FipaAclMessage message)
    {
        _messageQueue.Enqueue(message);
    }

    /// Process messages in the queue, limiting to max messages per frame
    protected virtual void ProcessMessageQueue()
    {
        int processedCount = 0;

        while (_messageQueue.Count > 0 && processedCount < _maxMessagesPerFrame)
        {
            ProcessMessage(_messageQueue.Dequeue());
            processedCount++;
        }
    }

    /// Process a single message based on its performative
    protected virtual void ProcessMessage(FipaAclMessage message)
    {
        // Default implementation routes messages based on performative
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


    /// Handle INFORM messages (to be overridden by derived classes)
    protected virtual void HandleInform(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received INFORM from {message.Sender}: {message.Content}");
    }

    /// Handle REQUEST messages (to be overridden by derived classes)
    protected virtual void HandleRequest(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received REQUEST from {message.Sender}: {message.Content}");
        // Default behavior is to not understand the request
        SendNotUnderstood(message.Sender, message.ConversationId);
    }

    /// Handle AGREE messages (to be overridden by derived classes)
    protected virtual void HandleAgree(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received AGREE from {message.Sender}: {message.Content}");
    }

    /// Handle REFUSE messages (to be overridden by derived classes)
    protected virtual void HandleRefuse(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received REFUSE from {message.Sender}: {message.Content}");
    }

    /// Handle NOT_UNDERSTOOD messages (to be overridden by derived classes)
    protected virtual void HandleNotUnderstood(FipaAclMessage message)
    {
        Debug.Log($"Agent {AgentId} received NOT_UNDERSTOOD from {message.Sender}: {message.Content}");
    }

    /// Send an INFORM message to a specific agent
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

    /// Send a REQUEST message to a specific agent
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

    /// Send an AGREE message to a specific agent
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

    /// Send a REFUSE message to a specific agent
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

    /// Send a NOT_UNDERSTOOD message to a specific agent
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

    /// Broadcast a message to all agents except self
    protected void BroadcastMessage(string content, string performative = FipaPerformatives.INFORM)
    {
        var message = new FipaAclMessage
        {
            Performative = performative,
            Sender = AgentId,
            Content = content,
            ConversationId = System.Guid.NewGuid().ToString()
        };

        // Add all other agents as receivers
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