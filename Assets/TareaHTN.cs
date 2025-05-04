using System.Collections;
using UnityEngine;
using UnityEngine.AI;


public abstract class TareaHTN
{
    public abstract bool EsEjecutable(Policia policia);
    public abstract IEnumerator Ejecutar(Policia policia);
}



// Clase que representa una tarea de patrullaje para un agente de policía
public class TareaPatrullar : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        // La tarea es ejecutable si el policía no está viendo al ladrón y no está ocupado
        return !policia.ladronViendo && !policia.ocupado ;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        // Comprobaciones iniciales
        if (!EsEjecutable(policia)) yield break;
        if (policia._navAgent == null || !policia._navAgent.isOnNavMesh) yield break;

        // Comprobar sie el policia ha llegado a su destino
        if (!policia._navAgent.pathPending && policia._navAgent.remainingDistance <= 0.2f)
        {
            // Asegurarse que hay destinos disponibles
            if (policia.destinos == null || policia.destinos.Length == 0) yield break;

            // Moverse al siguiente destino
            policia._currentWaypointIndex = (policia._currentWaypointIndex + 1) % policia.destinos.Length;
            var next = policia.destinos[policia._currentWaypointIndex];

            if (next != null)
            {
                policia._navAgent.SetDestination(next.position);
                Debug.Log($"Policia {policia.AgentId}: Moviendose a {policia._currentWaypointIndex}");
            }
        }

        yield return null;
    }
}



// Clase que representa una tarea de patrullaje para un agente de policía
public class TareaPerseguir : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        // La tarea es ejecutable si el policía está viendo al ladrón, tiene su referencia y no está ocupado
        return policia.ladronViendo && policia.thiefTransform != null;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        // Pausar la patrulla
        policia.isPatrolling = false;

        if (policia._navAgent == null || !policia._navAgent.isOnNavMesh)
        {
            Debug.LogError($"Policia {policia.AgentId}: NavMeshAgent no está disponible o no está en el NavMesh.");
            yield break;
        }

        // Reiniciar el camino actual
        policia._navAgent.ResetPath();
        Debug.Log($"Policia {policia.AgentId}: Pausando patrulla y comenzando persecución.");

        // Bucle de persecución
        while (policia.ladronViendo && policia.thiefTransform != null)
        {
            // Actualizar el destino del NavMeshAgent hacia la posición del ladrón
            if (policia._navAgent.isOnNavMesh)
            {
                policia._navAgent.SetDestination(policia.thiefTransform.position);
                Debug.Log($"Policia {policia.AgentId}: Persiguiendo al ladrón en {policia.thiefTransform.position}");
            }

            yield return new WaitForSeconds(0.2f); // Actualiza cada 0.2 segundos
        }

    }
}

// Clase que representa una tarea de lanzar una subasta para un agente de policía
public class TareaAuction : TareaHTN
{
    private readonly string _auctionId;

    public TareaAuction(string auctionId)
    {
        _auctionId = auctionId;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Solo lanzar si aun no existe esa subasta
        return !policia.ActiveAuctions.ContainsKey(_auctionId);
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.Log($"[HTN] Lanzando subasta {_auctionId} desde {policia.AgentId}");
        policia.StartAuction(_auctionId);
        yield break;
    }
}


public class TareaDarRefuerzo : TareaHTN
{
    private Vector3 posicionRefuerzo;

    public TareaDarRefuerzo(Vector3 posicion)
    {
        posicionRefuerzo = posicion;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Siempre se puede intentar acudir si no est� ocupado
        return true;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.LogError("AAAAAAAAAAAAaaaa");
        policia.isPatrolling = false;
        if (policia._navAgent != null && policia._navAgent.isOnNavMesh)
            policia._navAgent.ResetPath();
        Debug.Log($"Policia {policia.AgentId}: Pausando patrulla"); policia.GetComponent<NavMeshAgent>().SetDestination(posicionRefuerzo);

        while (policia.GetComponent<NavMeshAgent>().pathPending || policia.GetComponent<NavMeshAgent>().remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log($"{policia.AgentId}: Lleg� a la zona de refuerzo.");
        yield return new WaitForSeconds(3f); // Simular espera

    }
}


// Clase que representa una tarea de ir a la ubicacion del oro
public class TareaOro : TareaHTN
{
    private Vector3 _target;

    public TareaOro(Vector3 target)
    {
        _target = target;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Esta tarea siempre es ejecutable si es asignada, es prioritaria
        return  true;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.Log($"[HTN] TareaOro: {policia.AgentId} moviendose al oro {_target}");
        policia.ocupado = true;
        var nav = policia.GetComponent<NavMeshAgent>();
        if (nav == null)
        {
            Debug.LogError($"[HTN] TareaOro: {policia.AgentId} missing NavMeshAgent component");
            yield break;
        }

        nav.SetDestination(_target);
        // Esperar hasta que el agente llegue al oro
        while (nav.pathPending || nav.remainingDistance > 0.5f)
        {
            yield return null;
        }

        Debug.Log($"[HTN] TareaOro: {policia.AgentId} ha llegado al oro");
        yield return new WaitForSeconds(10f);
        yield break;
    }
}


// Clase que representa una tarea de ir a la ubicacion de la puerta principal
public class TareaPuerta : TareaHTN
{
    private Vector3 _target;
    public TareaPuerta(Vector3 target) { _target = target; }

    public override bool EsEjecutable(Policia policia) => true;

    public override IEnumerator Ejecutar(Policia policia)
    {
        policia.ocupado = true;
        var nav = policia.GetComponent<NavMeshAgent>();
        nav.SetDestination(_target);
        while (nav.pathPending || nav.remainingDistance > 0.5f) yield return null;
        Debug.Log($"[HTN] TareaPuerta: {policia.AgentId} ha llegado a la puerta");

    }
}


