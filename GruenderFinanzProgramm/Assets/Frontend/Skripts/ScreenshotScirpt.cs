using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenshotHelper : MonoBehaviour
{
    [SerializeField] private string prefix = "Tutorial";
    private int zaehler = 1;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject); // ← bleibt über Scenenwechsel erhalten
    }

    private void Update()
    {
        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            string ordner = $"{Application.dataPath}/Frontend/Bilder/TutorialBilder";

            if (!System.IO.Directory.Exists(ordner))
                System.IO.Directory.CreateDirectory(ordner);

            string pfad = $"{Application.dataPath}/Frontend/Bilder/TutorialBilder/{prefix}_{zaehler:D2}.png";
            ScreenCapture.CaptureScreenshot(pfad);
            Debug.Log($"Screenshot gespeichert: {pfad}");
            zaehler++;
        }
    }
}