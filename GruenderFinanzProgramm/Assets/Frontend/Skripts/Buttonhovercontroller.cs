using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

public static class ButtonHoverController
{
    private static readonly Color RandGruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color GruenHell = new Color(160f / 255f, 220f / 255f, 175f / 255f);
    private static readonly Color RotHell = new Color(240f / 255f, 100f / 255f, 110f / 255f);

    private static readonly string[] BereitsPerCssGestyltePraefixe =
    {
        "doc-btn-primary",
        "doc-btn-secondary",
        "doc-btn-danger-outline",
        "doc-btn-add-inline",
        "btn-add-global",
        "btn-edit-pen",
        "btn-minus-delete",
        "btn-grey",
        "btn-orange",
        "btn-red",
        "btn-steuer",
        "btn-modus",
        "btn-danger-pop-up",
        "btn-danger"
    };

    private static bool HatEigenesCssHover(Button btn)
        => BereitsPerCssGestyltePraefixe.Any(klasse => btn.ClassListContains(klasse));

    // Für Screens mit CSS-Hover-Klassen
    public static void Registriere(VisualElement root)
    {
        root.Query<Button>().ForEach(btn =>
        {
            if (btn.ClassListContains("btn-hover-outline")) RegistriereRahmen(btn);
            else if (btn.ClassListContains("btn-hover-green")) RegistriereHintergrund(btn, GruenHell);
            else if (btn.ClassListContains("btn-hover-darkgreen")) RegistriereHintergrund(btn, GruenHell);
            else if (btn.ClassListContains("btn-hover-red")) RegistriereHintergrund(btn, RotHell);
        });
    }

    // Für Screens ohne CSS-Klassen: automatische Farberkennung
    public static void RegistriereAlle(VisualElement root)
    {
        root.Query<Button>().ForEach(btn =>
        {
            if (HatEigenesCssHover(btn)) return; // Diese Buttons stylen sich per USS selbst

            btn.schedule.Execute(() =>
            {
                Color bg = btn.resolvedStyle.backgroundColor;

                if (IstRot(bg))
                    RegistriereHintergrund(btn, RotHell);
                else if (IstGruen(bg))
                    RegistriereHintergrund(btn, GruenHell);
                else
                    RegistriereRahmen(btn);
            });
        });

        // Toggles: grüner Rahmen auf das Checkbox-Element
        root.Query<Toggle>().ForEach(toggle =>
        {
            var checkmark = toggle.Q<VisualElement>(className: "unity-toggle__input");
            if (checkmark == null) return;

            toggle.RegisterCallback<MouseEnterEvent>(_ =>
            {
                checkmark.style.borderTopColor = RandGruen;
                checkmark.style.borderRightColor = RandGruen;
                checkmark.style.borderBottomColor = RandGruen;
                checkmark.style.borderLeftColor = RandGruen;
                checkmark.style.borderTopWidth = 2;
                checkmark.style.borderRightWidth = 2;
                checkmark.style.borderBottomWidth = 2;
                checkmark.style.borderLeftWidth = 2;
            });

            toggle.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                checkmark.style.borderTopColor = StyleKeyword.Null;
                checkmark.style.borderRightColor = StyleKeyword.Null;
                checkmark.style.borderBottomColor = StyleKeyword.Null;
                checkmark.style.borderLeftColor = StyleKeyword.Null;
                checkmark.style.borderTopWidth = StyleKeyword.Null;
                checkmark.style.borderRightWidth = StyleKeyword.Null;
                checkmark.style.borderBottomWidth = StyleKeyword.Null;
                checkmark.style.borderLeftWidth = StyleKeyword.Null;
            });
        });
    }

    // Grauer Rahmen-Hover: grüner Rand, kein Hintergrundwechsel,
    // Rahmenbreite ist dauerhaft reserviert (transparent), damit sich
    // beim Hover nur die Farbe ändert und nicht die Layoutgröße.
    private static void RegistriereRahmen(Button btn)
    {
        btn.style.borderTopWidth = 2;
        btn.style.borderRightWidth = 2;
        btn.style.borderBottomWidth = 2;
        btn.style.borderLeftWidth = 2;
        btn.style.borderTopColor = Color.clear;
        btn.style.borderRightColor = Color.clear;
        btn.style.borderBottomColor = Color.clear;
        btn.style.borderLeftColor = Color.clear;

        btn.RegisterCallback<MouseEnterEvent>(_ =>
        {
            btn.style.borderTopColor = RandGruen;
            btn.style.borderRightColor = RandGruen;
            btn.style.borderBottomColor = RandGruen;
            btn.style.borderLeftColor = RandGruen;
        });

        btn.RegisterCallback<MouseLeaveEvent>(_ =>
        {
            btn.style.borderTopColor = Color.clear;
            btn.style.borderRightColor = Color.clear;
            btn.style.borderBottomColor = Color.clear;
            btn.style.borderLeftColor = Color.clear;
        });
    }

    // Farbiger Hover: Hintergrund heller
    private static void RegistriereHintergrund(Button btn, Color hoverFarbe)
    {
        btn.RegisterCallback<MouseEnterEvent>(_ =>
        {
            btn.style.backgroundColor = hoverFarbe;
        });

        btn.RegisterCallback<MouseLeaveEvent>(_ =>
        {
            btn.style.backgroundColor = StyleKeyword.Null;
        });
    }

    private static bool IstRot(Color c)
        => c.r > 0.55f && c.g < 0.35f && c.b < 0.35f;

    private static bool IstGruen(Color c)
        => c.g > 0.6f && c.r < 0.6f;
}