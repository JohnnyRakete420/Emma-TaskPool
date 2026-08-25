using System.Windows;
using System.Windows.Controls;
using Emma.Shared;

namespace Emma.TrayApp;

/// <summary>Wählt das passende Eingabe-Template (Text/Auswahl/Mehrfachauswahl) für ein <see cref="ParameterEingabe"/>.</summary>
public class ParameterFeldTemplateSelector : DataTemplateSelector
{
    public DataTemplate? TextTemplate { get; set; }
    public DataTemplate? AuswahlTemplate { get; set; }
    public DataTemplate? MehrfachauswahlTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object? item, DependencyObject container) => item switch
    {
        ParameterEingabe { Typ: ParameterFeldTyp.Auswahl } => AuswahlTemplate,
        ParameterEingabe { Typ: ParameterFeldTyp.Mehrfachauswahl } => MehrfachauswahlTemplate,
        _ => TextTemplate
    };
}
