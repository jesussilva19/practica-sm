using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Persecucion : MonoBehaviour
{
    public Transform ladron; // Referencia al ladrón
    public Transform[] puntosBusqueda; // Puntos donde buscar tras perder al ladrón

    private float tiempoEsperaBusqueda = 3f; // Tiempo de espera antes de buscar
    private float distanciaMinima = 5f; //  Distancia mínima para llegar a un punto

    private NavMeshAgent agentePolicia;
    private Agente patrullaPolicia;
    private bool enBusqueda = false; 
    private bool haVistoAlLadron = false; // Indica si alguna vez lo ha visto

    private void Start()
    {
        agentePolicia = GetComponentInParent<NavMeshAgent>();
        patrullaPolicia = GetComponentInParent<Agente>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            haVistoAlLadron = true; // Activa persecución solo si lo ve
            patrullaPolicia.PausarPatrulla();
            agentePolicia.SetDestination(ladron.position);
            Debug.Log("Policía detectó al ladrón. ¡Iniciando persecución!");
        }
        else
        {
            haVistoAlLadron = false;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform == ladron)
        {
            if (TieneLineaDeVision())
            {
                haVistoAlLadron = true; // Solo lo persigue si ya lo vio antes
                agentePolicia.SetDestination(ladron.position);
                Debug.Log("👀 Policía sigue viendo al ladrón.");
            }
            else
            {
                if (haVistoAlLadron) //Solo si antes lo vio
                {
                    Debug.Log("Perdí al ladrón tras una pared. Deteniéndome...");
                    agentePolicia.ResetPath();
                    if (!enBusqueda)
                    {
                        StartCoroutine(EsperarYBuscarAntesDePatrullar());
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == ladron)
        {
            if (haVistoAlLadron) // Solo busca si lo había visto antes
            {
                Debug.Log(" El ladrón salió del área de detección.");
                if (!enBusqueda)
                {
                    StartCoroutine(EsperarYBuscarAntesDePatrullar());
                    haVistoAlLadron = false;
                }
            }
        }
    }

    // Corrutina: Espera y revisa puntos antes de volver a patrullar
    private IEnumerator EsperarYBuscarAntesDePatrullar()
    {
        enBusqueda = true;
        agentePolicia.isStopped = true;
        yield return new WaitForSeconds(tiempoEsperaBusqueda); // Espera unos segundos antes de buscar

        agentePolicia.isStopped = false;

        // Revisar los puntos de búsqueda antes de patrullar
        foreach (Transform punto in puntosBusqueda)
        {
            if (punto != null)
            {
                agentePolicia.SetDestination(punto.position);
                Debug.Log("Buscando en: " + punto.position);

                // Esperar hasta llegar al punto
                while (agentePolicia.pathPending || agentePolicia.remainingDistance > distanciaMinima)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(tiempoEsperaBusqueda); //  Espera unos segundos en cada punto
            }
        }

        // Cuando termine la búsqueda, vuelve a la patrulla normal
        patrullaPolicia.ReanudarPatrulla();
        enBusqueda = false;
        Debug.Log("Finalizada la búsqueda. Volviendo a patrullar.");
    }

    // Verifica si el policía tiene línea de visión del ladrón
    private bool TieneLineaDeVision()
    {
        Vector3 origen = agentePolicia.transform.position + Vector3.up * 1f; // Nivel de los ojos
        Vector3 direccion = (ladron.position - origen).normalized;
        float distancia = Vector3.Distance(agentePolicia.transform.position, ladron.position);



        Vector3 forward = agentePolicia.transform.forward; 

        
        float angulo = Vector3.Angle(forward, direccion);

        float anguloVision = 60f;

        if (angulo > anguloVision)
        {
            Debug.Log("🚧 Ladrón fuera del campo de visión.");
            return false;
        }





        // Solo detectar objetos en "Obstaculos" y "Ladron"
        int mascara = LayerMask.GetMask("Obstaculos", "Ladron");

        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, distancia, mascara))
        {
            Debug.DrawRay(origen, direccion * distancia, Color.red, 0.5f);
            Debug.Log("El Raycast golpeó: " + hit.transform.name + " en la capa: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            // Si golpea un obstáculo antes del ladrón, FALSO 
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstaculos"))
            {
                Debug.Log("El ladrón está bloqueado por: " + hit.transform.name);
                return false;
            }

            // Si el primer objeto golpeado es el ladrón, VERDADERO
            else
            {
                Debug.Log("¡El policía ve al ladrón!");
                return true;
            }
        }

        
        return true; 
    }
}
