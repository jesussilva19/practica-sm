using System.Collections;

public class TareaPatrullar : TareaHTN
{
    public TareaPatrullar()
    {
    }

    public override bool EsEjecutable(Agente agente)
    {
        return agente.destinos.Length > 0;
    }

    public override IEnumerator Ejecutar(Agente agente)
    {
        agente.ReanudarPatrulla();

        while (!agente.ladronDetectado)
        {
            yield return null;
        }

        agente.PausarPatrulla();
    }
}