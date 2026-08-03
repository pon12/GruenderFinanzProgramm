using UnityEngine;

public class Schließen : MonoBehaviour
{
    public void AppSchliessen()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
