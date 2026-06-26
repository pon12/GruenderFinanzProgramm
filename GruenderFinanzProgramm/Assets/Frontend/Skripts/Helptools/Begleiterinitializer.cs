using UnityEngine;

// Dieses Script in jede Szene als eigenes GameObject legen.
// Die Ganzkörper-Begleiter-Textur im Inspector zuweisen.
public class BegleiterInitializer : MonoBehaviour
{
    [Header("Begleiter-Textur (Ganzkörper)")]
    [SerializeField] private Texture2D begleiterGanzkoerper;

    private void Awake()
    {
        // Übergibt die einzelne Textur an das Tooltip-System
        HelpTooltip.SetzeBegleiterTextur(begleiterGanzkoerper);
    }
}