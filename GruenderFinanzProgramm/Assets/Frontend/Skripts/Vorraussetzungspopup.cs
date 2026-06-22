using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Welche Pflichtbereiche fehlen
public enum VoraussetzungsBereich
{
    Unternehmensdaten,
    Bankverbindung,
    Rechnungsformat,
    Bezahlweise
}

public static class VoraussetzungsPopup
{
    private static readonly Color Rot        = new Color(230f / 255f,  57f / 255f,  70f / 255f);
    private static readonly Color Gruen      = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color KartenFarbe= new Color( 38f / 255f,  38f / 255f,  38f / 255f);
    private static readonly Color FeldFarbe  = new Color( 50f / 255f,  50f / 255f,  50f / 255f);

    // Zeigt das Popup wenn mind. ein Bereich fehlt.
    // Gibt true zurück wenn alle Bereiche vorhanden sind (Aktion darf fortfahren).
    // Gibt false zurück wenn etwas fehlt (Aktion soll abgebrochen werden).
    //
    // Verwendung durch Mitarbeiter:
    //   List<VoraussetzungsBereich> fehlend = PruefePflichtdaten();
    //   if (!VoraussetzungsPopup.Pruefen(root, fehlend)) return;
    //
    public static bool Pruefen(VisualElement root, List<VoraussetzungsBereich> fehlendeBereich)
    {
        if (fehlendeBereich == null || fehlendeBereich.Count == 0)
            return true;

        Zeigen(root, fehlendeBereich);
        return false;
    }

    // Zeigt das Popup direkt mit einer Liste fehlender Bereiche.
    public static void Zeigen(VisualElement root, List<VoraussetzungsBereich> fehlend)
    {
        if (root == null || fehlend == null || fehlend.Count == 0) return;

        var overlay = new VisualElement();
        overlay.style.position        = Position.Absolute;
        overlay.style.left = 0; overlay.style.right  = 0;
        overlay.style.top  = 0; overlay.style.bottom = 0;
        overlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
        overlay.style.alignItems      = Align.Center;
        overlay.style.justifyContent  = Justify.Center;

        var karte = new VisualElement();
        karte.style.width              = 500;
        karte.style.backgroundColor    = KartenFarbe;
        karte.style.borderTopLeftRadius    = 16; karte.style.borderTopRightRadius   = 16;
        karte.style.borderBottomLeftRadius = 16; karte.style.borderBottomRightRadius= 16;
        karte.style.borderTopWidth    = 2; karte.style.borderRightWidth  = 2;
        karte.style.borderBottomWidth = 2; karte.style.borderLeftWidth   = 2;
        karte.style.borderTopColor    = Rot; karte.style.borderRightColor  = Rot;
        karte.style.borderBottomColor = Rot; karte.style.borderLeftColor   = Rot;
        karte.style.paddingTop    = 28; karte.style.paddingBottom = 28;
        karte.style.paddingLeft   = 32; karte.style.paddingRight  = 32;

        // Schließen-Button
        var btnSchliessen = new Button(() => overlay.RemoveFromHierarchy()) { text = "\u2715" };
        btnSchliessen.style.position         = Position.Absolute;
        btnSchliessen.style.top              = 8;
        btnSchliessen.style.right            = 10;
        btnSchliessen.style.width            = 28;
        btnSchliessen.style.height           = 28;
        btnSchliessen.style.backgroundColor  = Color.clear;
        btnSchliessen.style.color            = Color.white;
        btnSchliessen.style.fontSize         = 14;
        btnSchliessen.style.borderTopWidth   = 0; btnSchliessen.style.borderRightWidth   = 0;
        btnSchliessen.style.borderBottomWidth= 0; btnSchliessen.style.borderLeftWidth    = 0;
        btnSchliessen.RegisterCallback<MouseEnterEvent>(_ =>
            btnSchliessen.style.color = new Color(0.7f, 0.7f, 0.7f));
        btnSchliessen.RegisterCallback<MouseLeaveEvent>(_ =>
            btnSchliessen.style.color = Color.white);
        karte.Add(btnSchliessen);

        // X-Symbol
        var symbol = ErstelleXSymbol();
        symbol.style.alignSelf  = Align.Center;
        symbol.style.marginBottom = 16;
        karte.Add(symbol);

        // Überschrift
        var ueberschrift = new Label("Aktion nicht möglich");
        ueberschrift.style.fontSize = 20;
        ueberschrift.style.color    = Color.white;
        ueberschrift.style.unityFontStyleAndWeight = FontStyle.Bold;
        ueberschrift.style.unityTextAlign  = TextAnchor.MiddleCenter;
        ueberschrift.style.marginBottom    = 8;
        karte.Add(ueberschrift);

        // Erklärungstext
        var erklaerung = new Label(
            "Bitte hinterlege zuerst alle erforderlichen\nDaten in den Einstellungen:");
        erklaerung.style.fontSize  = 14;
        erklaerung.style.color     = new Color(0.7f, 0.7f, 0.7f);
        erklaerung.style.whiteSpace = WhiteSpace.Normal;
        erklaerung.style.unityTextAlign = TextAnchor.MiddleCenter;
        erklaerung.style.marginBottom   = 20;
        karte.Add(erklaerung);

        // Fehlende Bereiche als Kacheln
        foreach (var bereich in fehlend)
        {
            var zeile = ErstelleBereichsZeile(bereich);
            karte.Add(zeile);
        }

        // Einstellungen-Hinweis
        var hinweis = new Label("Einstellungen \u2192 jeweiligen Bereich öffnen");
        hinweis.style.fontSize        = 12;
        hinweis.style.color           = new Color(0.5f, 0.5f, 0.5f);
        hinweis.style.unityTextAlign  = TextAnchor.MiddleCenter;
        hinweis.style.marginTop       = 18;
        karte.Add(hinweis);

        karte.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
        overlay.RegisterCallback<ClickEvent>(_ => overlay.RemoveFromHierarchy());

        overlay.Add(karte);
        root.Add(overlay);
    }

