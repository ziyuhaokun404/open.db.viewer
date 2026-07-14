using System.Windows.Controls;

namespace Open.Db.Viewer.Shell.Views.Workspace;

public partial class QueryPanel : UserControl
{
    public QueryPanel()
    {
        InitializeComponent();
    }

    private void OnAutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (e.Column is DataGridTextColumn textColumn &&
            TryFindResource("DataGridTextCellStyle") is System.Windows.Style style)
        {
            textColumn.ElementStyle = style;
        }
    }
}
