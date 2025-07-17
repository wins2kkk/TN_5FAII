using Fusion;
using UnityEngine;
using System.Collections;

public class PlayerNetworkkẻ : NetworkBehaviour
{
    [Networked] public string PlayerName { get; set; }
    private bool nameSent = false;

    public override void Spawned()
    {
        // Khi object được spawn, gửi tên của local player
        if (Object.HasInputAuthority)
        {
            // Đăng ký event để nhận thông báo khi tên sẵn sàng
            UserInfor.OnDisplayNameReady += OnDisplayNameReady;

            // Kiểm tra xem tên đã sẵn sàng chưa
            if (!string.IsNullOrEmpty(UserInfor.displayNameCached) &&
                UserInfor.displayNameCached != "Người chơi")
            {
                RPC_SendNameToHost(UserInfor.displayNameCached);
                nameSent = true;
            }
            else
            {
                // Nếu chưa sẵn sàng, đợi event hoặc dùng coroutine backup
                StartCoroutine(SendNameWhenReady());
            }
        }
    }

    private void OnDisplayNameReady(string displayName)
    {
        if (Object.HasInputAuthority && !nameSent)
        {
            RPC_SendNameToHost(displayName);
            nameSent = true;
        }
    }

    private System.Collections.IEnumerator SendNameWhenReady()
    {
        // Đợi tối đa 10 giây cho tên từ PlayFab
        float timeout = 10f;
        while (timeout > 0 && !nameSent)
        {
            if (!string.IsNullOrEmpty(UserInfor.displayNameCached) &&
                UserInfor.displayNameCached != "Người chơi")
            {
                RPC_SendNameToHost(UserInfor.displayNameCached);
                nameSent = true;
                break;
            }

            timeout -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }

        // Nếu timeout, gửi tên mặc định
        if (!nameSent)
        {
            RPC_SendNameToHost("Người chơi");
            nameSent = true;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // Hủy đăng ký event khi object bị destroy
        UserInfor.OnDisplayNameReady -= OnDisplayNameReady;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SendNameToHost(string name)
    {
        Debug.Log($"RPC: Nhận tên từ client → {name}");
        PlayerName = name;

        // Cập nhật tên trong FusionLauncher
        if (FusionLauncher.Instance != null)
        {
            FusionLauncher.Instance.SetRemotePlayerName(Object.InputAuthority, name);
        }

        // Nếu đây là server, gửi tên của server cho tất cả client
        if (Object.HasStateAuthority)
        {
            RPC_SendNameToAllClients(Object.InputAuthority, name);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SendNameToAllClients(PlayerRef player, string name)
    {
        Debug.Log($"RPC: Cập nhật tên cho tất cả client - Player {player}: {name}");

        // Cập nhật tên trong FusionLauncher trên tất cả client
        if (FusionLauncher.Instance != null)
        {
            FusionLauncher.Instance.SetRemotePlayerName(player, name);
        }
    }

    // Method public để FusionLauncher có thể gọi
    public void SendServerNameToAllClients(PlayerRef serverPlayer, string serverName)
    {
        if (Object.HasStateAuthority)
        {
            RPC_SendNameToAllClients(serverPlayer, serverName);
        }
    }
}