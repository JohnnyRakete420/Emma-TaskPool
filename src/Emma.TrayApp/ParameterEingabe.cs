using System.Collections.ObjectModel;
using Emma.Shared;

namespace Emma.TrayApp;

/// <summary>Eine Option innerhalb eines Mehrfachauswahl-Felds (z.B. eine Software-Checkbox).</summary>
public class AuswahlOption
{
    public string Name { get; set; } = "";
    public bool IstAusgewaehlt { get; set; }
}

/// <summary>Bindungsobjekt für ein dynamisch erzeugtes Formularfeld (Bezeichnung + eingegebener Wert).</summary>
public class ParameterEingabe
{
    public string Bezeichnung { get; set; } = "";
    public ParameterFeldTyp Typ { get; set; } = ParameterFeldTyp.Text;
    public List<string> Optionen { get; set; } = [];

    /// <summary>Wert für Text-/Auswahl-Felder (bei Auswahl per ComboBox-Bindung gesetzt).</summary>
    public string Wert { get; set; } = "";

    /// <summary>Checkbox-Zustände für Mehrfachauswahl-Felder.</summary>
    public ObservableCollection<AuswahlOption> Mehrfachoptionen { get; set; } = [];

    /// <summary>Erzeugt eine Eingabe für ein Feld, optional mit vorhandenem Wert (zum Bearbeiten).</summary>
    public static ParameterEingabe Neu(ParameterFeldDefinition feld, string? vorhandenerWert = null)
    {
        var eingabe = new ParameterEingabe { Bezeichnung = feld.Bezeichnung, Typ = feld.Typ, Optionen = feld.Optionen ?? [] };

        if (feld.Typ == ParameterFeldTyp.Mehrfachauswahl)
        {
            var ausgewaehlt = (vorhandenerWert ?? "")
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToHashSet();
            eingabe.Mehrfachoptionen = new ObservableCollection<AuswahlOption>(
                eingabe.Optionen.Select(o => new AuswahlOption { Name = o, IstAusgewaehlt = ausgewaehlt.Contains(o) }));
        }
        else
        {
            eingabe.Wert = vorhandenerWert ?? "";
        }

        return eingabe;
    }

    /// <summary>Der zu übermittelnde Wert - bei Mehrfachauswahl aus den angehakten Optionen zusammengesetzt.</summary>
    public string ErmittleWert() => Typ == ParameterFeldTyp.Mehrfachauswahl
        ? string.Join(", ", Mehrfachoptionen.Where(o => o.IstAusgewaehlt).Select(o => o.Name))
        : Wert;
}
