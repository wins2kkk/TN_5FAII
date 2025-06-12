using UnityEngine;

public class SimpleMiniMapTarget : MonoBehaviour
{
    public Transform[] targets;

    void Update()
    {
        if (targets == null || targets.Length == 0) return;

        // Tính trung bình vị trí
        Vector3 avgPos = Vector3.zero;
        foreach (var t in targets)
        {
            if (t != null) avgPos += t.position;
        }
        avgPos /= targets.Length;
        transform.position = avgPos;

        // Tính trung bình góc Y (xoay trái/phải)
        float avgY = 0f;
        foreach (var t in targets)
        {
            if (t != null) avgY += t.eulerAngles.y;
        }
        avgY /= targets.Length;

        // Gán xoay chỉ theo Y, giữ X/Z = 0
        transform.rotation = Quaternion.Euler(0f, avgY, 0f);
    }
}
