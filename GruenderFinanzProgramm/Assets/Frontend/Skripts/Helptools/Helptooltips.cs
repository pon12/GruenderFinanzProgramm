using UnityEngine;
using UnityEngine.UIElements;
using Color = UnityEngine.Color;

// Universelles Hilfe-Tooltip-System mit dynamisch gespiegeltem Ganzkörper-Begleiter.
public static class HelpTooltip
{
    private const float BubbleBreite = 320f;
    private const float BubblePadding = 14f;
    private const float BubbleAbstand = 10f;
    private const int BubbleSchrift = 11;
    private const float BubbleMaxHoehe = 320f;

    // Verzögerung, bevor der Tooltip beim Hovern erscheint - verhindert,
    // dass er beim bloßen Drüberwischen mit der Maus schon aufpoppt.
    private const long HoverVerzoegerungMs = 450;

    // Einheitliche Größe für deinen Ganzkörper-Begleiter
    private const float BegleiterBreite = 180f;
    private const float BegleiterHoehe = 220f;

    // --- POSITION LINKS VOM POPUP (Wenn Icon auf der rechten Bildschirmseite ist) ---
    // Der Begleiter steht links von der Box und schaut im Standard-Zustand nach rechts.
    private const float BegleiterOffsetX_LinksVomPopup = -160f;
    private const float BegleiterOffsetY_LinksVomPopup = -120f;

    // --- POSITION RECHTS VOM POPUP (Wenn Icon auf der linken Bildschirmseite ist) ---
    // Der Begleiter steht rechts von der Box. Hier wird er im Code nach links gespiegelt!
    private const float BegleiterOffsetX_RechtsVomPopup = 240f;
    private const float BegleiterOffsetY_RechtsVomPopup = -120f;

    private static readonly Color RandFarbe = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color IconTint = new Color(128f / 255f, 207f / 255f, 149f / 255f);
    private static readonly Color HoverHintergrund = new Color(128f / 255f, 207f / 255f, 149f / 255f, 0.18f);
    private static readonly Color Hintergrund = new Color(70f / 255f, 70f / 255f, 70f / 255f);

    // Speichert die einzelne Ganzkörper-Textur
    private static Texture2D _begleiterTextur;

    // PlayerPrefs-Key für den Begleiter — wird vom Einstellungscontroller gesetzt
    private static string _begleiterPrefKey = "settings_begleiter";

    // Muss vom Einstellungscontroller beim Start aufgerufen werden
    // damit der korrekte Key (mit DB-Prefix) genutzt wird
    public static void SetzeBegleiterPrefKey(string key)
    {
        _begleiterPrefKey = key;
    }

    // Für feste Elemente direkt im Root-UXML
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

