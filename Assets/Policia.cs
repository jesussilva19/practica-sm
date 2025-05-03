using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Policia : CommunicationAgent
{
    public NavMeshAgent _navAgent;

    public Transform[] destinos;
    public int _currentWaypointIndex = 0;

    public bool isPatrolling = true;
    public bool isSearching = false;
    public bool ocupado = false;  // Flag that indicates if the agent is busy with a task

    // Estados de detección del ladrón
    public bool ladronVisto = false;    // Ha sido visto alguna vez
    public bool ladronViendo = false;   // Lo estamos viendo ahora mismo
    public bool ladronPerdido = false;  // Lo vimos pero lo perdimos
    public Transform thiefTransform;    // Referencia al transform del ladrón

    public float searchDelayTime = 2f; // Wait time before starting search after losing thief
    public float searchPointWaitTime = 2f; // Time to spend at each search location

    // Puntos de interés
    public Transform goldPoint;
    public Transform doorPoint;

    // Sistema de subastas FIPA
    public float proposalTimeout = 1f;
    public Dictionary<string, AuctionState> ActiveAuctions = new Dictionary<string, AuctionState>();
    private HashSet<string> auctionsParticipating = new HashSet<string>();  // Track auctions we're participating in

    // Planificador HTN
    private PlaneadorHTN planeador;
    private Coroutine _htnRoutine;  // Reference to HTN planning coroutine

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
        if (planeador != null)
        {
            planeador.Planificar(this);
        }
        // Puede añadir lógica adicional aquí si lo necesita
    }

    /// <summary>Override message processing to handle auction performatives</summary>
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

    private IEnumerator EjecutarPlanPeriodicamente()
    {
        while (true)
        {
            Debug.Log($"[HTN] Planificando para {AgentId} - ladronViendo={ladronViendo}, ladronVisto={ladronVisto}, ocupado={ocupado}");

            // Only plan if not busy with auctions
            if (!ocupado)
            {
                planeador.Planificar(this);
                yield return StartCoroutine(planeador.EjecutarPlan(this, this));
            }

            yield return new WaitForSeconds(1f);
        }
    }


    public void LadronVisto(Transform thief)
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            // Actualizar estados de detección
            ladronViendo = true;
            ladronVisto = true;
            ladronPerdido = false;
            thiefTransform = thief;

            // If we're busy with an auction task, we can interrupt it for a high-priority thief pursuit
            if (ocupado)
            {
                Debug.Log($"Policia {AgentId}: Interrumpiendo tarea actual para perseguir al ladrón");
                // Don't set ocupado to false here as the task should handle the cleanup
            }
            _navAgent.SetDestination(thief.position);

            // Broadcast a otros policías
            Debug.Log($"Policia {AgentId}: Ladrón detectado en {thief.position}! iniciando subastas...");
            BroadcastThiefSighting(thief.position);

            // Iniciar subasta para interceptar solo si no estamos ocupados
            if (!ocupado)
            {
                auctionsParticipating.Clear();
                StartAuction("AUCTION_INTERCEPT");
                StartAuction("AUCTION_GOLD");
                StartAuction("AUCTION_DOOR");
            }
        }
    }

    public void LadronPerdido(List<Transform> searchPoints)
    {
        Debug.Log($"Policia {AgentId}: Ladrón perdido");
        ladronViendo = false;
        ladronPerdido = true;

        // Notificar a otros policías
        if (!isSearching)
            BroadcastThiefLost();

        // Iniciar búsqueda si hay puntos y no estamos ocupados
        if (searchPoints != null && searchPoints.Count > 0 && !ocupado)
        {
            StartCoroutine(BuscarYLuegoPatrullar(searchPoints));
        }
    }

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

    private IEnumerator BuscarYLuegoPatrullar(List<Transform> searchPoints)
    {
        isSearching = true;
        if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.isStopped = true;
        Debug.Log($"Policia {AgentId}: Esperando antes de buscar");
        yield return new WaitForSeconds(searchDelayTime);
        if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.isStopped = false;

        foreach (var point in searchPoints)
        {
            if (point == null) continue;
            if (_navAgent != null && _navAgent.isOnNavMesh)
            {
                _navAgent.SetDestination(point.position);
                while (_navAgent.pathPending || _navAgent.remainingDistance > 0.5f)
                    yield return null;
                yield return new WaitForSeconds(searchPointWaitTime);
            }
        }

        isSearching = false;
        Debug.Log($"Policia {AgentId}: Search complete, resuming patrol");
    }

    #region Auction Methods

    public void StartAuction(string auctionId)
    {
        Vector3 target = auctionId == "AUCTION_INTERCEPT"
            ? thiefTransform.position
            : (auctionId == "AUCTION_GOLD" ? goldPoint.position : doorPoint.position);

        var state = new AuctionState { AuctionId = auctionId, Target = target, StartTime = Time.time };
        ActiveAuctions[auctionId] = state;

        // Remember that we're participating in this auction
        auctionsParticipating.Add(auctionId);

        BroadcastMessage($"CFP:{auctionId}:{target.x},{target.y},{target.z}", FipaPerformatives.CFP);
        Debug.Log($"[{AgentId}] Broadcast CFP for {auctionId} at {target}");

        StartCoroutine(ResolveAuction(auctionId));
    }

    private IEnumerator ResolveAuction(string auctionId)
    {
        var state = ActiveAuctions[auctionId];
        yield return new WaitForSeconds(proposalTimeout);

        if (state.Proposals.Count == 0)
        {
            Debug.LogWarning($"[{AgentId}] Sin propuestas para {auctionId}");
            ActiveAuctions.Remove(auctionId);
            auctionsParticipating.Remove(auctionId);
            yield break;
        }

        Debug.Log($"[{AgentId}] Received {state.Proposals.Count} proposals for {auctionId}");
        foreach (var p in state.Proposals)
            Debug.Log($"  Proposal from {p.Key}: {p.Value:F2}");

        // If we have proposals, we need to filter out any from agents who are already busy
        var availableProposals = state.Proposals
            .Where(p => !p.Key.Contains("_busy"))
            .ToDictionary(p => p.Key, p => p.Value);

        if (availableProposals.Count == 0)
        {
            Debug.LogWarning($"[{AgentId}] No available agents for {auctionId}, all are busy");
            ActiveAuctions.Remove(auctionId);
            auctionsParticipating.Remove(auctionId);
            yield break;
        }

        var winner = availableProposals.Aggregate((l, r) => l.Value < r.Value ? l : r);
        Debug.Log($"[{AgentId}] Winner for {auctionId} is {winner.Key} with bid {winner.Value:F2}");

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

        // 2) Calculate bid = distance from this police to the target
        bid = Vector3.Distance(transform.position, target);

        // 3) Add a penalty if busy (to make it less likely we'll win multiple auctions)
        string bidderStatus = ocupado ? "_busy" : "";
        string bidderId = AgentId + bidderStatus;

        // 4) If we're busy, artificially increase our bid to make it less competitive
        if (ocupado)
        {
            bid *= 2.0f;  // Double the bid if we're already busy
        }

        // 5) Save the auction state and send PROPOSE
        ActiveAuctions[auctionId] = new AuctionState
        {
            AuctionId = auctionId,
            Target = target,
            StartTime = Time.time
        };

        Debug.Log($"[{AgentId}] Received CFP {auctionId}, bid={bid:F2}, ocupado={ocupado}");
        SendPerformative(msg.Sender, $"{auctionId}:{bid:F2}", FipaPerformatives.PROPOSE);
    }

    protected void HandlePropose(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var id = parts[0];
        var bid = float.Parse(parts[1]);
        if (ActiveAuctions.TryGetValue(id, out var st))
        {
            st.Proposals[msg.Sender] = bid;
            Debug.Log($"[{AgentId}] Recorded proposal for {id} from {msg.Sender}: {bid:F2}");
        }
    }

    protected void HandleAcceptProposal(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];

        // Check if we're already busy with another auction
        if (ocupado)
        {
            Debug.Log($"[{AgentId}] Won auction {auctionId} but already busy, sending refuse");
            SendPerformative(msg.Sender, $"BUSY:{auctionId}", FipaPerformatives.REFUSE);
            return;
        }

        if (!ActiveAuctions.TryGetValue(auctionId, out var state))
            return;

        // Set as busy now that we've accepted the auction
        ocupado = true;

        // Cancel HTN & patrol
        ladronViendo = false;
        isPatrolling = false;
        if (_htnRoutine != null) { StopCoroutine(_htnRoutine); _htnRoutine = null; }
        if (_navAgent != null && _navAgent.isOnNavMesh) _navAgent.ResetPath();

        if (auctionId == "AUCTION_INTERCEPT")
        {
            Debug.Log($"[{AgentId}] Ganador INTERCEPT, moviendo a {state.Target}");
            _navAgent.SetDestination(state.Target);

            // Corrutina para monitorear si vemos al ladrón mientras nos movemos
            StartCoroutine(MonitorearLadronDuranteIntercept(state.Target));
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

    private IEnumerator MonitorearLadronDuranteIntercept(Vector3 lastKnownPosition)
    {
        // Esperar a que nos acerquemos a la posición
        while (_navAgent.pathPending || _navAgent.remainingDistance > 0.5f)
        {
            // Si vemos al ladrón mientras vamos hacia allí, esto se interrumpirá por otra acción
            if (ladronViendo)
            {
                yield break;
            }
            yield return null;
        }

        // Llegamos a la última posición conocida
        Debug.Log($"[{AgentId}] Llegué a la última posición del ladrón, pero no lo veo");

        // Esperar un poco para ver si aparece
        yield return new WaitForSeconds(1.5f);

        // Si no lo hemos visto en este tiempo
        if (!ladronViendo)
        {
            // Dado que ya no estamos en intercepción, marcamos como no ocupado
            ocupado = false;

            // Lanzar las subastas de oro y puerta si no estamos viendo al ladrón
            Debug.Log($"[{AgentId}] No encontré al ladrón, iniciando subastas de oro y puerta");
            ActiveAuctions.Clear();
            auctionsParticipating.Clear();
            StartAuction("AUCTION_GOLD");
            StartAuction("AUCTION_DOOR");
        }
    }

    protected void HandleRejectProposal(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];
        Debug.Log($"[{AgentId}] Received REJECT_PROPOSAL for {auctionId} from {msg.Sender}");

        // Si no nos tocó, volvemos a la rutina de patrulla/HTN solo si no estamos ocupados
        if (!ocupado && _htnRoutine == null)
            _htnRoutine = StartCoroutine(EjecutarPlanPeriodicamente());
    }

    #endregion

    // --- Broadcast helpers ---
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