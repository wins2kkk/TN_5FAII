using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AutoSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkObject carPrefab;
    public Transform[] spawnPoints; // [0] = vị trí cho người join, [1] = vị trí cho host
    private Dictionary<PlayerRef, NetworkObject> spawnedCars = new();

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        int spawnIndex;
        string playerType;

        // 🎯 Kiểm tra số lượng player hiện tại
        int currentPlayerCount = runner.ActivePlayers.Count();

        if (currentPlayerCount == 1)
        {
            // 👑 Player đầu tiên = Host → spawn ở vị trí 1 (index 1)
            spawnIndex = 1;
            playerType = "Host";
        }
        else if (currentPlayerCount == 2)
        {
            // 🤝 Player thứ hai = Guest → spawn ở vị trí 0 (index 0) 
            spawnIndex = 0;
            playerType = "Guest";
        }
        else
        {
            Debug.LogWarning($"⚠️ Game chỉ hỗ trợ 2 người chơi! Player {player} không thể join.");
            return;
        }

        // ✅ Spawn xe
        Transform spawnPoint = spawnPoints[spawnIndex];
        NetworkObject car = runner.Spawn(carPrefab, spawnPoint.position, spawnPoint.rotation, player);
        spawnedCars[player] = car;

        Debug.Log($"✅ Spawned {playerType} {player} at spawn point {spawnIndex}: {spawnPoint.name}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer && spawnedCars.ContainsKey(player))
        {
            runner.Despawn(spawnedCars[player]);
            spawnedCars.Remove(player);
            Debug.Log($"🔄 Player {player} left and despawned");
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        CarInputData data = new CarInputData
        {
            vertical = Input.GetAxis("Vertical"),
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
        spawnedCars.Clear();
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