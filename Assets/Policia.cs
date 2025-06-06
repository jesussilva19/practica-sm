﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Policia : CommunicationAgent
{
    public NavMeshAgent _navAgent;

    public Transform[] destinos;
    public int _currentWaypointIndex = 0;

    public bool isPatrolling = true; // Estado que indica si el agente está patrullando
    public bool isSearching = false; // Estado que indica si el agente está buscando
    public bool ocupado = false;  // Estado que indica si el agente está ocupado con una tarea

    // Estados de detección del ladrón
    public bool ladronVisto = false;    // Ha sido visto alguna vez
    public bool ladronViendo = false;   // Lo estamos viendo ahora mismo
    public bool ladronPerdido = false;  // Lo vimos pero lo perdimos
    public Transform thiefTransform;    // Referencia al transform del ladrón

    public float searchDelayTime = 2f; // Tiempo de espera para volver a buscar tras perder al ladrón
    public float searchPointWaitTime = 2f; // Tiempo en cada punto de búsqueda

    // Puntos de interés
    public Transform goldPoint;
    public Transform doorPoint;

    // Sistema de subastas FIPA
    public float proposalTimeout = 1f;
    public Dictionary<string, AuctionState> ActiveAuctions = new Dictionary<string, AuctionState>();
    public HashSet<string> auctionsParticipating = new HashSet<string>();  // Track auctions we're participating in

    // Planificador HTN
    private PlaneadorHTN planeador;
    private Coroutine _htnRoutine; 

    public class AuctionState
    {
        public string AuctionId;
        public Vector3 Target;
        public float StartTime;
        public Dictionary<string, float> Proposals = new Dictionary<string, float>();
    }

    protected override void Awake()
    {
        base.Awake();
        _navAgent = GetComponent<NavMeshAgent>();
        if (_navAgent == null)
            Debug.LogError($"Policia {AgentId}: NavMeshAgent component not found");
    }

    protected override void Start()
    {
        base.Start();
        if (destinos != null && destinos.Length > 0)
        {
            planeador = new PlaneadorHTN();
            _htnRoutine = StartCoroutine(EjecutarPlanPeriodicamente());
        }
        else
        {
            Debug.LogWarning($"Policia {AgentId}: No hay puntos de patrulla asignados");
        }
    }

    protected override void Update()
    {
        base.Update();
    }

    // Método para procesar mensajes recibidos
    protected override void ProcessMessage(FipaAclMessage message)
    {
        switch (message.Performative)
        {
            case FipaPerformatives.CFP:
                HandleCfp(message);
                break;
            case FipaPerformatives.PROPOSE:
                HandlePropose(message);
                break;
            case FipaPerformatives.ACCEPT_PROPOSAL:
                HandleAcceptProposal(message);
                break;
            case FipaPerformatives.REJECT_PROPOSAL:
                HandleRejectProposal(message);
                break;
            case FipaPerformatives.INFORM:
                // Añadido para procesar mensajes INFORM que podrían contener información del ladrón
                if (message.Content.StartsWith("THIEF_SPOTTED:"))
                {
                    HandleThiefSpotted(message);
                }
                else if (message.Content == "THIEF_LOST")
                {
                    // Otro policía perdió al ladrón
                    Debug.Log($"Policia {AgentId}: Recibido aviso de ladrón perdido");
                }
                else if (message.Content == "THIEF_CAUGHT")
                {
                    // Ladrón capturado
                    Debug.Log($"Policia {AgentId}: Recibido aviso de ladrón capturado");
                    ladronViendo = false;
                    ladronPerdido = false;
                }
                else
                {
                    base.ProcessMessage(message);
                }
                break;
            default:
                base.ProcessMessage(message);
                break;
        }
    }

    // Método para manejar el mensaje de ladrón avistado
    private void HandleThiefSpotted(FipaAclMessage message)
    {
        // Solo procesamos si no estamos viendo al ladrón ahora mismo
        if (!ladronViendo && !ocupado)
        {
            var parts = message.Content.Split(':')[1].Split(',');
            if (parts.Length >= 3)
            {
                Vector3 thiefPos = new Vector3(
                    float.Parse(parts[0]),
                    float.Parse(parts[1]),
                    float.Parse(parts[2])
                );

                Debug.Log($"Policia {AgentId}: Recibido aviso de ladrón en {thiefPos}");

                // Aquí podrías dirigirte a la posición reportada
                // O iniciar alguna acción coordinada
            }
        }
    }

    // Método para ejecutar el plan HTN periódicamente
    private IEnumerator EjecutarPlanPeriodicamente()
    {
        while (true)
        {
            // Solo se planifica si no se esta ocupado
            if (!ocupado)
            {
                planeador.Planificar(this);
                yield return StartCoroutine(planeador.EjecutarPlan(this, this));
            }
            StartCoroutine(planeador.EjecutarPlan(this, this));
            yield return new WaitForSeconds(1f);
        }
    }


    // Método para actuar cuando se ve al ladrón
    public void LadronVisto(Transform thief)
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            // Actualizar estados de detección
            ladronViendo = true;
            ladronVisto = true;
            ladronPerdido = false;
            thiefTransform = thief;

            // Si estamos ocupados, interrumpimos la tarea actual
            if (ocupado)
            {
                Debug.Log($"Policia {AgentId}: Interrumpiendo tarea actual para perseguir al ladrón");
            }

            // Broadcast a otros policías
            Debug.Log($"Policia {AgentId}: Ladrón detectado en {thief.position}! iniciando subastas...");
            BroadcastThiefSighting(thief.position);

            auctionsParticipating.Clear();
            planeador.tareas.Clear();  
            planeador.tareas.Enqueue(new TareaPerseguir());

            // Iniciar subastas 
            StartAuction("AUCTION_INTERCEPT");
            StartAuction("AUCTION_GOLD");
            StartAuction("AUCTION_DOOR");

            _htnRoutine = StartCoroutine(planeador.EjecutarPlan(this, this));

            
        }
    }

    // Método para actuar cuando se pierde al ladrón
    public void LadronPerdido(List<Transform> searchPoints)
    {
        Debug.Log($"Policia {AgentId}: Ladrón perdido");
        ladronViendo = false;
        ladronVisto = false;
        ladronPerdido = true;
        ocupado = false;
        isPatrolling = true; 
        isSearching = false;

        destinos = searchPoints.ToArray();

        // Notificar a otros policías
        if (!isSearching)
            BroadcastThiefLost();

        planeador.tareas.Clear(); 
        planeador.tareas.Enqueue(new TareaPatrullar());    
        _htnRoutine = StartCoroutine(planeador.EjecutarPlan(this, this));
    }

    // Método para cuando se detiene al ladrón
    public void DetenerLadron()
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            BroadcastThiefCaught();
            Debug.Log($"Policia {AgentId}: Ladron detenido!");

            // Limpiar estados
            ladronViendo = false;
            ladronPerdido = false;
        }
    }



    // Método para inciar la subasta
    public void StartAuction(string auctionId)
    {
        // Asignar el destino según el tipo de subasta
        Vector3 target;
        if (auctionId == "AUCTION_INTERCEPT")
        {
            target = thiefTransform.position;
        }
        else if (auctionId == "AUCTION_GOLD")
        {
            target = goldPoint.position;
        }
        else if (auctionId == "AUCTION_DOOR")
        {
            target = doorPoint.position;
        }
        else
        {
            Debug.LogError($"[{AgentId}] Tipo de subasta desconocido: {auctionId}");
            return;
        }
        var state = new AuctionState { AuctionId = auctionId, Target = target, StartTime = Time.time };
        ActiveAuctions[auctionId] = state;

        auctionsParticipating.Add(auctionId);

        BroadcastMessage($"CFP:{auctionId}:{target.x},{target.y},{target.z}", FipaPerformatives.CFP);
        Debug.Log($"[{AgentId}] Broadcast CFP for {auctionId} at {target}");

        StartCoroutine(ResolveAuction(auctionId));
    }

    // Método para resolver la subasta
    private IEnumerator ResolveAuction(string auctionId)
    {
        var state = ActiveAuctions[auctionId];
        yield return new WaitForSeconds(proposalTimeout);

        // Comprobar si hay propuestas
        if (state.Proposals.Count == 0)
        {
            Debug.LogWarning($"[{AgentId}] Sin propuestas para {auctionId}");
            ActiveAuctions.Remove(auctionId);
            auctionsParticipating.Remove(auctionId);
            yield break;
        }

        Debug.Log($"[{AgentId}] Recividas {state.Proposals.Count} propuestas para {auctionId}");
        foreach (var p in state.Proposals)
            Debug.Log($"Propuesta de {p.Key}: {p.Value:F2}");

        // Filtrar las propuestas, eliminando agentes ocupados
        var availableProposals = state.Proposals
            .Where(p => !p.Key.Contains("_busy"))
            .ToDictionary(p => p.Key, p => p.Value);

        // Si no quedan propuestas disponibles, eliminar la subasta
        if (availableProposals.Count == 0)
        {
            Debug.LogWarning($"[{AgentId}] No quedan agentes disponibles para {auctionId}, todos están ocupados");
            ActiveAuctions.Remove(auctionId);
            auctionsParticipating.Remove(auctionId);
            yield break;
        }

        // Elegir el ganador de la subasta
        var winner = availableProposals.Aggregate((l, r) => l.Value < r.Value ? l : r);
        Debug.Log($"[{AgentId}] Ganador de {auctionId} es {winner.Key} con {winner.Value:F2}");

        // Enviar el resultado a todos los participantes
        foreach (var kv in state.Proposals)
        {
            string perf = kv.Key == winner.Key
                ? FipaPerformatives.ACCEPT_PROPOSAL
                : FipaPerformatives.REJECT_PROPOSAL;
            SendPerformative(kv.Key, $"RESULT:{auctionId}", perf);
        }

        ActiveAuctions.Remove(auctionId);
        auctionsParticipating.Remove(auctionId);
    }

    // Manejo de los mensajes recibidos de subasta
    protected void HandleCfp(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];

        Vector3 target;
        float bid;

        // 1) Elige el punto de referencia según el tipo de subasta
        if (auctionId == "AUCTION_GOLD")
        {
            target = goldPoint.position;
        }
        else if (auctionId == "AUCTION_DOOR")
        {
            target = doorPoint.position;
        }
        else  // AUCTION_INTERCEPT
        {
            // Parsear la posición del ladrón
            var coords = parts[2].Split(',');
            target = new Vector3(
                float.Parse(coords[0]),
                float.Parse(coords[1]),
                float.Parse(coords[2])
            );
        }

        // 2) Calcular la puja = distancia al objetivo
        bid = Vector3.Distance(transform.position, target);

        // 3) Añadir penalizacion si estamos ocupados
        string bidderStatus = ocupado ? "_busy" : "";
        string bidderId = AgentId + bidderStatus;
        if (ocupado)
        {
            bid *= 2.0f;  
        }

        // 4) Guardar la subasta y enviar la propuesta
        ActiveAuctions[auctionId] = new AuctionState
        {
            AuctionId = auctionId,
            Target = target,
            StartTime = Time.time
        };

        Debug.Log($"[{AgentId}] Recibido CFP {auctionId}, puja={bid:F2}, ocupado={ocupado}");
        SendPerformative(msg.Sender, $"{auctionId}:{bid:F2}", FipaPerformatives.PROPOSE);
    }

    // Manejo de mensajes de propuesta
    protected void HandlePropose(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var id = parts[0];
        var bid = float.Parse(parts[1]);
        if (ActiveAuctions.TryGetValue(id, out var st))
        {
            st.Proposals[msg.Sender] = bid;
            Debug.Log($"[{AgentId}] Guardada propuesta para {id} de {msg.Sender}: {bid:F2}");
        }
    }

    // Manejo de mensajes de aceptación de propuestas
    protected void HandleAcceptProposal(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];

        // Comprobar si estamos ocupados
        if (ocupado)
        {
            Debug.Log($"[{AgentId}] Subasta ganada {auctionId} pero ocupado, rechazando");
            SendPerformative(msg.Sender, $"BUSY:{auctionId}", FipaPerformatives.REFUSE);
            return;
        }

        if (!ActiveAuctions.TryGetValue(auctionId, out var state))
            return;

        // Establecernos como ocupados
        ocupado = true;

        // Cancelar HTN
        ladronViendo = false;
        isPatrolling = false;
        if (_htnRoutine != null) { StopCoroutine(_htnRoutine); _htnRoutine = null; }
        if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.ResetPath();


        if (auctionId == "AUCTION_INTERCEPT")
        {
            StartCoroutine(new TareaInterceptar(state.Target).Ejecutar(this));
        }
        else if (auctionId == "AUCTION_GOLD")
        {
            StartCoroutine(new TareaOro(state.Target).Ejecutar(this));
        }
        else if (auctionId == "AUCTION_DOOR")
        {
            StartCoroutine(new TareaPuerta(state.Target).Ejecutar(this));
        }

        ActiveAuctions.Remove(auctionId);
    }

    // Manejo de mensajes de rechazo de propuestas
    protected void HandleRejectProposal(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];
        Debug.Log($"[{AgentId}] Recibido REJECT_PROPOSAL para {auctionId} de {msg.Sender}");

        // Si no nos tocó, volvemos a la rutina de patrulla/HTN solo si no estamos ocupados
        if (!ocupado && _htnRoutine == null)
            _htnRoutine = StartCoroutine(EjecutarPlanPeriodicamente());
    }


    // Broadcasts para distintos tipos de mensajes
    private void BroadcastThiefSighting(Vector3 position)
    {
        BroadcastMessage($"THIEF_SPOTTED:{position.x},{position.y},{position.z}", FipaPerformatives.INFORM);
    }

    private void BroadcastThiefLost()
    {
        BroadcastMessage("THIEF_LOST", FipaPerformatives.INFORM);
    }

    private void BroadcastThiefCaught()
    {
        BroadcastMessage("THIEF_CAUGHT", FipaPerformatives.INFORM);
    }
}