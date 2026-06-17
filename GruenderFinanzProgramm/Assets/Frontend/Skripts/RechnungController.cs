using System.Collections.Generic;

public class RechnungController : BelegScreenController
{
    protected override string BelegTyp => "Rechnung";
    protected override string NummernPrefix => "RE";
    protected override List<string> StatusOptionen => new List<string> { "Entwurf", "Versendet", "Angenommen", "Abgelehnt" };
}

