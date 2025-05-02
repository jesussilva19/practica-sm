using System.Collections;
using UnityEngine;

public class TareaAuction : TareaHTN
{
    private readonly string _auctionId;

    public TareaAuction(string auctionId)
    {
        _auctionId = auctionId;
    }

    public override bool EsEjecutable(Policia policia)
    {
        // Sólo lanzar si aún no existe esa subasta
        return !policia.ActiveAuctions.ContainsKey(_auctionId);
    }

    public override IEnumerator Ejecutar(Policia policia)
    {
        Debug.Log($"[HTN] Lanzando subasta {_auctionId} desde {policia.AgentId}");
        policia.StartAuction(_auctionId);
        yield break;
    }
}
