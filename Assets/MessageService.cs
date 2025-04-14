using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MessageService : MonoBehaviour
{
    private static MessageService _instance;
    public static MessageService Instance => _instance;
    
    private Dictionary<string, Agente> _agents = new Dictionary<string, Agente>();
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
        }
    }
    
    public void RegisterAgent(string agentId, Agente agent)
    {
        if (!_agents.ContainsKey(agentId))
        {
            _agents.Add(agentId, agent);
        }
    }
    
    public void SendMessage(FipaAclMessage message)
    {
        foreach (var receiver in message.Receivers)
        {
            if (_agents.TryGetValue(receiver, out Agente agent))
            {
                agent.ReceiveMessage(message);
            }
        }
    }
}