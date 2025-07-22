using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class RoomManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static RoomManager Instance;

    [Header("UI References")]
    public TMP_InputField joinRoomInput;
    public Button createRoomBtn, joinRoomBtn;
    public TMP_Text roomIDText;
    public TMP_Text playerNameText;
    public Button readyBtn, leaveBtn;
    public GameObject roomUI;
    public NetworkObject playerStatePrefab;

    private NetworkRunner runner;
    private string roomID;
    private Dictionary<PlayerRef, PlayerNetworkState> playerStates = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        createRoomBtn.onClick.AddListener(CreateRoom);
        joinRoomBtn.onClick.AddListener(() => JoinRoom(joinRoomInput.text));
        readyBtn.onClick.AddListener(SetReady);
        leaveBtn.onClick.AddListener(LeaveRoom);
    }

    public void CreateRoom()
    {
        roomID = Random.Range(100000, 999999).ToString();
        StartGame(roomID, GameMode.Host);
    }

    public void JoinRoom(string inputRoomID)
    {
        if (string.IsNullOrEmpty(inputRoomID) || inputRoomID.Length < 6)
        {
            Debug.LogWarning("ID phòng không hợp lệ!");
            return;
        }

        roomID = inputRoomID;
        StartGame(roomID, GameMode.Client);
    }

    private async void StartGame(string room, GameMode mode)
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = room,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void SetReady()
    {
        if (!playerStates.TryGetValue(runner.LocalPlayer, out var myState)) return;

        myState.IsReady = true;

        if (runner.IsServer)
        {
            foreach (var kvp in playerStates)
            {
                if (kvp.Key == runner.LocalPlayer) continue; // bỏ qua host
                if (!kvp.Value.IsReady)
                {
                    Debug.Log("Chưa đủ người chơi sẵn sàng.");
                    return;
                }
            }

            Debug.Log("Tất cả đã sẵn sàng. Host bắt đầu game!");
            SceneManager.LoadScene("Testonl");

        }
        else
        {
            Debug.Log("Bạn đã sẵn sàng. Chờ host bắt đầu.");
        }
    }

    public void LeaveRoom()
    {
        if (runner != null)
        {
            runner.Shutdown();
            SceneManager.LoadScene("Thanh_Pho2");
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Người chơi mới: {player}");

        NetworkObject obj = runner.Spawn(
            playerStatePrefab,
            Vector3.zero,
            Quaternion.identity,
            player
        );

        PlayerNetworkState state = obj.GetComponent<PlayerNetworkState>();
        playerStates[player] = state;
    }
    //public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken token) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }


}
