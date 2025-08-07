// FILE: GaugeForm.cs

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public class GaugeForm : Form
    {
        // --- UI Control Member Variables ---
        private TextProgressBar socProgressBar;
        private TextProgressBar sohProgressBar;
        private Label lblPackVoltage, lblCurrent, lblPackTemp, lblStatus;
        private Label[] lblCellVoltages = new Label[10];
        private RadioButton rbModeIdle, rbModeCharging, rbModeDischarging;
        private TrackBar tbPackVoltage, tbCurrent, tbPackTemp, tbCellVoltage;
        private ComboBox cmbSelectedCell;
        private Label lblSelectedCellValue;

        // --- Color Palette ---
        private Color backColorDark = Color.FromArgb(37, 37, 38);
        private Color panelColor = Color.FromArgb(45, 45, 48);
        private Color textColor = Color.FromArgb(241, 241, 241);
        private Color accentColorCyan = Color.FromArgb(0, 192, 192);
        private Color accentColorGreen = Color.FromArgb(16, 185, 129);
        private Color accentColorBlue = Color.FromArgb(59, 130, 246);
        private Color accentColorRed = Color.FromArgb(239, 68, 68);
        private Color accentColorOrange = Color.FromArgb(249, 115, 22);
        private Color valueTextColor = Color.FromArgb(13, 110, 253);     
        private Color labelTextColor = Color.FromArgb(108, 117, 125);     


        public GaugeForm()
        {
            this.Text = "BMS Gauge & Simulator (BQ78350)";
            this.Size = new Size(850, 650);
            this.MinimumSize = new Size(700, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = backColorDark;
            this.ForeColor = textColor;
            this.Font = new Font("Segoe UI", 9F);

            InitializeLayout();

            // Apply custom styles to RadioButtons after they are created.
            ApplyRadioButtonStyles(new[] { rbModeIdle, rbModeCharging, rbModeDischarging });
        }

        private void InitializeLayout()
        {
            var mainTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            this.Controls.Add(mainTlp);

            // --- LEFT PANEL: MONITOR ---
            var monitorPanel = CreateStyledGroupBox("Live Battery Monitor");
            mainTlp.Controls.Add(monitorPanel, 0, 0);
            var monitorTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(15) };
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            monitorPanel.Controls.Add(monitorTlp);

            socProgressBar = new TextProgressBar { Value = 0, ForeColor = accentColorGreen, Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 15) };
            sohProgressBar = new TextProgressBar { Value = 100, ForeColor = accentColorBlue, Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 15) };
            monitorTlp.Controls.Add(CreateLabeledControl("State of Charge (SoC)", socProgressBar), 0, 0);
            monitorTlp.Controls.Add(CreateLabeledControl("State of Health (SoH)", sohProgressBar), 1, 0);

            lblPackVoltage = CreateValueLabel("-.-- V", accentColorCyan);
            lblCurrent = CreateValueLabel("-.-- A", accentColorCyan);
            lblPackTemp = CreateValueLabel("--.- °C", accentColorCyan);
            monitorTlp.Controls.Add(CreateLabeledControl("Pack Voltage", lblPackVoltage), 0, 1);
            monitorTlp.Controls.Add(CreateLabeledControl("Current", lblCurrent), 1, 1);
            monitorTlp.Controls.Add(CreateLabeledControl("Pack Temperature", lblPackTemp), 0, 2);

            var cellGroup = CreateStyledGroupBox("Individual Cell Voltages (mV)");
            monitorTlp.SetColumnSpan(cellGroup, 2);
            monitorTlp.Controls.Add(cellGroup, 0, 3);
            var cellTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 2, Padding = new Padding(10) };
            for (int i = 0; i < 5; i++) cellTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            for (int i = 0; i < 2; i++) cellTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            cellGroup.Controls.Add(cellTlp);

            for (int i = 0; i < 10; i++)
            {
                lblCellVoltages[i] = new Label { Text = "----", Font = new Font("Consolas", 14F), ForeColor = textColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                cellTlp.Controls.Add(CreateLabeledControl($"Cell {i + 1}", lblCellVoltages[i]), i % 5, i / 5);
            }

            // --- RIGHT PANEL: CONTROLS ---
            var controlPanel = CreateStyledGroupBox("Simulation Control & Fault Injection");
            mainTlp.Controls.Add(controlPanel, 1, 0);
            var controlTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(15) };
            controlTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            controlPanel.Controls.Add(controlTlp);

            var modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, Padding = new Padding(0, 10, 0, 20) };
            rbModeIdle = new RadioButton { Text = "Idle", Checked = true, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            rbModeCharging = new RadioButton { Text = "Charging", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            rbModeDischarging = new RadioButton { Text = "Discharging", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            modePanel.Controls.AddRange(new Control[] { rbModeIdle, rbModeCharging, rbModeDischarging });
            controlTlp.Controls.Add(CreateLabeledControl("Simulation Mode", modePanel, 150), 0, 0);

            tbPackVoltage = CreateSlider(10000, 18000, 14450);
            tbCurrent = CreateSlider(-5000, 5000, 0);
            tbPackTemp = CreateSlider(-20, 80, 25);
            controlTlp.Controls.Add(CreateLabeledControl("Set Pack Voltage (mV)", tbPackVoltage, 150), 0, 1);
            controlTlp.Controls.Add(CreateLabeledControl("Set Current (mA)", tbCurrent, 150), 0, 2);
            controlTlp.Controls.Add(CreateLabeledControl("Set Pack Temp (°C)", tbPackTemp, 150), 0, 3);

            var singleCellPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
            singleCellPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            singleCellPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            singleCellPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            cmbSelectedCell = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90, BackColor = panelColor, ForeColor = textColor, FlatStyle = FlatStyle.Flat };
            for (int i = 0; i < 10; i++) cmbSelectedCell.Items.Add($"Cell {i + 1}");
            cmbSelectedCell.SelectedIndex = 0;
            tbCellVoltage = CreateSlider(2500, 4200, 3650);
            lblSelectedCellValue = new Label { Text = "3650 mV", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Width = 70, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            cmbSelectedCell.SelectedIndexChanged += (s, e) => tbCellVoltage.Value = 3650;
            tbCellVoltage.ValueChanged += (s, e) => lblSelectedCellValue.Text = $"{tbCellVoltage.Value} mV";
            singleCellPanel.Controls.Add(cmbSelectedCell, 0, 0);
            singleCellPanel.Controls.Add(tbCellVoltage, 1, 0);
            singleCellPanel.Controls.Add(lblSelectedCellValue, 2, 0);
            controlTlp.Controls.Add(CreateLabeledControl("Set Individual Cell (mV)", singleCellPanel, 150), 0, 4);
        }

        #region Helper Methods
        private GroupBox CreateStyledGroupBox(string title) { return new GroupBox { Text = title, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11F, FontStyle.Bold), ForeColor = textColor, Padding = new Padding(10), Margin = new Padding(5), BackColor = panelColor }; }
        private TableLayoutPanel CreateLabeledControl(string labelText, Control control) { var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(3) }; tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); var label = new Label { Text = labelText, AutoSize = true, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.Gray }; tlp.Controls.Add(label, 0, 0); tlp.Controls.Add(control, 0, 1); return tlp; }
        private TableLayoutPanel CreateLabeledControl(string labelText, Control control, int labelWidth) { var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 5, 0, 5) }; tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F, FontStyle.Regular) }; tlp.Controls.Add(label, 0, 0); tlp.Controls.Add(control, 1, 0); return tlp; }
        private Label CreateValueLabel(string initialText, Color color) { return new Label { Text = initialText, Font = new Font("Consolas", 20F, FontStyle.Bold), ForeColor = color, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }; }
        private TrackBar CreateSlider(int min, int max, int value) { return new TrackBar { Minimum = min, Maximum = max, Value = value, Dock = DockStyle.Fill, TickFrequency = (max - min) / 10, Padding = new Padding(0), Margin = new Padding(0) }; }
        private void ApplyRadioButtonStyles(RadioButton[] buttons) { foreach (var btn in buttons) { btn.Appearance = Appearance.Button; btn.FlatAppearance.CheckedBackColor = accentColorBlue; btn.FlatStyle = FlatStyle.Flat; btn.BackColor = Color.FromArgb(63, 63, 70); btn.FlatAppearance.BorderSize = 0; btn.MinimumSize = new Size(80, 30); } }
        #endregion

        public void UpdateGaugeData(float voltage, float current, int soc, int soh, float[] cellVoltages, float temperature, string status)
        {
            // If this method is called from a non-UI thread (e.g., the pipe listener),
            // marshal the call to the UI thread to prevent cross-threading exceptions.
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateGaugeData(voltage, current, soc, soh, cellVoltages, temperature, status)));
                return;
            }

            // --- Update Progress Bars ---
            // Math.Max/Min ensures the value stays within the ProgressBar's 0-100 range.
            socProgressBar.Value = Math.Max(0, Math.Min(100, soc));
            socProgressBar.CustomText = $"{soc}%";

            sohProgressBar.Value = Math.Max(0, Math.Min(100, soh));
            sohProgressBar.CustomText = $"{soh}%";

            // --- Update Main Pack Value Labels ---
            // Using format specifiers for clean output (e.g., F2 for two decimal places).
            lblPackVoltage.Text = $"{voltage:F2} V";
            lblCurrent.Text = $"{current:F2} A";
            lblPackTemp.Text = $"{temperature:F1} °C";

            // Convert status string to uppercase for consistency and handle null cases.
            lblStatus.Text = status?.ToUpper() ?? "UNKNOWN";

            // --- Dynamically Change Colors Based on Current ---
            if (current > 0.1) // Charging
            {
                lblCurrent.ForeColor = accentColorGreen;
                lblStatus.ForeColor = accentColorGreen;
            }
            else if (current < -0.1) // Discharging
            {
                lblCurrent.ForeColor = accentColorOrange;
                lblStatus.ForeColor = accentColorOrange;
            }
            else // Idle / Resting
            {
                lblCurrent.ForeColor = valueTextColor;
                lblStatus.ForeColor = labelTextColor;
            }

            // --- Update Individual Cell Voltage Labels ---
            if (cellVoltages != null)
            {
                for (int i = 0; i < 10; i++)
                {
                    // Ensure we don't try to access an index that is out of bounds.
                    if (i < cellVoltages.Length)
                    {
                        // Display voltage in millivolts (integer) for better readability.
                        int cellVoltageMv = (int)(cellVoltages[i] * 1000);
                        lblCellVoltages[i].Text = $"{cellVoltageMv}";
                    }
                    else
                    {
                        // If the provided array has fewer than 10 cells, display placeholders.
                        lblCellVoltages[i].Text = "----";
                    }
                }
            }
        }
    }
}