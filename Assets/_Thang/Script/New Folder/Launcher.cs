using Fusion;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Launcher : MonoBehaviour
{
    public NetworkRunner runnerPrefab;
    public InputField roomInputField;

    private NetworkRunner _runnerInstance;

    public void CreateRoom()
    {
        StartGame(GameMode.Host);
    }

    public void JoinRoom()
    {
        StartGame(GameMode.Client);
    }

    private async void StartGame(GameMode mode)
    {
        string roomName = roomInputField.text;

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Room name is empty!");
            return;
        }

        _runnerInstance = Instantiate(runnerPrefab);
        _runnerInstance.name = "NetworkRunner";
        _runnerInstance.ProvideInput = true;

        // Thêm AutoSpawner nếu chưa có
        if (_runnerInstance.GetComponent<AutoSpawner>() == null)
            _runnerInstance.gameObject.AddComponent<AutoSpawner>();

        // Thêm SceneManager nếu chưa có
        if (_runnerInstance.GetComponent<NetworkSceneManagerDefault>() == null)
            _runnerInstance.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runnerInstance.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(1), // Nếu Dua_Onl là scene số 1
            SceneManager = _runnerInstance.GetComponent<NetworkSceneManagerDefault>()
        });


        if (!result.Ok)
        {
            Debug.LogError($"❌ Failed to start: {result.ShutdownReason}");
        }
    }


}
