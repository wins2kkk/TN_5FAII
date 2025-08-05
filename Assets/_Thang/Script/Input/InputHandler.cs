using UnityEngine;

public class InputHandler : MonoBehaviour
{
    public CarInputData currentInput;

    void Update()
    {
        currentInput.vertical = Input.GetAxis("Vertical");
        currentInput.horizontal = Input.GetAxis("Horizontal");
        currentInput.isHandbraking = Input.GetKey(KeyCode.LeftShift);

        currentInput.verticall = Input.GetAxis("Vertical");
        currentInput.horizontall = Input.GetAxis("Horizontal");
        currentInput.brake = Input.GetKey(KeyCode.Space);
        currentInput.boost = Input.GetKey(KeyCode.LeftControl);
    }

    public CarInputData GetInputData()
    {
        return currentInput;
    }
}
