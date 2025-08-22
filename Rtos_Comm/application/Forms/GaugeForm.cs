// FILE: GaugeForm.cs

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public class GaugeForm : Form
    {
        private readonly BatterySimulator _simulator;
        private BatteryState _lastKnownState;

        // --- UI Control Member Variables ---
        private ModernProgressBar socProgressBar;
        private ModernProgressBar sohProgressBar;
        private ModernTrackBar tbCurrent, tbPackTemp, tbCellVoltage;
        private Label lblPackVoltage, lblCurrent, lblPackTemp, lblStatus;
        private Label[] lblCellVoltages = new Label[10];
        private RadioButton rbModeIdle, rbModeCharging, rbModeDischarging;
        private ComboBox cmbSelectedCell, _cmbStatusSelect;
        private Label lblSelectedCellValue;
        private StatusLed _ledCuv, _ledCov, _ledOcd, _ledOcc, _ledOtd, _ledUtc;
        private NumericUpDown _numSoC;
        private NumericUpDown _numSoH;

        private GroupBox _gbStatusFlags;
        private CheckedListBox _clbStatusBits;

        // --- Color Palette ---
        private readonly Color _background = Color.FromArgb(24, 24, 27);
        private readonly Color _panelBackground = Color.FromArgb(39, 39, 42);
        private readonly Color _border = Color.FromArgb(63, 63, 70);
        private readonly Color _textPrimary = Color.FromArgb(244, 244, 245);
        private readonly Color _textSecondary = Color.FromArgb(161, 161, 170);
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


        public GaugeForm(BatterySimulator simulator)
        {
            this.Text = "BMS Gauge & Simulator (BQ78350)";
            this.Size = new Size(1250, 750);
            this.MinimumSize = new Size(920, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = _background;
            this.ForeColor = _textPrimary;
            this.Font = new Font("Segoe UI", 9F);

            this.FormClosed += GaugeForm_FormClosed;
            _simulator = simulator;
            _simulator.StateUpdated += OnSimulatorStateUpdated;

            InitializeLayout();
            InitializeEventHandlers();

            // Apply custom styles to RadioButtons after they are created.
            ApplyRadioButtonStyles(new[] { rbModeIdle, rbModeCharging, rbModeDischarging });
        }

        private void GaugeForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            _simulator.Dispose();
        }
        private void InitializeLayout()
        {
            var mainTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(10) };
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            mainTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            this.Controls.Add(mainTlp);

            // --- LEFT PANEL: MONITOR ---
            var monitorPanel = CreateStyledGroupBox("Live Battery Monitor");
            mainTlp.Controls.Add(monitorPanel, 0, 0);
            var monitorTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(15) };
            monitorTlp.RowStyles.Clear();
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 85F));     // Row 0: SoC/SoH 
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));     // Row 1: Voltage/Current 
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 95F));     // Row 2: Temp/Status 
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));      // Row 3: Cell Voltages 
            monitorTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));      // Row 4: Safety Status 
            monitorPanel.Controls.Add(monitorTlp);

            socProgressBar = new ModernProgressBar { Value = 0, ForeColor = accentColorGreen, Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 15) };
            sohProgressBar = new ModernProgressBar { Value = 100, ForeColor = accentColorGreen, Dock = DockStyle.Fill, Margin = new Padding(5, 5, 5, 15) };
            monitorTlp.Controls.Add(CreateLabeledControl("State of Charge (SoC)", socProgressBar), 0, 0);
            monitorTlp.Controls.Add(CreateLabeledControl("State of Health (SoH)", sohProgressBar), 1, 0);

            lblPackVoltage = CreateValueLabel("-.-- V", accentColorCyan);
            lblCurrent = CreateValueLabel("-.-- A", accentColorCyan);
            lblPackTemp = CreateValueLabel("--.- °C", accentColorCyan);
            lblStatus = new Label { Text = "IDLE", Font = new Font("Segoe UI", 12F, FontStyle.Bold), ForeColor = _textSecondary, Dock = DockStyle.Fill, TextAlign = ContentAlignment.BottomLeft };
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
                lblCellVoltages[i] = new Label { Text = "----", Font = new Font("Consolas", 12F), ForeColor = textColor, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter };
                cellTlp.Controls.Add(CreateLabeledControl($"Cell {i + 1}", lblCellVoltages[i]), i % 5, i / 5);
            }

            var safetyPanel = CreateStyledGroupBox("Safety Status");
            monitorTlp.SetColumnSpan(safetyPanel, 2);
            monitorTlp.Controls.Add(safetyPanel, 0, 4); ;
            var safetyTlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 3,
                Padding = new Padding(8, 5, 5, 5)
            };
            safetyTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 25F));
            safetyTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            safetyTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 25F));
            safetyTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            for (int i = 0; i < 3; i++) safetyTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            safetyPanel.Controls.Add(safetyTlp);

            _ledCov = new StatusLed { ActiveColor = accentColorRed };
            _ledCuv = new StatusLed { ActiveColor = accentColorRed };
            _ledOcc = new StatusLed { ActiveColor = accentColorOrange };
            _ledOcd = new StatusLed { ActiveColor = accentColorOrange };
            _ledOtd = new StatusLed { ActiveColor = accentColorRed };
            _ledUtc = new StatusLed { ActiveColor = accentColorRed };

            AddStatusRow(safetyTlp, _ledCov, "Cell Overvoltage (COV)", 0, 0);
            AddStatusRow(safetyTlp, _ledCuv, "Cell Undervoltage (CUV)", 1, 0);
            AddStatusRow(safetyTlp, _ledOcd, "Overcurrent Discharge (OCD)", 2, 0);

            AddStatusRow(safetyTlp, _ledOcc, "Overcurrent Charge (OCC)", 0, 2);
            AddStatusRow(safetyTlp, _ledOtd, "Overtemperature Discharge (OTD)", 1, 2);
            AddStatusRow(safetyTlp, _ledUtc, "Undertemperature Charge (UTC)", 2, 2);

            // --- RIGHT PANEL: CONTROLS ---
            var controlPanel = CreateStyledGroupBox("Simulation Control & Fault Injection");
            mainTlp.Controls.Add(controlPanel, 1, 0);

            var controlTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, Padding = new Padding(15) };
            controlPanel.Controls.Add(controlTlp);

            // --- Row Definitions ---
            controlTlp.RowStyles.Clear();
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Mode
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Current
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Temp
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // SoC
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // SoH
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Cell Selection
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Cell Slider
            controlTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Spacer

            _gbStatusFlags = new GroupBox
            {
                Text = "Manual Status Flag Control",
                Dock = DockStyle.Fill,
                Padding = new Padding(10),
                Margin = new Padding(0, 15, 0, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _textSecondary
            };

            controlTlp.RowStyles[controlTlp.RowStyles.Count - 1] = new RowStyle(SizeType.AutoSize);
            controlTlp.Controls.Add(_gbStatusFlags, 0, controlTlp.RowCount - 1);

            var flagsTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Height = 150 };
            flagsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            flagsTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            _gbStatusFlags.Controls.Add(flagsTlp);

            _cmbStatusSelect = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Top };
            _clbStatusBits = new CheckedListBox { Dock = DockStyle.Fill, BackColor = _panelBackground, ForeColor = _textPrimary, BorderStyle = BorderStyle.None };

            flagsTlp.Controls.Add(_cmbStatusSelect, 0, 0);
            flagsTlp.Controls.Add(_clbStatusBits, 0, 1);

            // --- Helper function for creating rows ---
            var createRow = new Func<string, Control, FlowLayoutPanel>((labelText, control) =>
            {
                var flowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false
                };
                var label = new Label
                {
                    Text = labelText,
                    Width = 170, // Fixed width for alignment
                    Dock = DockStyle.None,
                    Anchor = AnchorStyles.Left,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                    Height = 30 // Set a fixed height
                };
                control.Dock = DockStyle.Fill;
                flowPanel.Controls.Add(label);
                flowPanel.Controls.Add(control);
                // Automatically set the width of the control to fill the rest of the panel
                flowPanel.Resize += (s, e) =>
                {
                    if (flowPanel.Controls.Count > 1)
                        flowPanel.Controls[1].Width = flowPanel.ClientSize.Width - flowPanel.Controls[0].Width - 5;
                };
                return flowPanel;
            });


            // Row 0: Simulation Mode
            rbModeIdle = new RadioButton { Text = "Idle", Checked = true, AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            rbModeCharging = new RadioButton { Text = "Charging", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            rbModeDischarging = new RadioButton { Text = "Discharging", AutoSize = true, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            var modePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true };
            modePanel.Controls.AddRange(new Control[] { rbModeIdle, rbModeCharging, rbModeDischarging });
            controlTlp.Controls.Add(createRow("Simulation Mode", modePanel), 0, 0);

            // Row 1: Set Current
            tbCurrent = CreateSlider(-50000, 50000, 0);
            controlTlp.Controls.Add(createRow("Set Current (mA)", tbCurrent), 0, 1);

            // Row 2: Set Pack Temp
            tbPackTemp = CreateSlider(-80, 80, 25);
            controlTlp.Controls.Add(createRow("Set Pack Temp (°C)", tbPackTemp), 0, 2);

            // Row 3: Set SoC
            _numSoC = CreateNumericControl(0, 100, 50);
            controlTlp.Controls.Add(createRow("Set SoC (%)", _numSoC), 0, 3);

            // Row 4: Set SoH
            _numSoH = CreateNumericControl(0, 100, 98);
            controlTlp.Controls.Add(createRow("Set SoH (%)", _numSoH), 0, 4);

            // --- Individual Cell Controls (split into two rows) ---

            // Row 5: Cell Selection
            cmbSelectedCell = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 120, // Give it a bit more space
                BackColor = panelColor,
                ForeColor = textColor,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F)
            };
            for (int i = 0; i < 10; i++) cmbSelectedCell.Items.Add($"Cell {i + 1}");
            cmbSelectedCell.SelectedIndex = 0;
            controlTlp.Controls.Add(createRow("Set Individual Cell", cmbSelectedCell), 0, 5);

            // Row 6: Cell Voltage Slider
            var sliderWithValuePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            sliderWithValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            sliderWithValuePanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tbCellVoltage = CreateSlider(2500, 4500, 3650);
            lblSelectedCellValue = new Label { Text = "3650 mV", Width = 80, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            sliderWithValuePanel.Controls.Add(tbCellVoltage, 0, 0);
            sliderWithValuePanel.Controls.Add(lblSelectedCellValue, 1, 0);
            // Add the slider panel to the grid, but with an empty label
            controlTlp.Controls.Add(createRow("", sliderWithValuePanel), 0, 6);
        }

        #region Helper Methods

        private NumericUpDown CreateNumericControl(int min, int max, int value)
        {
            var numeric = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Dock = DockStyle.Fill,
                BackColor = _panelBackground,
                ForeColor = _textPrimary,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = HorizontalAlignment.Center
            };
            return numeric;
        }

        private void AddStatusRow(TableLayoutPanel tlp, StatusLed led, string text, int row, int startColumn)
        {
            tlp.Controls.Add(led, startColumn, row);
            tlp.Controls.Add(new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, startColumn + 1, row);
        }
        private GroupBox CreateStyledGroupBox(string title)
        {
            var groupBox = new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                Padding = new Padding(15), // Increased padding for a cleaner look
                Margin = new Padding(5),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = _textSecondary // Use the modern text color
            };

            // This is the key to a flat, modern look without a custom control.
            groupBox.Paint += (sender, e) =>
            {
                GroupBox box = sender as GroupBox;
                // Draw the background
                e.Graphics.Clear(this.BackColor); // Clear the area with the form's background color

                // Draw the main panel background
                using (var backBrush = new SolidBrush(_panelBackground))
                {
                    e.Graphics.FillRectangle(backBrush, 1, box.Font.Height / 2, box.Width - 2, box.Height - (box.Font.Height / 2) - 1);
                }

                // Draw the border
                using (var borderPen = new Pen(_border))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, box.Font.Height / 2, box.Width - 1, box.Height - (box.Font.Height / 2) - 1);
                }

                // Draw the title text
                using (var textBrush = new SolidBrush(_textSecondary))
                {
                    var textSize = e.Graphics.MeasureString(box.Text, box.Font);
                    // Draw a small rectangle behind the text to cover the top border line
                    e.Graphics.FillRectangle(new SolidBrush(this.BackColor), 10, 0, textSize.Width + 4, box.Font.Height);
                    e.Graphics.DrawString(box.Text, box.Font, textBrush, 12, 0);
                }
            };

            return groupBox;
        }
        private TableLayoutPanel CreateLabeledControl(string labelText, Control control) { var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(3) }; tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize)); tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); var label = new Label { Text = labelText, AutoSize = true, Dock = DockStyle.Top, Font = new Font("Segoe UI", 9F, FontStyle.Italic), ForeColor = Color.Gray }; tlp.Controls.Add(label, 0, 0); tlp.Controls.Add(control, 0, 1); return tlp; }
        private TableLayoutPanel CreateLabeledControl(string labelText, Control control, int labelWidth) { var tlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 5, 0, 5) }; tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth)); tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); var label = new Label { Text = labelText, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Font = new Font("Segoe UI", 10F, FontStyle.Regular) }; tlp.Controls.Add(label, 0, 0); tlp.Controls.Add(control, 1, 0); return tlp; }
        private Label CreateValueLabel(string initialText, Color color) { return new Label { Text = initialText, Font = new Font("Consolas", 20F, FontStyle.Bold), ForeColor = color, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight }; }
        private ModernTrackBar CreateSlider(int min, int max, int value)
        {
            return new ModernTrackBar
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0),
                TrackColor = _border,
                ThumbColor = accentColorBlue,
                TrackProgressColor = accentColorBlue
            };
        }
        private void ApplyRadioButtonStyles(RadioButton[] buttons) { foreach (var btn in buttons) { btn.Appearance = Appearance.Button; btn.FlatAppearance.CheckedBackColor = accentColorBlue; btn.FlatStyle = FlatStyle.Flat; btn.BackColor = Color.FromArgb(63, 63, 70); btn.FlatAppearance.BorderSize = 0; btn.MinimumSize = new Size(80, 30); } }
        #endregion

        private void OnSimulatorStateUpdated(BatteryState state)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnSimulatorStateUpdated(state)));
                return;
            }

            _lastKnownState = state;
            UpdateCheckedListBoxFromState();

            UpdateGaugeData(
                state.PackVoltage_V,
                state.Current_A,
                state.SoC,
                state.SoH,
                state.CellVoltages_V,
                state.Temperature_C
            );
        }

        private void UpdateCheckedListBoxFromState()
        {
            if (_simulator == null || _cmbStatusSelect.SelectedItem == null) return;
            OnStatusRegisterSelected(this, EventArgs.Empty);
        }

        private void InitializeEventHandlers()
        {
            // Sliders (tbPackVoltage artık simülatör tarafından kontrol edilecek)
            tbCurrent.ValueChanged += (s, e) => _simulator.SetCurrent(tbCurrent.Value);
            tbPackTemp.ValueChanged += (s, e) => _simulator.SetTemperature(tbPackTemp.Value);
            tbCellVoltage.ValueChanged += OnIndividualCellSliderChanged;
            cmbSelectedCell.SelectedIndexChanged += OnSelectedCellChanged;
            _numSoC.ValueChanged += (s, e) => _simulator.SetSoC((int)_numSoC.Value);
            _numSoH.ValueChanged += (s, e) => _simulator.SetSoH((int)_numSoH.Value);
            _cmbStatusSelect.SelectedIndexChanged += OnStatusRegisterSelected;
            _clbStatusBits.ItemCheck += OnStatusBitChanged;

            PopulateStatusSelector();

            tbCellVoltage.ValueChanged += OnIndividualCellSliderChanged;

            // Send initial values to the simulator
            _simulator.SetCurrent(tbCurrent.Value);
            _simulator.SetTemperature(tbPackTemp.Value);
            _simulator.SetSoC((int)_numSoC.Value);
            _simulator.SetSoH((int)_numSoH.Value);
        }
        private void PopulateStatusSelector()
        {
            _cmbStatusSelect.Items.Clear();
            _cmbStatusSelect.Items.Add("Safety Status");
            _cmbStatusSelect.Items.Add("Charging Status");
            _cmbStatusSelect.Items.Add("Operation Status");
            _cmbStatusSelect.Items.Add("Gauging Status");
            _cmbStatusSelect.Items.Add("Battery Status");
            _cmbStatusSelect.Items.Add("Manufacturing Status");
            _cmbStatusSelect.Items.Add("PF Status");
            _cmbStatusSelect.SelectedIndex = 0;
        }

        private void OnStatusRegisterSelected(object sender, EventArgs e)
        {
            if (_simulator == null || _cmbStatusSelect.SelectedItem == null) return;

            _clbStatusBits.ItemCheck -= OnStatusBitChanged;

            _clbStatusBits.Items.Clear();
            string selected = _cmbStatusSelect.SelectedItem.ToString();

            var currentRegs = _simulator.CurrentState.BmsRegisters;

            Type enumType = null;
            ulong currentFlags = 0;

            if (selected == "Safety Status") { enumType = typeof(SafetyStatus); currentFlags = currentRegs.safety_status; }
            else if (selected == "Charging Status") { enumType = typeof(ChargingStatus); currentFlags = currentRegs.charging_status; }
            else if (selected == "Operation Status") { enumType = typeof(OperationStatus); currentFlags = currentRegs.operation_status; }
            else if (selected == "Gauging Status") { enumType = typeof(GaugingStatus); currentFlags = currentRegs.gauging_status; }
            else if (selected == "Battery Status") { enumType = typeof(BatteryStatus); currentFlags = currentRegs.battery_status; }
            else if (selected == "Manufacturing Status") { enumType = typeof(ManufacturingStatus); currentFlags = currentRegs.manufacturing_status; }
            else if (selected == "PF Status") { enumType = typeof(PFStatus); currentFlags = currentRegs.pf_status; }

            if (enumType != null)
            {
                foreach (var name in Enum.GetNames(enumType))
                {
                    if (name == "None")
                        continue;

                    var flagValue = (ulong)Convert.ChangeType(Enum.Parse(enumType, name), typeof(ulong));

                    bool isChecked = (currentFlags & flagValue) == flagValue;

                    _clbStatusBits.Items.Add(name, isChecked);
                }
            }

            _clbStatusBits.ItemCheck += OnStatusBitChanged;
        }

        private void OnStatusBitChanged(object sender, ItemCheckEventArgs e)
        {
            this.BeginInvoke((MethodInvoker)delegate
            {
                string selectedRegister = _cmbStatusSelect.SelectedItem.ToString();
                ulong flags = 0;

                foreach (var item in _clbStatusBits.CheckedItems)
                {
                    try
                    {
                        if (selectedRegister == "Safety Status")
                        {
                            SafetyStatus flag = (SafetyStatus)Enum.Parse(typeof(SafetyStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                        else if (selectedRegister == "Charging Status")
                        {
                            ChargingStatus flag = (ChargingStatus)Enum.Parse(typeof(ChargingStatus), item.ToString());
                            flags |= (ushort)flag;
                        }
                        else if (selectedRegister == "Operation Status")
                        {
                            OperationStatus flag = (OperationStatus)Enum.Parse(typeof(OperationStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                        else if (selectedRegister == "Gauging Status")
                        {
                            GaugingStatus flag = (GaugingStatus)Enum.Parse(typeof(GaugingStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                        else if (selectedRegister == "Battery Status")
                        {
                            BatteryStatus flag = (BatteryStatus)Enum.Parse(typeof(BatteryStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                        else if (selectedRegister == "Manufacturing Status")
                        {
                            ManufacturingStatus flag = (ManufacturingStatus)Enum.Parse(typeof(ManufacturingStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                        else if (selectedRegister == "PF Status")
                        {
                            PFStatus flag = (PFStatus)Enum.Parse(typeof(PFStatus), item.ToString());
                            flags |= (uint)flag;
                        }
                    }
                    catch (ArgumentException)
                    {
                        continue;
                    }
                }

                _simulator.SetStatusFlags(selectedRegister, flags);
            });
        }
        private void OnSelectedCellChanged(object sender, EventArgs e)
        {
            if (_lastKnownState == null) return;

            int cellIndex = cmbSelectedCell.SelectedIndex;
            if (cellIndex < 0 || cellIndex >= _lastKnownState.CellVoltages_V.Length) return;

            // Slider'ı, seçilen hücrenin mevcut voltajını yansıtacak şekilde güncelle.
            // Bu, simülatöre tekrar komut göndermez, sadece UI'ı senkronize eder.
            int currentVoltageMv = (int)(_lastKnownState.CellVoltages_V[cellIndex] * 1000);

            // Olay döngüsünü kırmak için geçici olarak ValueChanged olayını devreden çıkar.
            tbCellVoltage.ValueChanged -= OnIndividualCellSliderChanged;
            tbCellVoltage.Value = currentVoltageMv;
            tbCellVoltage.ValueChanged += OnIndividualCellSliderChanged;
        }

        /// <summary>
        /// Kullanıcı bireysel hücre voltajı slider'ını hareket ettirdiğinde tetiklenir.
        /// </summary>
        private void OnIndividualCellSliderChanged(object sender, EventArgs e)
        {
            int cellIndex = cmbSelectedCell.SelectedIndex;
            int voltageMv = tbCellVoltage.Value;
            _simulator.SetIndividualCellVoltage(cellIndex, voltageMv);
        }

        public void UpdateGaugeData(float voltage, float current, int soc, int soh, float[] cellVoltages, float temperature)
        {
            // If this method is called from a non-UI thread (e.g., the pipe listener),
            // marshal the call to the UI thread to prevent cross-threading exceptions.
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateGaugeData(voltage, current, soc, soh, cellVoltages, temperature)));
                return;
            }

            string status;
            if (current > 0.05)
                status = "CHARGING";
            else if (current < -0.05)
                status = "DISCHARGING";
            else
                status = "IDLE";

            lblStatus.Text = status;
            switch (status)
            {
                case "CHARGING":
                    rbModeCharging.Checked = true;
                    break;
                case "DISCHARGING":
                    rbModeDischarging.Checked = true;
                    break;
                default: // IDLE
                    rbModeIdle.Checked = true;
                    break;
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

            lblStatus.Text = status?.ToUpper() ?? "UNKNOWN";

            // --- Dynamically Change Colors Based on Current ---
            if (current > 0.1) // Charging
            {
                lblCurrent.ForeColor = accentColorGreen;
                lblStatus.ForeColor = accentColorGreen; // Status rengini de güncelle
            }
            else if (current < -0.1) // Discharging
            {
                lblCurrent.ForeColor = accentColorOrange;
                lblStatus.ForeColor = accentColorOrange; // Status rengini de güncelle
            }
            else // Idle / Resting
            {
                lblCurrent.ForeColor = _textPrimary; // valueTextColor
                lblStatus.ForeColor = _textSecondary;  // labelTextColor
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

            if (_lastKnownState?.BmsRegisters != null)
            {
                var safetyFlags = (SafetyStatus)_lastKnownState.BmsRegisters.safety_status;
                _ledCov.Active = safetyFlags.HasFlag(SafetyStatus.COV);
                _ledCuv.Active = safetyFlags.HasFlag(SafetyStatus.CUV);
                _ledOcd.Active = safetyFlags.HasFlag(SafetyStatus.OCD);
                _ledOcc.Active = safetyFlags.HasFlag(SafetyStatus.OCC);
                _ledOtd.Active = safetyFlags.HasFlag(SafetyStatus.OTD);
                _ledUtc.Active = safetyFlags.HasFlag(SafetyStatus.UTC);

            }
        }
    }
}