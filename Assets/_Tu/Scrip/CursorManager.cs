using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [Header("Custom Cursor Settings")]
    [SerializeField] private Texture2D cursorTexture;
    [SerializeField] private AudioClip clickSound; // Kéo sound vào đây

    private Vector2 cursorHotspot;
    private float timer = 0f;
    private bool isCursorVisible = true;
    //private float hideDelay = 3f;

    private AudioSource audioSource;

    void Start()
    {
        cursorHotspot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
        Cursor.SetCursor(cursorTexture, cursorHotspot, CursorMode.Auto);
        Cursor.visible = true;

        // Tạo AudioSource nếu chưa có
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // Nếu người dùng bấm chuột
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
        {
            ShowCursor();
            timer = 0f;

            // Phát âm thanh click nếu có gán
            if (clickSound != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }
        //else
        //{
        //    timer += Time.deltaTime;

        //    if (timer >= hideDelay && isCursorVisible)
        //    {
        //        HideCursor();
        //    }
        //}
    }

    void ShowCursor()
    {
        Cursor.visible = true;
        isCursorVisible = true;
    }

    void HideCursor()
    {
        Cursor.visible = false;
        isCursorVisible = false;
    }
}
