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
        Debug.Log($"Agente: {AgentId} registrado");
        IniciarPatrulla();

    }

    private void IniciarPatrulla()
    {
        if (destinos.Length == 0)
        {
            Debug.LogWarning($"Agente {AgentId}: No hay puntos de patrulla configurados");
            return;
        }
        patrullando = true;
        IrAlSiguienteDestino();
    }

    void Update()
    {
        ActualizarPatrulla();
        ProcesarMensajes();
    }

    private void ActualizarPatrulla()
    {
        if (patrullando && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            indiceDestino = (indiceDestino + 1) % destinos.Length;
            IrAlSiguienteDestino();
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
        if (!agent.isStopped)
        {
            PausarPatrulla();
            ladronDetectado = true;
            ladronTransform = ladron;
            agent.SetDestination(ladron.position);
            Debug.Log("Agente: Ladrón detectado.");
            Debug.Log("Agente: Persiguiendo al ladrón.");

            // Enviar mensaje INFORM a todos los otros agentes
            EnviarMensajeLadronDetectado(ladron.position);
        }
    }

    public void PerderLadron(List<Transform> puntosBusqueda, float espera)
    {
        Debug.Log("Agente: Perdiendo al ladrón.");
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

    private void ProcesarMensajes()
    {
        int mensajesProcesados = 0;
        int maximoMensajesPorFrame = 3; // Limitar el número de mensajes procesados por frame

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
                // Extraer la posición del ladrón del mensaje
                if (message.Content.Contains(":"))
                {
                    string[] coordenadas = message.Content.Split(':')[1].Split(',');
                    float x = float.Parse(coordenadas[0]);
                    float y = float.Parse(coordenadas[1]);
                    float z = float.Parse(coordenadas[2]);
                    Vector3 posicionLadron = new Vector3(x, y, z);

                    // Decidir si acudir en ayuda basado en la distancia, estado actual, etc.
                    if (!ladronDetectado && Vector3.Distance(transform.position, posicionLadron) < 50f)
                    {
                        Debug.Log(AgentId + ": Recibido mensaje de ladrón detectado. Acudiendo.");
                        // Ir a la posición del ladrón
                        PausarPatrulla();
                        agent.SetDestination(posicionLadron);
                    }
                }
                break;

            case "LADRON_PERDIDO":
                // Puedes implementar alguna lógica para responder a este mensaje
                Debug.Log(AgentId + ": Recibido mensaje de ladrón perdido.");
                break;

            case "LADRON_DETENIDO":
                // Si el ladrón ha sido detenido, volver a la rutina normal
                if (ladronDetectado)
                {
                    Debug.Log(AgentId + ": Recibido mensaje de ladrón detenido. Volviendo a patrullar.");
                    ladronDetectado = false;
                    ReanudarPatrulla();
                }
                break;

            default:
                Debug.Log(AgentId + ": Mensaje no reconocido recibido: " + message.Content);
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
   


    // Metodos para enviar mensajes FIPA-ACL
    private void EnviarMensajeLadronDetectado(Vector3 posicion)
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = FipaPerformatives.INFORM;
        mensaje.Sender = AgentId;
        mensaje.Content = "LADRON_DETECTADO:" + posicion.x + "," + posicion.y + "," + posicion.z;
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

    private void EnviarMensajeLadronPerdido()
    {
        FipaAclMessage mensaje = new FipaAclMessage();
        mensaje.Performative = FipaPerformatives.INFORM;
        mensaje.Sender = AgentId;
        mensaje.Content = "LADRON_PERDIDO";
        mensaje.ConversationId = System.Guid.NewGuid().ToString();

        // Añadir todos los otros agentes como receptores
        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId)
            {
                mensaje.Receivers.Add(agente);
            }
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

        // Añadir todos los otros agentes como receptores
        foreach (var agente in MessageService.Instance.GetAllAgentIds())
        {
            if (agente != AgentId)
            {
                mensaje.Receivers.Add(agente);
            }
        }

        MessageService.Instance.SendMessage(mensaje);
    }
}