using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneadorHTN
{
    private Queue<TareaHTN> tareas = new();

    public void Planificar(Policia policia)
    {
        tareas.Clear();

        if (policia.LadronVisto)
        {
            tareas.Enqueue(new TareaPerseguir());
        }
        else
        {
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
