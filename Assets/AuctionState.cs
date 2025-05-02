using System.Collections.Generic;
using UnityEngine;

public class AuctionState
{
    public string AuctionId;
    public Vector3 Target;
    public float StartTime;
    public Dictionary<string, float> Proposals = new Dictionary<string, float>();
}
