using System.Collections.Generic;
using UnityEngine.UIElements;

public class AngebotController : BelegScreenController
{
    protected override string BelegTyp => "Angebot";
    protected override string NummernPrefix => "AN";
    protected override List<string> StatusOptionen => new List<string> { "Entwurf", "Versendet", "Angenommen", "Abgelehnt" };

    protected override void RegistriereZusatzButtons()
    {
        var umwandeln = FindeButton("btn-umwandeln", "Angebot in Rechnung umwandeln");
        umwandeln?.RegisterCallback<ClickEvent>(_ =>
            FeedbackPopup.Show(Root, "Angebot in Rechnung umgewandelt", FeedbackTyp.Erfolg));
    }

}