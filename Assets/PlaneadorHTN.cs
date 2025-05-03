using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneadorHTN
{
    public Queue<TareaHTN> tareas = new();

    public void Planificar(Policia policia)
    {
        tareas.Clear();
        Debug.Log($"[HTN] Planificando para {policia.AgentId}. thiefDetected={policia.ladronViendo}, thiefTransform={policia.thiefTransform}");

        if (policia.ladronViendo)
        {
            Debug.Log("[HTN] A�adiendo tarea de PERSEGUIR");
            tareas.Enqueue(new TareaPerseguir());
        }
        if (!policia.ladronViendo && !policia.isSearching)
        {
            Debug.Log("[HTN] A�adiendo tarea de PATRULLAR");
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
