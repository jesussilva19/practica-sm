using UnityEngine;
using UnityEngine.AI;
using System.Collections;
public class Persecucion : MonoBehaviour
{
    public Transform ladron;
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
            StartCoroutine(EsperarAntesDePatrullar()); // Inicia la espera antes de patrullar
            Debug.Log("El ladrón salió del área. Esperando 3 segundos antes de patrullar.");
        }
    }

  
    private IEnumerator EsperarAntesDePatrullar()
    {
        yield return new WaitForSeconds(10f); // Espera 3 segundos
        agentePolicia.ResetPath();
        patrullaPolicia.ReanudarPatrulla();
        Debug.Log("Reanudando patrulla después de 3 segundos.");
    }

    private bool TieneLineaDeVision()
    {
        Vector3 origen = agentePolicia.transform.position + Vector3.up * 1.0f; // Posición de los ojos del policía
        Vector3 direccion = (ladron.position - agentePolicia.transform.position).normalized;

        float distancia = Vector3.Distance(agentePolicia.transform.position, ladron.position);

        // 1️⃣ Calcula el ángulo entre la mirada del policía y el ladrón
        float angulo = Vector3.Angle(agentePolicia.transform.forward, direccion);

        // Si el ladrón está dentro del ángulo de 180°
        if (angulo > 90)  // 180° dividido entre 2 = 90°
        {
            return false; // Está fuera del campo de visión
        }

        // 2️⃣ Verificar si hay paredes en medio con un Raycast
        RaycastHit hit;
        if (Physics.Raycast(origen, direccion, out hit, distancia))
        {
            return hit.transform == ladron; // Si el Raycast golpea al ladrón, lo ve
        }

        return false; // Si el Raycast choca con otra cosa, no lo ve
    }

}