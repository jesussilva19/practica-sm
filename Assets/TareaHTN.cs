using System.Collections;

public abstract class TareaHTN
{
    public abstract bool EsEjecutable(Policia policia);
    public abstract IEnumerator Ejecutar(Policia policia);
}
