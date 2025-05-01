using System.Collections;
using UnityEngine;

public class TareaPatrullar : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        return !policia.thiefDetected;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        policia.ActualizarPatrulla();
        yield return null;
    }
}
