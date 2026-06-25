using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

// Universelles Hilfe-Tooltip-System.
//
// Für feste Elemente im Screen-UXML:
//   HelpTooltip.Registriere(root, "btn-help-xyz", "Erklärungstext.");
//
// Für dynamisch instanziierte Templates (z.B. Kundenkarten):
//   HelpTooltip.RegistriereInKarte(root, karte.Q<VisualElement>("btn-help-xyz"), "Text");
//
// UXML-Snippet für jedes Icon-Element:
//   <ui:VisualElement name="btn-help-NAME" class="help-icon"
//       style="background-image: url('...Help circle.png...guid=1ea511ec5bd6a8148ab87a586b001f57...');
//              width: 30px; height: 30px; min-width: 30px; min-height: 30px; flex-shrink: 0;"/>
public static class HelpTooltip
{
    private const float BubbleBreite  = 280f;
    private const float BubblePadding = 14f;
    private const float BubbleAbstand = 10f;
    private const int   BubbleSchrift = 13;

    private static readonly Color RandFarbe       = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color IconTint         = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color HoverHintergrund = new Color(128f / 255f, 207f / 255f, 149f / 255f, 0.18f);
    private static readonly Color Hintergrund      = new Color(70f / 255f, 70f / 255f, 70f / 255f);
    private static readonly Color Transparent      = new Color(0f, 0f, 0f, 0f);

    // Für benannte Elemente direkt im Root-UXML
    public static void Registriere(VisualElement root, string elementName, string tooltipText)
    {
        var icon = root.Q<VisualElement>(elementName);
        if (icon == null)
        {
            Debug.LogWarning("[HelpTooltip] Element nicht gefunden: " + elementName);
            return;
        }
        RegistriereInKarte(root, icon, tooltipText);
    }

    // Für dynamisch instanziierte Template-Karten.
    // bubbleRoot: Root der Hauptszene (Bubble schwebt über allem)
    // iconElement: das konkrete VisualElement aus der Karte
    public static void RegistriereInKarte(VisualElement bubbleRoot, VisualElement iconElement, string tooltipText)
    {
        if (iconElement == null)
        {
            Debug.LogWarning("[HelpTooltip] RegistriereInKarte: iconElement ist null");
            return;
        }

        SetzeBasisStil(iconElement);

        var bubble = ErstelleBubble(tooltipText);
        bubble.style.display = DisplayStyle.None;
        bubble.pickingMode   = PickingMode.Ignore;
        bubbleRoot.Add(bubble);

        bool fixiert = false;

        iconElement.RegisterCallback<PointerEnterEvent>(_ =>
        {
            iconElement.style.backgroundColor = HoverHintergrund;
            if (!fixiert) ZeigeBubble(bubble, iconElement, bubbleRoot);
        });

        iconElement.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            if (!fixiert)
            {
                iconElement.style.backgroundColor = Hintergrund;
                bubble.style.display = DisplayStyle.None;
            }
        });

        iconElement.RegisterCallback<PointerDownEvent>(_ =>
        {
            fixiert = !fixiert;
            iconElement.style.backgroundColor = fixiert ? HoverHintergrund : Hintergrund;
            if (fixiert)
                ZeigeBubble(bubble, iconElement, bubbleRoot);
            else
                bubble.style.display = DisplayStyle.None;
        });

        // Klick außerhalb schließt fixierte Bubble
        bubbleRoot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (!fixiert) return;
            if (!iconElement.worldBound.Contains(new Vector2(evt.position.x, evt.position.y)))
            {
                fixiert = false;
                iconElement.style.backgroundColor = Hintergrund;
                bubble.style.display = DisplayStyle.None;
            }
        }, TrickleDown.TrickleDown);
    }

    private static void SetzeBasisStil(VisualElement icon)
    {
        icon.pickingMode                         = PickingMode.Position;
        icon.focusable                           = false;
        // Größe: breiter als das Icon selbst, damit Abstand zum Rand entsteht
        icon.style.width                         = 36;
        icon.style.height                        = 36;
        icon.style.minWidth                      = 36;
        icon.style.minHeight                     = 36;
        // Ecken leicht abgerundet aber eckig — per C# inline gesetzt,
        // da Unity USS-Werte bei VisualElement zur Laufzeit überschreibt
        icon.style.borderTopLeftRadius           = 6;
        icon.style.borderTopRightRadius          = 6;
        icon.style.borderBottomLeftRadius        = 6;
        icon.style.borderBottomRightRadius       = 6;
        icon.style.backgroundColor               = Hintergrund;
        icon.style.unityBackgroundImageTintColor = IconTint;
        // Icon nimmt nur 60% der Fläche ein, Rest ist Padding-Abstand
        icon.style.backgroundSize                = new BackgroundSize(
            new Length(60, LengthUnit.Percent),
            new Length(60, LengthUnit.Percent)
        );
        icon.style.backgroundPositionX           = new BackgroundPosition(BackgroundPositionKeyword.Center);
        icon.style.backgroundPositionY           = new BackgroundPosition(BackgroundPositionKeyword.Center);
        icon.style.flexShrink                    = 0;
    }

    private static void ZeigeBubble(VisualElement bubble, VisualElement icon, VisualElement root)
    {
        bubble.style.display = DisplayStyle.Flex;

        bubble.schedule.Execute(() =>
        {
            var iconPos = icon.worldBound;
            var rootPos = root.worldBound;
            float bubbleH = bubble.resolvedStyle.height > 0 ? bubble.resolvedStyle.height : 80f;

            float left = iconPos.x - rootPos.x - (BubbleBreite / 2f) + (iconPos.width / 2f);
            float top  = iconPos.y - rootPos.y - bubbleH - BubbleAbstand;

            left = Mathf.Clamp(left, 8f, rootPos.width - BubbleBreite - 8f);
            if (top < 8f) top = iconPos.y - rootPos.y + iconPos.height + BubbleAbstand;

            bubble.style.left = left;
            bubble.style.top  = top;
        });
    }

    private static VisualElement ErstelleBubble(string text)
    {
        var bubble = new VisualElement();
        bubble.style.position                = Position.Absolute;
        bubble.style.width                   = BubbleBreite;
        bubble.style.backgroundColor         = new Color(28f / 255f, 28f / 255f, 28f / 255f, 0.97f);
        bubble.style.borderTopLeftRadius     = 10;
        bubble.style.borderTopRightRadius    = 10;
        bubble.style.borderBottomLeftRadius  = 10;
        bubble.style.borderBottomRightRadius = 10;
        bubble.style.borderTopWidth          = 1;
        bubble.style.borderRightWidth        = 1;
        bubble.style.borderBottomWidth       = 1;
        bubble.style.borderLeftWidth         = 1;
        bubble.style.borderTopColor          = RandFarbe;
        bubble.style.borderRightColor        = RandFarbe;
        bubble.style.borderBottomColor       = RandFarbe;
        bubble.style.borderLeftColor         = RandFarbe;
        bubble.style.paddingTop              = BubblePadding;
        bubble.style.paddingBottom           = BubblePadding;
        bubble.style.paddingLeft             = BubblePadding;
        bubble.style.paddingRight            = BubblePadding;

        var label = new Label(text);
        label.style.color      = new Color(0.88f, 0.88f, 0.88f);
        label.style.fontSize   = BubbleSchrift;
        label.style.whiteSpace = WhiteSpace.Normal;
        bubble.Add(label);

        return bubble;
    }
}