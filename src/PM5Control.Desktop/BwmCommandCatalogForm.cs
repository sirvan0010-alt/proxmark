using PM5Control.Core.Bwm;

namespace PM5Control.Desktop;

internal sealed class BwmCommandCatalogForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly ComboBox _group = new();
    private readonly Label _status = new();

    public BwmCommandCatalogForm(string? initialGroup = null)
    {
        Text = "PM5 BWM command catalog";
        StartPosition = FormStartPosition.CenterParent;
        Width = 1180;
        Height = 720;
        BackColor = Color.FromArgb(18, 20, 24);
        ForeColor = Color.White;

        _group.DropDownStyle = ComboBoxStyle.DropDownList;
        _group.Items.Add("All");
        foreach (var group in BwmCommandCatalog.All.Select(x => x.Group).Distinct())
            _group.Items.Add(group);
        _group.SelectedItem = initialGroup ?? "All";
        _group.SelectedIndexChanged += (_, _) => RefreshGrid();
        _group.Location = new Point(18, 16);
        _group.Width = 180;

        _status.AutoSize = true;
        _status.Location = new Point(220, 20);
        _status.ForeColor = Color.FromArgb(160, 168, 180);

        _grid.Location = new Point(18, 55);
        _grid.Size = new Size(1125, 610);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AutoGenerateColumns = false;
        _grid.BackgroundColor = Color.FromArgb(25, 28, 34);
        _grid.GridColor = Color.FromArgb(55, 60, 68);
        _grid.ForeColor = Color.Black;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", DataPropertyName = "Code", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Group", DataPropertyName = "Group", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Command", DataPropertyName = "Name", Width = 360 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Firmware", DataPropertyName = "Firmware", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Note", DataPropertyName = "Description", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

        Controls.Add(_group);
        Controls.Add(_status);
        Controls.Add(_grid);
        RefreshGrid();
    }

    private void RefreshGrid()
    {
        var selected = _group.SelectedItem?.ToString() ?? "All";
        var rows = BwmCommandCatalog.All
            .Where(x => selected == "All" || x.Group.Equals(selected, StringComparison.OrdinalIgnoreCase))
            .Select(x => new
            {
                x.Code,
                x.Group,
                x.Name,
                Firmware = x.ExposedByCurrentFirmware ? "EXPOSED" : "NOT EXPOSED",
                x.Description
            })
            .ToList();

        _grid.DataSource = rows;
        _status.Text = $"{rows.Count} commands from upstream app_com_defs.h · catalog only; no guessed on-wire execution";
    }
}
