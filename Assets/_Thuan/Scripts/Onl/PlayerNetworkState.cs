using Fusion;

public class PlayerNetworkState : NetworkBehaviour
{
    [Networked] public bool IsReady { get; set; }
}
