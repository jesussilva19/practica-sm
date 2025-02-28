using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class Persecucion : MonoBehaviour
{
    public Transform ladron;
    public Transform[] puntosBusqueda; // ✅ Dos destinos extra antes de patrullar

    private NavMeshAgent agentePolicia;
    private Agente patrullaPolicia;

    private void Start()
    {
        agentePolicia = GetComponentInParent<NavMeshAgent>();
        patrullaPolicia = GetComponentInParent<Agente>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == ladron && TieneLineaDeVision())
        {
            patrullaPolicia.PausarPatrulla();
            agentePolicia.SetDestination(ladron.position);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.transform == ladron)
        {
            if (TieneLineaDeVision())
            {
                agentePolicia.SetDestination(ladron.position);
            }
            else
            {
                agentePolicia.ResetPath();
                patrullaPolicia.ReanudarPatrulla();
                Debug.Log("Perdí al ladrón tras pared. Vuelvo a patrullar.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform == ladron)
        {
            StartCoroutine(EsperarYBuscarAntesDePatrullar());
            Debug.Log("El ladrón salió del área. Esperando antes de investigar.");
        }
    }

    // 🕵️‍♂️ Corrutina: Espera 3 segundos, luego revisa dos puntos antes de patrullar
    private IEnumerator EsperarYBuscarAntesDePatrullar()
    {
        // 🛑 Detenerse antes de buscar
        agentePolicia.isStopped = true;
        yield return new WaitForSeconds(3f); // ✅ Espera 3 segundos antes de moverse
        agentePolicia.isStopped = false;

        // 🔎 Revisar los dos puntos de búsqueda
        foreach (Transform punto in puntosBusqueda)
        {
            if (punto != null)
            {
                agentePolicia.SetDestination(punto.position);
                Debug.Log("Investigando punto: " + punto.position);

                // ✅ Esperar hasta llegar al punto antes de seguir
                while (agentePolicia.pathPending || agentePolicia.remainingDistance > 0.5f)
                {
                    yield return null;
                }

                yield return new WaitForSeconds(2f); // ✅ Espera 2 segundos en cada punto antes de moverse
            }
        }

        // ✅ Cuando termine la búsqueda, vuelve a la patrulla normal
        patrullaPolicia.ReanudarPatrulla();
        Debug.Log("Finalizada la búsqueda. Volviendo a patrulla normal.");
    }
    private bool TieneLineaDeVision()
    {
        Vector3 origen = agentePolicia.transform.position + Vector3.up * 1.8f; // 📌 Nivel de los ojos
        Vector3 direccion = (ladron.position - agentePolicia.transform.position).normalized;
        float distancia = Vector3.Distance(agentePolicia.transform.position, ladron.position);

        // 🚨 Solo detectamos capas "Ladron" y "Obstaculos"
        int mascara = LayerMask.GetMask("Ladron", "Obstaculos");

        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, distancia, mascara))
        {
            Debug.DrawRay(origen, direccion * distancia, Color.red, 5f);

            Debug.Log("🔎 El Raycast golpeó: " + hit.transform.name + " en la capa: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            // ❌ Si golpea una pared, el policía NO debe ver al ladrón
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Obstaculo"))
            {
                Debug.Log("🚧 El rayo golpeó una pared. BLOQUEANDO VISIÓN.");
                return false;  // 🔴 Ahora bloquea correctamente la visión
            }

            // ✅ Si golpea directamente al ladrón, lo detecta
            if (hit.transform == ladron)
            {
                Debug.Log("👀 El policía VE al ladrón!");
                return true;
            }
        }

        Debug.Log("⚠️ El Raycast NO golpeó nada.");
        return false; // Si no golpea nada, no lo ve
    }


}




