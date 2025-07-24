using UnityEngine;

public class ShowSpinEffect : MonoBehaviour
{
    public Vector3 startPositionOffset = new Vector3(-1000f, 0f, 0f); // bay từ trái
    public float appearDuration = 0.5f;
    public AnimationCurve moveCurve;
    public AnimationCurve scaleCurve;

    private Vector3 originalPosition;
    private Vector3 originalScale;

    void Awake()
    {
        originalPosition = transform.localPosition;
        originalScale = transform.localScale;

        // Ẩn ban đầu (đặt ở vị trí lệch + nhỏ lại)
        transform.localPosition = originalPosition + startPositionOffset;
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(AnimateAppear());
    }

    private System.Collections.IEnumerator AnimateAppear()
    {
        float time = 0f;

        while (time < appearDuration)
        {
            float t = time / appearDuration;

            // Áp dụng animation curve nếu có
            float moveT = moveCurve != null ? moveCurve.Evaluate(t) : t;
            float scaleT = scaleCurve != null ? scaleCurve.Evaluate(t) : t;

            transform.localPosition = Vector3.Lerp(originalPosition + startPositionOffset, originalPosition, moveT);
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, scaleT);

            time += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
        transform.localScale = originalScale;
    }
}
