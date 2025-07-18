using UnityEngine;
using Fusion;

public class InputHandler : NetworkBehaviour, IBeforeUpdate
{
    public void BeforeUpdate()
    {
        if (HasInputAuthority && Runner != null)
        {
            CarInputData data = new CarInputData
            {
                vertical = Input.GetAxis("Vertical"),
                horizontal = Input.GetAxis("Horizontal"),
                isHandbraking = Input.GetKey(KeyCode.LeftShift)
            };

            //Runner.SetInput(data); // Sử dụng SetInput không cần PlayerRef
        }
    }
}
