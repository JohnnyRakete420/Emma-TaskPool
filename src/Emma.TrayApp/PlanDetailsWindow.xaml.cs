using System.Windows;
using Emma.Shared;
using Emma.Shared.Dtos;

namespace Emma.TrayApp;

/// <summary>Read-only Detailansicht für einen wiederkehrenden Plan - per Doppelklick geöffnet.</summary>
public partial class PlanDetailsWindow : Window
{
    public PlanDetailsWindow(WiederkehrenderPlanDto plan)
    {
        InitializeComponent();

        ProzessNameText.Text = plan.ProzessName;

        var zeitpunkte = WochentagsReihenfolge(plan.Zeitpunkte)
            .Select(z => new { Wochentag = PlanJsonHelper.DeutscherWochentag(z.Wochentag), Uhrzeit = PlanJsonHelper.FormatiereUhrzeit(z.Uhrzeit) })
            .ToList();
        ZeitpunkteItemsControl.ItemsSource = zeitpunkte;

        ParameterItemsControl.ItemsSource = plan.ParameterWerte;
        ParameterItemsControl.Visibility = plan.ParameterWerte.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static IEnumerable<PlanZeitpunkt> WochentagsReihenfolge(List<PlanZeitpunkt> zeitpunkte)
    {
        DayOfWeek[] reihenfolge =
        [
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
            DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
        ];
        return zeitpunkte
            .OrderBy(z => Array.IndexOf(reihenfolge, z.Wochentag))
            .ThenBy(z => z.Uhrzeit);
    }

    private void SchliessenButton_Click(object sender, RoutedEventArgs e) => Close();
}
