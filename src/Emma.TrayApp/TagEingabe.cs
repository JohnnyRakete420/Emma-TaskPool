using System.Collections.ObjectModel;

namespace Emma.TrayApp;

/// <summary>Ein einzelner Uhrzeit-Eintrag innerhalb eines Wochentags (editierbar, entfernbar).</summary>
public class ZeitWert
{
    public string Wert { get; set; } = "20:00";
}

/// <summary>Bindungsobjekt für einen Wochentag im Plan-Formular: an/aus + beliebig viele Uhrzeiten.</summary>
public class TagEingabe
{
    public DayOfWeek Wochentag { get; set; }
    public string Anzeigename { get; set; } = "";
    public bool IstAktiv { get; set; }
    public ObservableCollection<ZeitWert> Zeiten { get; set; } = [];
}