    // Für dynamisch instanziierte Template-Karten
    public static void RegistriereInKarte(VisualElement bubbleRoot, VisualElement iconElement, string tooltipText)
    {
        if (iconElement == null)
        {
            Debug.LogWarning("[HelpTooltip] RegistriereInKarte: iconElement ist null");
            return;
        }

        SetzeBasisStil(iconElement);

        var bubble = ErstelleBubble(tooltipText);
        var begleiter = ErstelleBegleiter();

        bubble.style.display = DisplayStyle.None;
        begleiter.style.display = DisplayStyle.None;

        bubble.pickingMode = PickingMode.Ignore;
        begleiter.pickingMode = PickingMode.Ignore;

        // Begleiter zuerst hinzufügen (liegt hinter/unter der Bubble)
        bubbleRoot.Add(begleiter);
        bubbleRoot.Add(bubble);

        bool fixiert = false;
        IVisualElementScheduledItem hoverTask = null;

        iconElement.RegisterCallback<PointerEnterEvent>(_ =>
        {
            iconElement.style.backgroundColor = HoverHintergrund;
            if (fixiert) return;

            hoverTask?.Pause();
            hoverTask = iconElement.schedule.Execute(() =>
            {
                ZeigeBubbleMitBegleiter(bubble, begleiter, iconElement, bubbleRoot);
            }).StartingIn(HoverVerzoegerungMs);
        });

        iconElement.RegisterCallback<PointerLeaveEvent>(_ =>
        {
            hoverTask?.Pause();
            hoverTask = null;

            if (!fixiert)
            {
                iconElement.style.backgroundColor = Hintergrund;
                bubble.style.display = DisplayStyle.None;
                begleiter.style.display = DisplayStyle.None;
            }
        });

        iconElement.RegisterCallback<PointerDownEvent>(_ =>
        {
            hoverTask?.Pause();
            hoverTask = null;

            fixiert = !fixiert;
            iconElement.style.backgroundColor = fixiert ? HoverHintergrund : Hintergrund;
            if (fixiert)
                ZeigeBubbleMitBegleiter(bubble, begleiter, iconElement, bubbleRoot);
            else
            {
                bubble.style.display = DisplayStyle.None;
                begleiter.style.display = DisplayStyle.None;
            }
        });

        bubbleRoot.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (!fixiert) return;
            if (!iconElement.worldBound.Contains(new Vector2(evt.position.x, evt.position.y)))
            {
                fixiert = false;
                iconElement.style.backgroundColor = Hintergrund;
                bubble.style.display = DisplayStyle.None;
                begleiter.style.display = DisplayStyle.None;
            }
        }, TrickleDown.TrickleDown);
    }

    public static void SetzeBasisStilOeffentlich(VisualElement icon) => SetzeBasisStil(icon);

    // Wird vom neuen BegleiterInitializer aufgerufen
    public static void SetzeBegleiterTextur(Texture2D textur)
    {
        _begleiterTextur = textur;
    }

    // ============================================================
    // POSITIONIERUNG & SPIEGELUNG
    // ============================================================

    private static void ZeigeBubbleMitBegleiter(
        VisualElement bubble,
        VisualElement begleiter,
        VisualElement icon,
        VisualElement root)
    {
        bubble.style.display = DisplayStyle.Flex;

        bubble.schedule.Execute(() =>
        {
            var iconPos = icon.worldBound;
            var rootPos = root.worldBound;
            float bubbleH = bubble.resolvedStyle.height > 0 ? bubble.resolvedStyle.height : 80f;

            float left = iconPos.x - rootPos.x - (BubbleBreite / 2f) + (iconPos.width / 2f);
            float top = iconPos.y - rootPos.y - bubbleH - BubbleAbstand;

            // PRÜFUNG: Befindet sich das Hilfe-Icon auf der rechten Bildschirmhälfte?
            bool istAufRechterSeite = (iconPos.x - rootPos.x) > (rootPos.width / 2f);

            left = Mathf.Clamp(left, 8f, rootPos.width - BubbleBreite - 8f);
            if (top < 8f) top = iconPos.y - rootPos.y + iconPos.height + BubbleAbstand;

            bubble.style.left = left;
            bubble.style.top = top;

            if (!BegleiterAktiv())
            {
                begleiter.style.display = DisplayStyle.None;
                return;
            }

            begleiter.style.display = DisplayStyle.Flex;

            if (istAufRechterSeite)
            {
                // 1. Position auf die LINKE Seite des Popups setzen
                begleiter.style.left = left + BegleiterOffsetX_LinksVomPopup;
                begleiter.style.top = top + bubbleH + BegleiterOffsetY_LinksVomPopup;

                // 2. Normal darstellen (X = 1), da er von links nach rechts zur Box schaut
                begleiter.style.scale = new StyleScale(new Scale(new Vector3(1f, 1f, 1f)));
            }
            else
            {
                // 1. Position auf die RECHTE Seite des Popups setzen
                begleiter.style.left = left + BegleiterOffsetX_RechtsVomPopup;
                begleiter.style.top = top + bubbleH + BegleiterOffsetY_RechtsVomPopup;

                // 2. Horizontal spiegeln (X = -1), damit er von rechts nach links zur Box schaut
                begleiter.style.scale = new StyleScale(new Scale(new Vector3(-1f, 1f, 1f)));
            }

            // Sicherstellen, dass die Textur zugewiesen ist (falls sich die Szene live geändert hat)
            if (_begleiterTextur != null && begleiter.style.backgroundImage.value.texture == null)
            {
                begleiter.style.backgroundImage = new StyleBackground(_begleiterTextur);
            }
        });
    }

    // ============================================================
    // BEGLEITER ELEMENT ERSTELLEN
    // ============================================================

    private static bool BegleiterAktiv()
    {
        return PlayerPrefs.GetInt(_begleiterPrefKey, 1) == 1;
    }

    private static VisualElement ErstelleBegleiter()
    {
        var el = new VisualElement();
        el.style.position = Position.Absolute;
        el.style.width = BegleiterBreite;
        el.style.height = BegleiterHoehe;

        // Nutzt jetzt direkt die eine zugewiesene Ganzkörper-Textur
        if (_begleiterTextur != null)
        {
            el.style.backgroundImage = new StyleBackground(_begleiterTextur);
            el.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            el.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            el.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Bottom);
        }

        return el;
    }

    // ============================================================
    // BASIS-STIL FÜR FRAGEZEICHEN-ICON
    // ============================================================

    private static void SetzeBasisStil(VisualElement icon)
    {
        icon.pickingMode = PickingMode.Position;
        icon.focusable = false;
        icon.style.width = 36;
        icon.style.height = 36;
        icon.style.minWidth = 36;
        icon.style.minHeight = 36;
        icon.style.borderTopLeftRadius = 6;
        icon.style.borderTopRightRadius = 6;
        icon.style.borderBottomLeftRadius = 6;
        icon.style.borderBottomRightRadius = 6;
        icon.style.backgroundColor = Hintergrund;
        icon.style.unityBackgroundImageTintColor = IconTint;
        icon.style.backgroundSize = new BackgroundSize(
            new Length(60, LengthUnit.Percent),
            new Length(60, LengthUnit.Percent)
        );
        icon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        icon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        icon.style.flexShrink = 0;
    }

    // ============================================================
    // BUBBLE ELEMENT ERSTELLEN
    // ============================================================

    private static VisualElement ErstelleBubble(string text)
    {
        var bubble = new VisualElement();
        bubble.style.position = Position.Absolute;
        bubble.style.width = BubbleBreite;
        bubble.style.backgroundColor = new Color(28f / 255f, 28f / 255f, 28f / 255f, 0.97f);
        bubble.style.borderTopLeftRadius = 10;
        bubble.style.borderTopRightRadius = 10;
        bubble.style.borderBottomLeftRadius = 10;
        bubble.style.borderBottomRightRadius = 10;
        bubble.style.borderTopWidth = 1;
        bubble.style.borderRightWidth = 1;
        bubble.style.borderBottomWidth = 1;
        bubble.style.borderLeftWidth = 1;
        bubble.style.borderTopColor = RandFarbe;
        bubble.style.borderRightColor = RandFarbe;
        bubble.style.borderBottomColor = RandFarbe;
        bubble.style.borderLeftColor = RandFarbe;
        bubble.style.paddingTop = BubblePadding;
        bubble.style.paddingBottom = BubblePadding;
        bubble.style.paddingLeft = BubblePadding;
        bubble.style.paddingRight = BubblePadding;
        bubble.style.maxHeight = BubbleMaxHoehe;
        bubble.style.overflow = Overflow.Hidden;

        var label = new Label(text);
        label.style.color = new Color(0.88f, 0.88f, 0.88f);
        label.style.fontSize = BubbleSchrift;
        label.style.whiteSpace = WhiteSpace.Normal;
        bubble.Add(label);

        return bubble;
    }
}