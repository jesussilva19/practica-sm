using System.Collections;
using System.Collections.Generic;
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

    public float searchDelayTime = 2f; //Wait time before starting search after losing thief
    public float searchPointWaitTime = 2f; //Time to spend at each search location

    private PlaneadorHTN planeador;

    protected override void Awake()
    {
        base.Awake();
        _navAgent = GetComponent<NavMeshAgent>();

        // Validate components
        if (_navAgent == null)
        {
            Debug.LogError($"Policia {AgentId}: NavMeshAgent component not found");
        }
    }

    protected override void Start()
    {
        base.Start();

        // Comienza a patrullar si hay destinios
        if (destinos != null && destinos.Length > 0)
        {
            IniciarPatrulla();
            planeador = new PlaneadorHTN();
            StartCoroutine(EjecutarPlanPeriodicamente());
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

    private IEnumerator EjecutarPlanPeriodicamente()
    {
        while (true)
        {
            planeador.Planificar(this);
            yield return StartCoroutine(planeador.EjecutarPlan(this, this));
            yield return new WaitForSeconds(1f); // Esperar un poco antes de volver a planificar
        }
    }

    public void IniciarPatrulla()
    {
        if (destinos == null || destinos.Length == 0)
        {
            Debug.LogWarning($"Policia {AgentId}: No hay destinos asignados");
            return;
        }

        isPatrolling = true;
        IrAlSiguienteDestino();
        Debug.Log($"Policia {AgentId}: Comenzando patrulla");
    }


    public void PausarPatrulla()
    {
        isPatrolling = false;
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.ResetPath();
        }
        Debug.Log($"Policia {AgentId}: Pausando patrulla");
    }


    public void ActualizarPatrulla()
    {
        if (!isPatrolling || isSearching || thiefDetected)
            return;

        // Check if we've reached the current waypoint
        if (_navAgent != null && _navAgent.isOnNavMesh && !_navAgent.pathPending && _navAgent.remainingDistance <= 0.2f)
        {
            // Move to the next waypoint
            IrAlSiguienteDestino();
        }
    }

    /// Move to the next patrol waypoint
    private void IrAlSiguienteDestino()
    {
        if (destinos == null || destinos.Length == 0)
            return;

        _currentWaypointIndex = (_currentWaypointIndex + 1) % destinos.Length;

        if (destinos[_currentWaypointIndex] == null)
        {
            Debug.LogWarning($"Policia {AgentId}: Waypoint at index {_currentWaypointIndex} is null");
            return;
        }

        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.SetDestination(destinos[_currentWaypointIndex].position);
            Debug.Log($"Policia {AgentId}: Moving to waypoint {_currentWaypointIndex}");
        }
    }


    public void LadronVisto(Transform thief)
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            PausarPatrulla();
            thiefDetected = true;
            thiefTransform = thief;
            //_navAgent.SetDestination(thief.position);

            Debug.Log($"Policia {AgentId}: Ladrón detectado! persiguiendo...");

            // Inform other agents about thief sighting
            BroadcastThiefSighting(thief.position);
        }
    }

    /// Called when this agent loses sight of the thief
    public void LadronPerdido(List<Transform> searchPoints)
    {
        Debug.Log($"Policia {AgentId}: Ladrón perdido");

        if (!isSearching)
        {
            BroadcastThiefLost();// Broadcast ladron perdido
            //StartCoroutine(BuscarYLuegoPatrullar(searchPoints));
        }
    }

    /// Called when this agent catches the thief
    public void DetenerLadron()
    {
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            BroadcastThiefCaught();
            Debug.Log($"Policia {AgentId}: Ladron detenido!");
        }
    }

    /// Search pattern coroutine after losing sight of the thief
    private IEnumerator BuscarYLuegoPatrullar(List<Transform> searchPoints)
    {
        isSearching = true;

        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
        }

        Debug.Log($"Policia {AgentId}: Esperando antes de buscar");
        yield return new WaitForSeconds(searchDelayTime);

        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = false;
        }

        // Check each search point
        foreach (Transform point in searchPoints)
        {
            if (point != null)
            {
                if (_navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.SetDestination(point.position);
                    Debug.Log($"Policia {AgentId}: Buscando en {point.name}");

                    // Wait until we reach the search point
                    while (_navAgent.pathPending || _navAgent.remainingDistance > 0.5f)
                    {
                        yield return null;
                    }

                    // Wait at the search point
                    yield return new WaitForSeconds(searchPointWaitTime);
                }
            }
        }

        // Return to patrol
        IniciarPatrulla();
        isSearching = false;
        thiefDetected = false;
        Debug.Log($"Policia {AgentId}: Search complete, resuming patrol");
    }

 
    /// Handle INFORM messages from other agents
    protected override void HandleInform(FipaAclMessage message)
    {
        // Parse message content
        string[] parts = message.Content.Split(':');
        string messageType = parts[0];

        switch (messageType)
        {
            case "THIEF_SPOTTED":
                HandleThiefSpottedMessage(message, parts);
                break;

            case "THIEF_LOST":
                Debug.Log($"Policia {AgentId}: Received report that thief was lost by {message.Sender}");
                break;

            case "THIEF_CAUGHT":
                HandleThiefCaughtMessage(message);
                break;

            default:
                base.HandleInform(message);
                break;
        }
    }

    /// Handle a thief spotted message from another agent
    private void HandleThiefSpottedMessage(FipaAclMessage message, string[] parts)
    {
        // Only respond if not already pursuing a thief
        if (!thiefDetected && !isSearching && parts.Length > 1)
        {
            string[] coordinates = parts[1].Split(',');
            if (coordinates.Length >= 3)
            {
                float x = float.Parse(coordinates[0]);
                float y = float.Parse(coordinates[1]);
                float z = float.Parse(coordinates[2]);
                Vector3 thiefPosition = new Vector3(x, y, z);

                // Decide whether to respond based on distance
                float distance = Vector3.Distance(transform.position, thiefPosition);
                if (distance < 50f) // Response radius
                {
                    Debug.Log($"Policia {AgentId}: Responding to thief sighting by {message.Sender}");

                    // Go to the reported position
                    PausarPatrulla();
                    if (_navAgent != null && _navAgent.isOnNavMesh)
                    {
                        _navAgent.SetDestination(thiefPosition);
                    }
                }
                else
                {
                    Debug.Log($"Policia {AgentId}: Thief sighting too far away to respond ({distance:F1} units)");
                }
            }
        }
    }

    /// Handle a thief caught message from another agent
    private void HandleThiefCaughtMessage(FipaAclMessage message)
    {
        // If we were pursuing the thief, return to patrol
        if (thiefDetected)
        {
            Debug.Log($"Policia {AgentId}: Received notification that thief was caught by {message.Sender}");
            thiefDetected = false;
            IniciarPatrulla();
        }
    }


    /// Broadcast thief sighting to all other agents
    private void BroadcastThiefSighting(Vector3 position)
    {
        string content = $"THIEF_SPOTTED:{position.x},{position.y},{position.z}";
        BroadcastMessage(content, FipaPerformatives.INFORM);
    }

    /// Broadcast that thief was lost
    private void BroadcastThiefLost()
    {
        BroadcastMessage("THIEF_LOST", FipaPerformatives.INFORM);
    }

    /// Broadcast that thief was caught
    private void BroadcastThiefCaught()
    {
        BroadcastMessage("THIEF_CAUGHT", FipaPerformatives.INFORM);
    }

}