using System.Windows;
using System.Windows.Controls;
using PortCheck.Models;
using PortCheck.ViewModels;

namespace PortCheck.Controls;

public partial class GlassSortMenuControl : UserControl
{
    private const string PortLabel = "Port";
    private const string ProcessNameLabel = "Process name";
    private const string PidLabel = "PID";
    private const string AscendingLabel = "Ascending";
    private const string DescendingLabel = "Descending";

    public event EventHandler? MenuItemSelected;

    public GlassSortMenuControl()
    {
        InitializeComponent();
        Loaded += (_, _) => SyncSelectionState();
        DataContextChanged += (_, _) => SyncSelectionState();
    }

    private void SortFieldButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TrayViewModel viewModel || sender is not Button button)
            return;

        viewModel.SortField = button switch
        {
            var _ when ReferenceEquals(button, PortSortButton) => PortListSortField.Port,
            var _ when ReferenceEquals(button, ProcessNameSortButton) => PortListSortField.ProcessName,
            _ => PortListSortField.Pid
        };

        SyncSelectionState();
        MenuItemSelected?.Invoke(this, EventArgs.Empty);
    }

    private void SortOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TrayViewModel viewModel || sender is not Button button)
            return;

        viewModel.SortDescending = ReferenceEquals(button, DescendingButton);
        SyncSelectionState();
        MenuItemSelected?.Invoke(this, EventArgs.Empty);
    }

    private void SyncSelectionState()
    {
        if (DataContext is not TrayViewModel viewModel)
            return;

        ApplyMenuButtonState(PortSortButton, PortLabel, viewModel.SortField == PortListSortField.Port);
        ApplyMenuButtonState(ProcessNameSortButton, ProcessNameLabel, viewModel.SortField == PortListSortField.ProcessName);
        ApplyMenuButtonState(PidSortButton, PidLabel, viewModel.SortField == PortListSortField.Pid);
        ApplyMenuButtonState(AscendingButton, AscendingLabel, !viewModel.SortDescending);
        ApplyMenuButtonState(DescendingButton, DescendingLabel, viewModel.SortDescending);
    }

    private static void ApplyMenuButtonState(Button button, string label, bool isSelected)
    {
        button.Tag = isSelected ? "Selected" : null;
        button.Content = CreatePopupMenuItemContent(label, isSelected);
    }

    private static Grid CreatePopupMenuItemContent(string label, bool isSelected)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var text = new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center
        };
        text.SetResourceReference(TextBlock.ForegroundProperty, "Text.Primary");
        text.SetResourceReference(TextBlock.EffectProperty, "Text.Shadow");
        grid.Children.Add(text);

        if (!isSelected)
            return grid;

        var check = new TextBlock
        {
            Text = "\uE73E",
            FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 12,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        check.SetResourceReference(TextBlock.ForegroundProperty, "Text.Primary");
        check.SetResourceReference(TextBlock.EffectProperty, "Text.Shadow");
        Grid.SetColumn(check, 1);
        grid.Children.Add(check);
        return grid;
    }
}
