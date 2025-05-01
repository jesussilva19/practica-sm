using System.Collections;
using UnityEngine;

public class TareaPatrullar : TareaHTN
{
    public override bool EsEjecutable(Agente agente)
    {
        return !agente.ladronDetectado;
    }

    public override IEnumerator Ejecutar(Agente agente)
    {
        agente.ReanudarPatrulla();
        yield return null;
    }
}
