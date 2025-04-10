using System.Collections;

public abstract class TareaHTN
{
    public abstract bool EsEjecutable(Agente agente);
    public abstract IEnumerator Ejecutar(Agente agente);
}
