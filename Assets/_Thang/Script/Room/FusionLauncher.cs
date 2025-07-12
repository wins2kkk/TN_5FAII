using Fusion;
using Fusion.Sockets;
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("UI Elements")]
    public TMP_InputField roomInput;
    public GameObject lobbyPanel;
    public GameObject waitPanel;
    public TMP_Text errorText;
    public TMP_Text statusText; // Text hiển thị trạng thái chờ

    [Header("Tên người chơi trong phòng")]
    public TMP_Text hostNameText;
    public TMP_Text clientNameText;

    [Header("Nút")]
    public Button btnReady;
    public Button btnStartGame;
    public Button btnBack; // Nút Back trong waitPanel
    public Button btnCreateRoom; // Nút tạo phòng
    public Button btnJoinRoom; // Nút vào phòng

    [Header("Room Info")]
    public TMP_Text roomIdText;

    [Header("Player Network Prefab")]
    public GameObject playerNetworkPrefab; // Prefab chứa PlayerNetwork script

    public string gameplaySceneName = "GameScene";
    public static FusionLauncher Instance { get; private set; }

    private NetworkRunner runner;
    private Dictionary<PlayerRef, string> playerNames = new Dictionary<PlayerRef, string>();
    private Dictionary<PlayerRef, bool> playerReadyStates = new Dictionary<PlayerRef, bool>();

    private bool isReady = false;
    private bool isConnecting = false;
    private HashSet<string> existingRooms = new HashSet<string>(); // Lưu danh sách phòng đã tồn tại
    private string currentRoomName = ""; // Lưu tên phòng hiện tại
    private bool isHost = false; // Kiểm tra có phải host không

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);

        // Thiết lập sự kiện cho nút Back
        if (btnBack != null)
            btnBack.onClick.AddListener(OnBackClicked);

        // Reset scene về trạng thái ban đầu
        ResetToInitialState();
    }

    public void CreateRoom()
    {
        string roomName = roomInput.text.Trim();

        if (string.IsNullOrEmpty(roomName) || roomName.Length < 3)
        {
            ShowError("ID phòng phải có ít nhất 3 ký tự.", true);
            return;
        }

        if (isConnecting)
        {
            ShowError("Đang kết nối, vui lòng đợi...", true);
            return;
        }

        currentRoomName = roomName;
        isHost = true;
        StartGame(GameMode.Host, roomName);
    }

    public void JoinRoom()
    {
        string roomName = roomInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            ShowError("Vui lòng nhập ID phòng.", true);
            return;
        }

        if (isConnecting)
        {
            ShowError("Đang kết nối, vui lòng đợi...", true);
            return;
        }

        currentRoomName = roomName;
        isHost = false;
        StartGame(GameMode.Client, roomName);
    }

    private async void StartGame(GameMode mode, string roomName)
    {
        isConnecting = true;
        errorText.gameObject.SetActive(false);

        // Vô hiệu hóa các nút khi đang kết nối
        SetButtonsInteractable(false);

        try
        {
            var result = await runner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = roomName,
                SceneManager = runner.gameObject.AddComponent<NetworkSceneManagerDefault>(),
                PlayerCount = 2,
                IsVisible = true,
                IsOpen = true
            });

            if (result.Ok)
            {
                // Thêm phòng vào danh sách đã tồn tại nếu là host
                if (mode == GameMode.Host)
                {
                    existingRooms.Add(roomName);
                }

                roomIdText.text = $"ID Phòng: <b>{roomName}</b>";

                string currentPlayerName = UserInfoDisplay.displayNameCached;
                playerNames[runner.LocalPlayer] = currentPlayerName;
                playerReadyStates[runner.LocalPlayer] = false;

                lobbyPanel.SetActive(false);
                waitPanel.SetActive(true);

                btnStartGame.gameObject.SetActive(runner.IsServer);
                btnReady.gameObject.SetActive(!runner.IsServer);

                // Hiển thị trạng thái chờ
                if (runner.IsServer)
                {
                    UpdateStatusText("Chờ người chơi khác vào phòng...");
                }
                else
                {
                    UpdateStatusText("Đã vào phòng thành công!");
                }

                UpdatePlayerNameUI();
            }
            else
            {
                HandleConnectionError(result.ShutdownReason);
            }
        }
        catch (Exception e)
        {
            ShowError("Lỗi kết nối: " + e.Message, true);
        }
        finally
        {
            // Nếu có lỗi kết nối, reset trạng thái
            if (!runner.IsRunning)
            {
                isConnecting = false;
                SetButtonsInteractable(true);

                // Reset trạng thái về ban đầu
                ResetToInitialState();
            }
        }
    }

    private void HandleConnectionError(ShutdownReason reason)
    {
        string message = reason switch
        {
            ShutdownReason.GameNotFound => "ID phòng không tồn tại!",
            ShutdownReason.GameIsFull => "Phòng đã đầy!",
            ShutdownReason.GameClosed => "Phòng đã đóng!",
            ShutdownReason.ConnectionTimeout => "Kết nối timeout!",
            _ => $"Lỗi kết nối: {reason}"
        };

        ShowError(message, true);

        // Reset trạng thái sau khi có lỗi
        StartCoroutine(ResetAfterError());
    }

    private IEnumerator ResetAfterError()
    {
        // Đợi một chút để người dùng đọc thông báo lỗi
        yield return new WaitForSeconds(0.5f);

        // Reset về trạng thái ban đầu
        ResetToInitialState();
    }

    // Method để nhận tên từ PlayerNetwork
    public void SetRemotePlayerName(PlayerRef player, string name)
    {
        Debug.Log($"Đặt tên cho player {player}: {name}");
        playerNames[player] = name;
        UpdatePlayerNameUI();
    }

    private void UpdatePlayerNameUI()
    {
        hostNameText.text = "";
        clientNameText.text = "";

        PlayerRef hostPlayer = PlayerRef.None;
        PlayerRef clientPlayer = PlayerRef.None;

        // Tìm host và client
        foreach (var kvp in playerNames)
        {
            if (runner.IsServer)
            {
                if (kvp.Key == runner.LocalPlayer)
                {
                    hostPlayer = kvp.Key;
                }
                else
                {
                    clientPlayer = kvp.Key;
                }
            }
            else
            {
                if (kvp.Key == runner.LocalPlayer)
                {
                    clientPlayer = kvp.Key;
                }
                else
                {
                    hostPlayer = kvp.Key;
                }
            }
        }

        // Cập nhật UI
        if (hostPlayer != PlayerRef.None && playerNames.TryGetValue(hostPlayer, out string hostName))
        {
            hostNameText.text = "Chủ phòng: " + hostName;
        }

        if (clientPlayer != PlayerRef.None && playerNames.TryGetValue(clientPlayer, out string clientName))
        {
            clientNameText.text = "Người chơi: " + clientName;
        }

        UpdateStartButtonInteractable();
    }

    //private PlayerRef GetOtherPlayer()
    //{
    //    foreach (var kvp in playerNames)
    //    {
    //        if (kvp.Key != runner.LocalPlayer)
    //            return kvp.Key;
    //    }
    //    return PlayerRef.None;
    //}

    private void UpdateStartButtonInteractable()
    {
        if (!runner.IsServer) return;

        bool allReady = true;
        int playerCount = 0;

        foreach (var kvp in playerNames)
        {
            playerCount++;
            if (!playerReadyStates.ContainsKey(kvp.Key) || !playerReadyStates[kvp.Key])
                allReady = false;
        }

        bool canStart = playerCount >= 2 && allReady;
        btnStartGame.interactable = canStart;

        // Cập nhật text trạng thái
        if (runner.IsServer)
        {
            if (playerCount < 2)
            {
                UpdateStatusText("Chờ người chơi khác vào phòng...");
            }
            else if (!allReady)
            {
                UpdateStatusText("Chờ người chơi sẵn sàng...");
            }
            else
            {
                UpdateStatusText("Có thể bắt đầu trò chơi!");
            }
        }
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ShowError(string message, bool autoHide = false)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        roomInput.text = "";
        Debug.LogError(message);

        if (autoHide)
        {
            StartCoroutine(HideErrorAfterDelay(3f));
        }
    }

    private IEnumerator HideErrorAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    //private void ResetButtons()
    //{
    //    SetButtonsInteractable(true);
    //    isConnecting = false;
    //    isReady = false;

    //    if (btnReady != null)
    //    {
    //        btnReady.interactable = true;
    //        btnReady.GetComponentInChildren<TMP_Text>().text = "Sẵn sàng";
    //    }
    //}

    private void ResetToInitialState()
    {
        // Reset tất cả trạng thái về ban đầu
        isConnecting = false;
        isReady = false;
        isHost = false;
        currentRoomName = "";

        // Clear dữ liệu
        playerNames.Clear();
        playerReadyStates.Clear();
        existingRooms.Clear();

        // Reset UI
        lobbyPanel.SetActive(true);
        waitPanel.SetActive(false);

        if (roomInput != null)
            roomInput.text = "";

        if (errorText != null)
            errorText.gameObject.SetActive(false);

        if (hostNameText != null)
            hostNameText.text = "";

        if (clientNameText != null)
            clientNameText.text = "";

        if (roomIdText != null)
            roomIdText.text = "";

        if (statusText != null)
            statusText.text = "";

        // Reset buttons
        SetButtonsInteractable(true);

        if (btnReady != null)
        {
            btnReady.interactable = true;
            btnReady.GetComponentInChildren<TMP_Text>().text = "Sẵn sàng";
        }

        if (btnStartGame != null)
        {
            btnStartGame.interactable = false;
        }

        Debug.Log("Đã reset scene về trạng thái ban đầu");
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (btnCreateRoom != null) btnCreateRoom.interactable = interactable;
        if (btnJoinRoom != null) btnJoinRoom.interactable = interactable;
    }

    public void OnBackClicked()
    {
        // Shutdown runner nếu đang chạy
        if (runner != null && runner.IsRunning)
        {
            runner.Shutdown();
        }

        // Xóa phòng đã tạo nếu là host
        if (isHost && !string.IsNullOrEmpty(currentRoomName))
        {
            existingRooms.Remove(currentRoomName);
            Debug.Log($"Đã xóa phòng: {currentRoomName}");
        }

        // Reset toàn bộ scene về trạng thái ban đầu
        ResetToInitialState();

        Debug.Log("Đã quay lại lobby và reset scene");
    }

    public void OnReadyClicked()
    {
        isReady = true;
        btnReady.interactable = false;
        btnReady.GetComponentInChildren<TMP_Text>().text = "Đã sẵn sàng";

        playerReadyStates[runner.LocalPlayer] = true;
        UpdateStartButtonInteractable();
    }

    public void OnStartGameClicked()
    {
        int playerCount = playerNames.Count;
        if (playerCount < 2)
        {
            ShowError("Cần ít nhất 2 người chơi!", true);
            return;
        }

        bool allReady = true;
        foreach (var kvp in playerReadyStates)
        {
            if (kvp.Key != runner.LocalPlayer && !kvp.Value)
            {
                allReady = false;
                break;
            }
        }

        if (!allReady)
        {
            ShowError("Người chơi khác chưa sẵn sàng!", true);
            return;
        }

        LoadGameScene();
    }

    public void LoadGameScene()
    {
        if (runner.IsServer)
        {
            runner.LoadScene(gameplaySceneName);
        }
    }

    // ======================== Fusion Callbacks ========================
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Người chơi vào phòng: {player} (Local: {runner.LocalPlayer})");

        if (!playerNames.ContainsKey(player))
        {
            if (player == runner.LocalPlayer)
            {
                playerNames[player] = UserInfoDisplay.displayNameCached;
            }
            else
            {
                playerNames[player] = "Đang tải..."; // Placeholder cho đến khi nhận được tên thật
            }
        }

        if (!playerReadyStates.ContainsKey(player))
        {
            playerReadyStates[player] = false;
        }

        // Spawn PlayerNetwork object để đồng bộ tên
        if (runner.IsServer && playerNetworkPrefab != null)
        {
            var playerNetworkObj = runner.Spawn(playerNetworkPrefab, Vector3.zero, Quaternion.identity, player);
            Debug.Log($"Spawned PlayerNetwork for {player}");

            // Nếu có client mới join, server gửi tên của mình cho client đó
            if (player != runner.LocalPlayer)
            {
                StartCoroutine(SendServerNameToNewClient(playerNetworkObj));
            }
        }

        UpdatePlayerNameUI();
    }

    private System.Collections.IEnumerator SendServerNameToNewClient(NetworkObject playerNetworkObj)
    {
        // Đợi một chút để đảm bảo PlayerNetwork đã sẵn sàng
        yield return new WaitForSeconds(0.5f);

        var playerNetwork = playerNetworkObj.GetComponent<PlayerNetwork>();
        if (playerNetwork != null)
        {
            // Gửi tên server cho client mới
            string serverName = UserInfoDisplay.displayNameCached;
            if (string.IsNullOrEmpty(serverName) || serverName == "Người chơi")
            {
                serverName = "Chủ phòng";
            }

            playerNetwork.SendServerNameToAllClients(runner.LocalPlayer, serverName);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (playerNames.ContainsKey(player)) playerNames.Remove(player);
        if (playerReadyStates.ContainsKey(player)) playerReadyStates.Remove(player);
        UpdatePlayerNameUI();
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("Đã kết nối đến Fusion server");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner)
    {
        ShowError("Mất kết nối đến server!", true);

        // Delay một chút rồi reset
        StartCoroutine(DelayedReset());
    }

    private IEnumerator DelayedReset()
    {
        yield return new WaitForSeconds(1f);
        ResetToInitialState();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        ShowError($"Kết nối thất bại: {reason}", true);

        // Reset sau khi có lỗi kết nối
        StartCoroutine(ResetAfterError());
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"Network shutdown: {shutdownReason}");

        // Reset trạng thái
        isConnecting = false;

        // Nếu không phải do người dùng bấm back mà bị shutdown
        if (shutdownReason != ShutdownReason.Ok)
        {
            ResetToInitialState();
        }
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        ShowError($"Mất kết nối: {reason}", true);

        // Delay một chút rồi reset
        StartCoroutine(DelayedReset());
    }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
        throw new NotImplementedException();
    }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        throw new NotImplementedException();
    }
}