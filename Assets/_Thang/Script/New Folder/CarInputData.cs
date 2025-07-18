using Fusion;

public struct CarInputData : INetworkInput
{
    public float vertical;
    public float horizontal;
    public NetworkBool isHandbraking;
    public float verticall;
    public float horizontall;
    public bool brake;
    public bool boost;
    // Bạn có thể thêm các input khác nếu cần
    // public NetworkBool isBoostPressed; // Nếu muốn sync boost qua network
    // public NetworkBool isFlipping; // Nếu muốn sync flip qua network
}