    private static VisualElement ErstelleBereichsZeile(VoraussetzungsBereich bereich)
    {
        var zeile = new VisualElement();
        zeile.style.flexDirection   = FlexDirection.Row;
        zeile.style.alignItems      = Align.Center;
        zeile.style.backgroundColor = FeldFarbe;
        zeile.style.borderTopLeftRadius    = 8; zeile.style.borderTopRightRadius   = 8;
        zeile.style.borderBottomLeftRadius = 8; zeile.style.borderBottomRightRadius= 8;
        zeile.style.paddingTop    = 10; zeile.style.paddingBottom = 10;
        zeile.style.paddingLeft   = 14; zeile.style.paddingRight  = 14;
        zeile.style.marginBottom  = 8;

        // Roter Punkt
        var punkt = new VisualElement();
        punkt.style.width  = 10; punkt.style.height = 10;
        punkt.style.borderTopLeftRadius    = 5; punkt.style.borderTopRightRadius   = 5;
        punkt.style.borderBottomLeftRadius = 5; punkt.style.borderBottomRightRadius= 5;
        punkt.style.backgroundColor = Rot;
        punkt.style.flexShrink  = 0;
        punkt.style.marginRight = 12;
        zeile.Add(punkt);

        // Name + Beschreibung
        var textSpalte = new VisualElement();
        textSpalte.style.flexDirection = FlexDirection.Column;
        textSpalte.style.flexGrow = 1;

        var name = new Label(BereichName(bereich));
        name.style.fontSize  = 14;
        name.style.color     = Color.white;
        name.style.unityFontStyleAndWeight = FontStyle.Bold;
        textSpalte.Add(name);

        var beschreibung = new Label(BereichBeschreibung(bereich));
        beschreibung.style.fontSize = 11;
        beschreibung.style.color    = new Color(0.6f, 0.6f, 0.6f);
        textSpalte.Add(beschreibung);

        zeile.Add(textSpalte);

        // Pfeil-Icon
        var pfeil = new Label("\u203a");
        pfeil.style.fontSize    = 18;
        pfeil.style.color       = new Color(0.5f, 0.5f, 0.5f);
        pfeil.style.flexShrink  = 0;
        pfeil.style.marginLeft  = 8;
        zeile.Add(pfeil);

        return zeile;
    }

    private static string BereichName(VoraussetzungsBereich bereich)
    {
        switch (bereich)
        {
            case VoraussetzungsBereich.Unternehmensdaten: return "Unternehmensdaten";
            case VoraussetzungsBereich.Bankverbindung:    return "Bankverbindung";
            case VoraussetzungsBereich.Rechnungsformat:  return "Rechnungsformat";
            case VoraussetzungsBereich.Bezahlweise:      return "Bezahlweise";
            default:                                      return bereich.ToString();
        }
    }

    private static string BereichBeschreibung(VoraussetzungsBereich bereich)
    {
        switch (bereich)
        {
            case VoraussetzungsBereich.Unternehmensdaten:
                return "Firmenname, Adresse und Steuerdaten fehlen";
            case VoraussetzungsBereich.Bankverbindung:
                return "IBAN, BIC oder Kontoinhaber fehlen";
            case VoraussetzungsBereich.Rechnungsformat:
                return "Nummernkreis oder Zahlungsziel fehlen";
            case VoraussetzungsBereich.Bezahlweise:
                return "AGB, Disclaimer oder Zahlungshinweis fehlen";
            default:
                return "";
        }
    }

    private static VisualElement ErstelleXSymbol()
    {
        var container = new VisualElement();
        container.style.width  = 56;
        container.style.height = 56;
        container.style.marginTop = 8;

        var balken1 = ErstelleBalken(Rot, 44, 6, 6, 25, 45f);
        var balken2 = ErstelleBalken(Rot, 44, 6, 6, 25, -45f);
        container.Add(balken1);
        container.Add(balken2);
        return container;
    }

    private static VisualElement ErstelleBalken(
        Color farbe, float breite, float hoehe,
        float links, float oben, float winkel)
    {
        var balken = new VisualElement();
        balken.style.position        = Position.Absolute;
        balken.style.width           = breite;
        balken.style.height          = hoehe;
        balken.style.left            = links;
        balken.style.top             = oben;
        balken.style.backgroundColor = farbe;
        balken.style.rotate          = new Rotate(new Angle(winkel));
        balken.style.borderTopLeftRadius    = 4; balken.style.borderTopRightRadius   = 4;
        balken.style.borderBottomLeftRadius = 4; balken.style.borderBottomRightRadius= 4;
        return balken;
    }
}
