using TMPro;
using UnityEngine;

public class CompanyFrontendController : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField companyNameInput;
    [SerializeField] private TMP_InputField legalFormInput;
    [SerializeField] private TMP_InputField industryInput;
    [SerializeField] private TMP_InputField locationInput;

    [Header("Output")]
    [SerializeField] private TMP_Text feedbackText;

    public void createCompanyForCurrentUser()
    {
        DataBase db = UserDatabaseAccess.getCurrentUserDatabase();

        if (db == null)
        {
            showFeedback("Keine aktive Nutzerdatenbank gefunden.");
            return;
        }

        if (companyNameInput == null || legalFormInput == null || industryInput == null || locationInput == null)
        {
            showFeedback("InputFields sind nicht vollständig zugewiesen.");
            return;
        }

        string companyName = companyNameInput.text.Trim();
        string location = locationInput.text.Trim();

        if (string.IsNullOrWhiteSpace(companyName))
        {
            showFeedback("Firmenname fehlt.");
            return;
        }

        if (!int.TryParse(legalFormInput.text, out int legalForm))
        {
            showFeedback("Rechtsform muss eine Zahl sein.");
            return;
        }

        if (!int.TryParse(industryInput.text, out int industry))
        {
            showFeedback("Branche muss eine Zahl sein.");
            return;
        }
        //Vorrübergehende lösung

        string steuerNr = "";
        string gruendungsJahr = "";
        string handelsReg = "";
        string strasseuHausNr = "";
        int plz = 0;
        string ustIdNr = "";
        string email = "Null";
        string handyNr = "Null";

        db.createCompany( name, legalForm, industry, location, steuerNr,gruendungsJahr ,handelsReg , strasseuHausNr , plz , ustIdNr, email, handyNr);

        showFeedback("Firma gespeichert in: " + db.getDatabaseName());
        Debug.Log("Firma erstellt: " + companyName + " in Datenbank: " + db.getDatabaseName());
    }

    private void showFeedback(string message)
    {
        Debug.Log(message);

        if (feedbackText != null)
        {
            feedbackText.text = message;
        }
    }
}