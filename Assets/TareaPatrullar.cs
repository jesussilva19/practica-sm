using System.Collections;
using UnityEngine;

public class TareaPatrullar : TareaHTN
{
    public override bool EsEjecutable(Policia policia)
    {
        return !policia.LadronVisto;
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        policia.IniciarPatrulla();
        yield return null;
    }
}
