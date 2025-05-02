using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class Policia : CommunicationAgent
{
    private NavMeshAgent _navAgent;

    public Transform[] destinos;
    private int _currentWaypointIndex = 0;

    public bool isPatrolling = true;
    private bool isSearching = false;
    public bool thiefDetected = false;

    public Transform thiefTransform;

    public float searchDelayTime = 2f; // Wait time before starting search after losing thief
    public float searchPointWaitTime = 2f; // Time to spend at each search location

    private PlaneadorHTN planeador;
    private Coroutine _htnRoutine;  // Reference to HTN planning coroutine

    // --- FIPA Contract Net Auction ---
    public Transform goldPoint;
    public Transform doorPoint;
    public float proposalTimeout = 1f;
    public Dictionary<string, AuctionState> ActiveAuctions = new Dictionary<string, AuctionState>();

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
            IniciarPatrulla();
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
        ActualizarPatrulla();
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
            default:
                base.ProcessMessage(message);
                break;
        }
    }

    private IEnumerator EjecutarPlanPeriodicamente()
    {
        while (true)
        {
            Debug.Log($"[HTN] Planificando para {AgentId} - thiefDetected={thiefDetected}");
            planeador.Planificar(this);
            yield return StartCoroutine(planeador.EjecutarPlan(this, this));
            yield return new WaitForSeconds(1f);
        }
    }

    public void IniciarPatrulla()
    {
        if (destinos == null || destinos.Length == 0) return;
        isPatrolling = true;
        IrAlSiguienteDestino();
        Debug.Log($"Policia {AgentId}: Comenzando patrulla");
    }

    public void PausarPatrulla()
    {
        isPatrolling = false;
        if (_navAgent != null && _navAgent.isOnNavMesh)
            _navAgent.ResetPath();
        Debug.Log($"Policia {AgentId}: Pausando patrulla");
    }

    public void ActualizarPatrulla()
    {
        if (!isPatrolling || isSearching || thiefDetected) return;
        if (_navAgent != null && _navAgent.isOnNavMesh && !_navAgent.pathPending && _navAgent.remainingDistance <= 0.2f)
            IrAlSiguienteDestino();
    }

    private void IrAlSiguienteDestino()
    {
        if (destinos == null || destinos.Length == 0) return;
        _currentWaypointIndex = (_currentWaypointIndex + 1) % destinos.Length;
        var next = destinos[_currentWaypointIndex];
        if (next != null && _navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.SetDestination(next.position);
            Debug.Log($"Policia {AgentId}: Moving to waypoint {_currentWaypointIndex}");
        }
    }

    public void LadronVisto(Transform thief)
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            // Cada vez que vemos al ladrón, reiniciamos estado y subastas
            thiefDetected = true;
            thiefTransform = thief;

            Debug.Log($"Policia {AgentId}: Ladrón detectado! iniciando subastas...");
            BroadcastThiefSighting(thief.position);

            ActiveAuctions.Clear();

            StartAuction("AUCTION_INTERCEPT");
            
        }
    }

    public void LadronPerdido(List<Transform> searchPoints)
    {
        Debug.Log($"Policia {AgentId}: Ladrón perdido");
        if (!isSearching)
            BroadcastThiefLost();
    }

    public void DetenerLadron()
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            BroadcastThiefCaught();
            Debug.Log($"Policia {AgentId}: Ladron detenido!");
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

        IniciarPatrulla();
        isSearching = false;
        thiefDetected = false;
        Debug.Log($"Policia {AgentId}: Search complete, resuming patrol");
    }

    #region Auction Methods

    // Helper genérico para enviar cualquier performativo FIPA-ACL
    private void SendPerformative(string receiver, string content, string performative)
    {
        var msg = new FipaAclMessage
        {
            Sender = AgentId,
            Receivers = new List<string> { receiver },
            Content = content,
            Performative = performative,
            ConversationId = System.Guid.NewGuid().ToString()
        };
        MessageService.Instance.SendMessage(msg);
        Debug.Log($"[{AgentId}] Sent {performative} to {receiver}: {content}");
    }

    public void StartAuction(string auctionId)
    {
        Vector3 target = auctionId == "AUCTION_INTERCEPT"
            ? thiefTransform.position
            : (auctionId == "AUCTION_GOLD" ? goldPoint.position : doorPoint.position);

        var state = new AuctionState { AuctionId = auctionId, Target = target, StartTime = Time.time };
        ActiveAuctions[auctionId] = state;

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
            yield break;
        }

        Debug.Log($"[{AgentId}] Received {state.Proposals.Count} proposals for {auctionId}");
        foreach (var p in state.Proposals)
            Debug.Log($"  Proposal from {p.Key}: {p.Value:F2}");

        var winner = state.Proposals.Aggregate((l, r) => l.Value < r.Value ? l : r);
        Debug.Log($"[{AgentId}] Winner for {auctionId} is {winner.Key} with bid {winner.Value:F2}");

        foreach (var kv in state.Proposals)
        {
            string perf = kv.Key == winner.Key
                ? FipaPerformatives.ACCEPT_PROPOSAL
                : FipaPerformatives.REJECT_PROPOSAL;
            SendPerformative(kv.Key, $"RESULT:{auctionId}", perf);
        }

        ActiveAuctions.Remove(auctionId);
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

        // 2) Bid = distancia desde este policía al target correspondiente
        bid = Vector3.Distance(transform.position, target);

        // 3) Guardar el estado para la propuesta y enviar PROPOSE
        ActiveAuctions[auctionId] = new AuctionState
        {
            AuctionId = auctionId,
            Target = target,
            StartTime = Time.time
        };

        Debug.Log($"[{AgentId}] Received CFP {auctionId}, bid={bid:F2}");
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

        if (!ActiveAuctions.TryGetValue(auctionId, out var state))
            return;

        // Común: cancelamos HTN & patrulla
        thiefDetected = false;
        isPatrolling = false;
        if (_htnRoutine != null) { StopCoroutine(_htnRoutine); _htnRoutine = null; }
        _navAgent.ResetPath();

        if (auctionId == "AUCTION_INTERCEPT")
        {
            Debug.Log($"[{AgentId}] Ganador INTERCEPT, moviendo a {state.Target}");
            _navAgent.SetDestination(state.Target);

            // **Cuando acabe la persecución**, arranca las subastas de oro y puerta
            StartCoroutine(AfterIntercept());
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

    // Coroutine auxiliar que espera a que acabe la persecución, y luego lanza las siguientes subastas
    private IEnumerator AfterIntercept()
    {
        // Opcional: espera a que el agente llegue
        while (_navAgent.pathPending || _navAgent.remainingDistance > 0.5f)
            yield return null;

        // Un pequeño retardo para dar tiempo a todos a volver a escuchar
        yield return new WaitForSeconds(0.5f);

        // Ahora lanzo las subastas pendientes
        Debug.Log($"[{AgentId}] Arrancando subasta de oro y puerta tras intercept");
        ActiveAuctions.Clear();
        StartAuction("AUCTION_GOLD");
        StartAuction("AUCTION_DOOR");
    }




    protected void HandleRejectProposal(FipaAclMessage msg)
    {
        var parts = msg.Content.Split(':');
        var auctionId = parts[1];
        Debug.Log($"[{AgentId}] Received REJECT_PROPOSAL for {auctionId} from {msg.Sender}");

        // Si no nos tocó, volvemos a la rutina de patrulla/HTN:
        

        // Reinicia el HTN si estaba parado
        if (_htnRoutine == null)
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
