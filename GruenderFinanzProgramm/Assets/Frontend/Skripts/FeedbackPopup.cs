using UnityEngine;
using UnityEngine.UIElements;

public enum FeedbackTyp
{
    Erfolg,
    Fehler
}

public static class FeedbackPopup
{
    private static readonly Color Gruen = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color Rot = new Color(230f / 255f, 57f / 255f, 70f / 255f);
    private static readonly Color KartenFarbe = new Color(38f / 255f, 38f / 255f, 38f / 255f);

    public static void Show(VisualElement root, string nachricht, FeedbackTyp typ = FeedbackTyp.Erfolg, float dauerSekunden = 2.5f)
    {
        if (root == null) return;

        Color akzent = typ == FeedbackTyp.Erfolg ? Gruen : Rot;

        var overlay = new VisualElement();
        overlay.style.position = Position.Absolute;
        overlay.style.left = 0;
        overlay.style.right = 0;
        overlay.style.top = 0;
        overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
        overlay.style.alignItems = Align.Center;
        overlay.style.justifyContent = Justify.Center;

        var karte = new VisualElement();
        karte.style.width = 460;
        karte.style.minHeight = 210;
        karte.style.backgroundColor = KartenFarbe;
        karte.style.borderTopWidth = 2;
        karte.style.borderRightWidth = 2;
        karte.style.borderBottomWidth = 2;
        karte.style.borderLeftWidth = 2;
        karte.style.borderTopColor = akzent;
        karte.style.borderRightColor = akzent;
        karte.style.borderBottomColor = akzent;
        karte.style.borderLeftColor = akzent;
        karte.style.borderTopLeftRadius = 14;
        karte.style.borderTopRightRadius = 14;
        karte.style.borderBottomLeftRadius = 14;
        karte.style.borderBottomRightRadius = 14;
        karte.style.alignItems = Align.Center;
        karte.style.paddingTop = 18;
        karte.style.paddingBottom = 28;
        karte.style.paddingLeft = 24;
        karte.style.paddingRight = 24;

        var schliessen = new Button(() => Schliessen(overlay)) { text = "\u2715" };
        schliessen.style.position = Position.Absolute;
        schliessen.style.top = 8;
        schliessen.style.right = 10;
        schliessen.style.width = 28;
        schliessen.style.height = 28;
        schliessen.style.backgroundColor = Color.clear;
        schliessen.style.color = new Color(0.7f, 0.7f, 0.7f);
        schliessen.style.fontSize = 14;
        schliessen.style.borderTopWidth = 0;
        schliessen.style.borderRightWidth = 0;
        schliessen.style.borderBottomWidth = 0;
        schliessen.style.borderLeftWidth = 0;
        schliessen.RegisterCallback<MouseEnterEvent>(_ => schliessen.style.color = Color.white);
        schliessen.RegisterCallback<MouseLeaveEvent>(_ => schliessen.style.color = new Color(0.7f, 0.7f, 0.7f));

        var symbol = ErstelleSymbol(typ, akzent);

        var text = new Label(nachricht);
        text.style.color = Color.white;
        text.style.fontSize = 18;
        text.style.unityTextAlign = TextAnchor.MiddleCenter;
        text.style.whiteSpace = WhiteSpace.Normal;
        text.style.marginTop = 12;

        karte.Add(schliessen);
        karte.Add(symbol);
        karte.Add(text);

        karte.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        overlay.RegisterCallback<ClickEvent>(_ => Schliessen(overlay));

        overlay.Add(karte);
        root.Add(overlay);

        overlay.schedule.Execute(() => Schliessen(overlay)).StartingIn((long)(dauerSekunden * 1000f));
    }

    private static void Schliessen(VisualElement overlay)
    {
        if (overlay.parent != null) overlay.RemoveFromHierarchy();
    }

    private static VisualElement ErstelleSymbol(FeedbackTyp typ, Color akzent)
    {
        var container = new VisualElement();
        container.style.width = 80;
        container.style.height = 80;
        container.style.marginTop = 14;

        if (typ == FeedbackTyp.Erfolg)
        {
            container.Add(ErstelleBalken(akzent, 28, 7, 10, 46, 45f));
            container.Add(ErstelleBalken(akzent, 50, 7, 24, 38, -50f));
        }
        else
        {
            container.Add(ErstelleBalken(akzent, 54, 7, 13, 37, 45f));
            container.Add(ErstelleBalken(akzent, 54, 7, 13, 37, -45f));
        }
        return container;
    }

    private static VisualElement ErstelleBalken(Color farbe, float breite, float hoehe, float links, float oben, float winkel)
    {
        var balken = new VisualElement();
        balken.style.position = Position.Absolute;
        balken.style.width = breite;
        balken.style.height = hoehe;
        balken.style.left = links;
        balken.style.top = oben;
        balken.style.backgroundColor = farbe;
        balken.style.rotate = new Rotate(new Angle(winkel));
        balken.style.borderTopLeftRadius = 4;
        balken.style.borderTopRightRadius = 4;
        balken.style.borderBottomLeftRadius = 4;
        balken.style.borderBottomRightRadius = 4;
        return balken;
    }
}