using Emma.Shared;

namespace Emma.TrayApp;

/// <summary>Bindungsobjekt für eine Formularfeld-Definition beim Anlegen/Bearbeiten eines Prozesses.</summary>
public class FeldDefinitionEingabe
{
    public string Bezeichnung { get; set; } = "";
    public ParameterFeldTyp Typ { get; set; } = ParameterFeldTyp.Text;

    /// <summary>Nur für Auswahl/Mehrfachauswahl relevant - durch Komma getrennte Optionen.</summary>
    public string OptionenText { get; set; } = "";

    public static FeldDefinitionEingabe Von(ParameterFeldDefinition definition) => new()
    {
        Bezeichnung = definition.Bezeichnung,
        Typ = definition.Typ,
        OptionenText = definition.Optionen is null ? "" : string.Join(", ", definition.Optionen)
    };

    public ParameterFeldDefinition ZuDefinition()
    {
        if (Typ == ParameterFeldTyp.Text)
            return new ParameterFeldDefinition(Bezeichnung.Trim());

        var optionen = OptionenText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        return new ParameterFeldDefinition(Bezeichnung.Trim(), Typ, optionen.Count > 0 ? optionen : null);
    }
}
