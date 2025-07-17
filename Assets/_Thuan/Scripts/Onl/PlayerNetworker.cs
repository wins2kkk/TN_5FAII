using Fusion;
using UnityEngine;

public class PlayerNetworker : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }
    [Networked] public bool IsReady { get; set; }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    public void RPC_SetReady()
    {
        IsReady = true;
    }
}