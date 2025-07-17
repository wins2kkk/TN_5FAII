using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject playerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            Vector3 spawnPos = new Vector3(Random.Range(-5, 5), 1, Random.Range(-5, 5));
            NetworkObject obj = Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
            obj.GetComponent<PlayerNetworker>().PlayerName = $"Người chơi {player.RawEncoded}";
        }
    }
}