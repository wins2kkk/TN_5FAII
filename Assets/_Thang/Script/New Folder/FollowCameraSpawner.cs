using Fusion;
using UnityEngine;

public class FollowCameraSpawner : NetworkBehaviour
{
    public GameObject cameraPrefab;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            GameObject cam = Instantiate(cameraPrefab);
            var follow = cam.GetComponent<CameraFollowPlayer>();
            follow.target = this.transform; // Camera ch? follow player local
        }
    }
}
