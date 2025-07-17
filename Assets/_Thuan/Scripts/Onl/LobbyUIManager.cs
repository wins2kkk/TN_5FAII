using Fusion;
using Fusion.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LobbyUIManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyUIManager Instance;

    [Header("UI Panels")]
    public GameObject createJoinPanel;
    public GameObject lobbyPanel;

    [Header("Room UI")]
    public TMP_Text roomIdText;
    public TMP_InputField roomInput;
    public Button createBtn;
    public Button joinBtn;
    public Button startBtn;
    public Transform playerListParent;
    public GameObject playerSlotPrefab;

    private NetworkRunner runner;
    private Dictionary<PlayerRef, GameObject> playerSlots = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        createBtn.onClick.AddListener(() => CreateRoom());
        joinBtn.onClick.AddListener(() => JoinRoom());
        startBtn.onClick.AddListener(() => StartGame());
    }

    void CreateRoom()
    {
        string roomID = Random.Range(100000, 999999).ToString();
        StartCoroutine(LaunchGame(roomID));
    }

    void JoinRoom()
    {
        string roomID = roomInput.text.Trim();
        StartCoroutine(LaunchGame(roomID));
    }

    IEnumerator LaunchGame(string roomID)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Sử dụng Task để chờ kết quả
        var startGameTask = runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = roomID,
           // Scene = SceneManager.GetActiveScene().buildIndex,
            SceneManager = sceneManager
        });

        // Chờ task hoàn thành
        yield return new WaitUntil(() => startGameTask.IsCompleted);

        var result = startGameTask.Result;

        if (!result.Ok || result.ShutdownReason == ShutdownReason.GameNotFound)
        {
            Debug.LogError("❌ Không thể vào phòng: " + result.ShutdownReason);
            createJoinPanel.SetActive(true);
            yield break;
        }

        Debug.Log("✅ Vào phòng thành công: " + roomID);
        roomIdText.text = "ID Phòng: " + roomID;
        createJoinPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void StartGame()
    {
        if (runner.IsSharedModeMasterClient)
        {
            //runner.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // chuyển sang scene game
        }
    }

    public void Ready(PlayerRef player)
    {
        Debug.Log($"{player} đã sẵn sàng!");
        // Có thể xử lý gửi ready status qua RPC nếu cần
    }

    void RefreshPlayerList()
    {
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        foreach (var player in runner.ActivePlayers)
        {
            GameObject slot = Instantiate(playerSlotPrefab, playerListParent);
            slot.GetComponentInChildren<TMP_Text>().text = player == runner.LocalPlayer ? "Bạn (You)" : $"Người chơi {player.PlayerId}";
            if (player != runner.LocalPlayer)
                slot.GetComponentInChildren<Button>().interactable = false;

            playerSlots[player] = slot;
        }

        startBtn.interactable = runner.IsSharedModeMasterClient;
    }

    // ========= INetworkRunnerCallbacks =========
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        RefreshPlayerList();
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (playerSlots.TryGetValue(player, out var go))
        {
            Destroy(go);
            playerSlots.Remove(player);
        }

        RefreshPlayerList();
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) => request.Accept();
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    // Thêm các callback methods còn thiếu
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey reliableKey, float progress) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}