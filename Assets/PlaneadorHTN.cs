using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneadorHTN
{
    private Queue<TareaHTN> tareas = new();

    public void Planificar(Policia policia)
    {
        tareas.Clear();
        Debug.Log($"[HTN] Planificando para {policia.AgentId}. thiefDetected={policia.thiefDetected}, thiefTransform={policia.thiefTransform}");

        if (policia.thiefDetected && policia.thiefTransform != null)
        {
            Debug.Log("[HTN] Añadiendo tarea de PERSEGUIR");
            tareas.Enqueue(new TareaPerseguir());
        }
        else
        {
            Debug.Log("[HTN] Añadiendo tarea de PATRULLAR");
            tareas.Enqueue(new TareaPatrullar());
        }
    }


    public IEnumerator EjecutarPlan(Policia policia, MonoBehaviour contexto)
    {
        while (tareas.Count > 0)
        {
            var tarea = tareas.Dequeue();
            if (tarea.EsEjecutable(policia))
            {
                yield return contexto.StartCoroutine(tarea.Ejecutar(policia));
            }
        }
    }
}
