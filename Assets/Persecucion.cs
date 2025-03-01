using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Persecucion : MonoBehaviour
{
    public Transform ladron; 
    public Transform[] puntosBusqueda; 
    public float tiempoEsperaBusqueda = 3f; 
    public float distanciaMinima = 0.5f; 

    private NavMeshAgent agentePolicia;
    private Agente patrullaPolicia;
    private bool enBusqueda = false; 
    private bool haVistoAlLadron = false; 

    private void Start()
    {
        agentePolicia = GetComponentInParent<NavMeshAgent>();
        patrullaPolicia = GetComponentInParent<Agente>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            haVistoAlLadron = true; 
            patrullaPolicia.PausarPatrulla();
            agentePolicia.SetDestination(ladron.position);
            Debug.Log("🚔 Policía detectó al ladrón. ¡Iniciando persecución!");
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
                haVistoAlLadron = true; 
                agentePolicia.SetDestination(ladron.position);
                Debug.Log("👀 Policía sigue viendo al ladrón.");
            }
            else
            {
                if (haVistoAlLadron)
                {
                    Debug.Log("🚧 Perdí al ladrón tras una pared. Deteniéndome...");
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
            if (haVistoAlLadron) 
            {
                Debug.Log("🏃‍♂️ El ladrón salió del área de detección.");
                if (!enBusqueda)
                {
                    StartCoroutine(EsperarYBuscarAntesDePatrullar());
                    haVistoAlLadron = false;
                }
            }
        }
    }

   
    private IEnumerator EsperarYBuscarAntesDePatrullar()
    {
        enBusqueda = true;
        agentePolicia.isStopped = true;
        yield return new WaitForSeconds(tiempoEsperaBusqueda); 

        agentePolicia.isStopped = false;

        foreach (Transform punto in puntosBusqueda)
        {
            if (punto != null)
            {
                agentePolicia.SetDestination(punto.position);
                Debug.Log("🔎 Buscando en: " + punto.position);

               
                while (agentePolicia.pathPending || agentePolicia.remainingDistance > distanciaMinima)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(2f); 
            }
        }

        patrullaPolicia.ReanudarPatrulla();
        enBusqueda = false;
        Debug.Log("🔄 Finalizada la búsqueda. Volviendo a patrullar.");
    }

    private bool TieneLineaDeVision()
    {
        Vector3 origen = agentePolicia.transform.position + Vector3.up * 1f; 
        Vector3 direccion = (ladron.position - origen).normalized;
        float distancia = Vector3.Distance(agentePolicia.transform.position, ladron.position);

        int mascara = LayerMask.GetMask("Obstaculos", "Ladron");

        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, distancia, mascara))
        {
            Debug.DrawRay(origen, direccion * distancia, Color.red, 0.5f);
            Debug.Log("🔎 El Raycast golpeó: " + hit.transform.name + " en la capa: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstaculos"))
            {
                Debug.Log("🚧 El ladrón está bloqueado por: " + hit.transform.name);
                return false;
            }

 
            else
            {
                Debug.Log("🚨 ¡El policía ve al ladrón!");
                return true;
            }
        }

        Debug.Log("⚠️ El Raycast NO golpeó nada.");
        return true; 
    }
}
