using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneadorHTN
{
    private Queue<TareaHTN> tareas = new();

    public void Planificar(Agente agente)
    {
        tareas.Clear();

        if (agente.ladronDetectado)
        {
            tareas.Enqueue(new TareaPerseguir());
        }
        else
        {
            tareas.Enqueue(new TareaPatrullar());
        }
    }

    public IEnumerator EjecutarPlan(Agente agente, MonoBehaviour contexto)
    {
        while (tareas.Count > 0)
        {
            var tarea = tareas.Dequeue();
            if (tarea.EsEjecutable(agente))
                yield return contexto.StartCoroutine(tarea.Ejecutar(agente));
        }
    }
}