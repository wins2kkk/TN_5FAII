using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class AutoSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Prefab và vị trí spawn")]
    public GameObject carPrefab;
    public Transform[] spawnPoints;

    // Theo dõi spawn points đã sử dụng
    private Dictionary<PlayerRef, int> playerSpawnIndex = new Dictionary<PlayerRef, int>();
    private int nextSpawnIndex = 0;

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"👤 Player joined: {player}");

        if (runner.IsServer)
        {
            int spawnIndex = nextSpawnIndex;
            if (spawnIndex >= spawnPoints.Length)
            {
                Debug.LogWarning("❌ Không còn spawn point nào khả dụng!");
                spawnIndex = 0;
                nextSpawnIndex = 1;
            }
            else
            {
                nextSpawnIndex++;
            }

            Transform spawnPoint = spawnPoints[spawnIndex];

            // ✅ Gán InputAuthority bằng cách truyền player vào runner.Spawn
            NetworkObject spawnedCar = runner.Spawn(carPrefab, spawnPoint.position, spawnPoint.rotation, player);

            // (Tùy chọn) Gán lại PlayerObject để truy cập player dễ hơn sau này
            runner.SetPlayerObject(player, spawnedCar);

            playerSpawnIndex[player] = spawnIndex;

            Debug.Log($"✅ Spawned player {player} at spawn point {spawnIndex} - Position: {spawnPoint.position}");
        }
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            if (playerSpawnIndex.ContainsKey(player))
            {
                int spawnIndex = playerSpawnIndex[player];
                playerSpawnIndex.Remove(player);
                Debug.Log($"🔄 Player {player} left - freed spawn point {spawnIndex}");
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // ✅ Tạo input data với tất cả controls
        CarInputData data = new CarInputData
        {
            vertical = Input.GetAxisRaw("Vertical"),
            horizontal = Input.GetAxis("Horizontal"),
            isHandbraking = Input.GetKey(KeyCode.Space)
        };

        input.Set(data);
    }

    // --- Các callback cần thiết ---
    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("✅ Connected to server");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"❌ Connection failed: {reason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.LogWarning($"⚠️ Disconnected from server: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"🔄 Network shutdown: {shutdownReason}");
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("✅ Scene loaded");
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("🔄 Scene loading...");
    }

    // --- Không dùng (nhưng bắt buộc phải có để interface hợp lệ) ---
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}
