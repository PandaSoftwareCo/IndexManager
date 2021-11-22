using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls.Ribbon;
using IndexManager.ViewModels;
using Microsoft.Extensions.Logging;

namespace IndexManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        private readonly ILogger<MainWindow> _logger;

        public MainWindow(MainWindowViewModel model, ILogger<MainWindow> logger)
        {
            InitializeComponent();

            DataContext = model;
            _logger = logger;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var columnIndex = 1;
            var column = IndexesDataGrid.Columns[columnIndex];
            var sortDirection = ListSortDirection.Ascending;

            // Clear current sort descriptions
            IndexesDataGrid.Items.SortDescriptions.Clear();

            // Add the new sort description
            IndexesDataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            columnIndex = 0;
            column = AliasesDataGrid.Columns[columnIndex];
            AliasesDataGrid.Items.SortDescriptions.Clear();
            AliasesDataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            column = AliasCountDataGrid.Columns[columnIndex];
            AliasCountDataGrid.Items.SortDescriptions.Clear();
            AliasCountDataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));

            column = TemplatesDataGrid.Columns[columnIndex];
            TemplatesDataGrid.Items.SortDescriptions.Clear();
            TemplatesDataGrid.Items.SortDescriptions.Add(new SortDescription(column.SortMemberPath, sortDirection));
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
