using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Rtos_Comm
{
    #region RoundButton Class
    public class RoundButton : Button
    {
        private int cornerRadius = 20;

        [System.ComponentModel.DefaultValue(20)]
        public int CornerRadius
        {
            get => cornerRadius;
            set { cornerRadius = value; UpdateRegion(); } // Region'ı güncelle ve yeniden çiz
        }

        public RoundButton()
        {
            // Titreşimi önlemek ve çizim performansını artırmak için
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        private GraphicsPath GetRoundPath(Rectangle rect, int radius)
        {
            // Köşe yarıçapının, buton boyutunun yarısından büyük olmamasını sağla
            if (radius > Math.Min(rect.Width, rect.Height) / 2)
                radius = Math.Min(rect.Width, rect.Height) / 2;

            GraphicsPath path = new GraphicsPath();
            if (radius <= 0)
            {
                path.AddRectangle(rect);
            }
            else
            {
                // Pürüzsüz kenarlar için -1 piksel küçültme
                rect.Width--;
                rect.Height--;
                path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
                path.AddArc(rect.Right - (radius * 2), rect.Y, radius * 2, radius * 2, 270, 90);
                path.AddArc(rect.Right - (radius * 2), rect.Bottom - (radius * 2), radius * 2, radius * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - (radius * 2), radius * 2, radius * 2, 90, 90);
                path.CloseFigure();
            }
            return path;
        }

        // Butonun tıklanabilir alanını (Region) ayarlar.
        // Bu, OnPaint içinde sürekli çağrılmak yerine sadece gerektiğinde çağrılır.
        private void UpdateRegion()
        {
            this.Region = new Region(GetRoundPath(this.ClientRectangle, this.CornerRadius));
            this.Invalidate(); // Değişikliğin görünmesi için kontrolü yeniden çizmeye zorla
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRegion(); // Boyut değiştiğinde Region'ı yeniden hesapla
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // base.OnPaint(e) ÇAĞIRILMAZ! Bu, pikselli kenarların ana sebebidir.
            // Biz butonun tamamını kendimiz çiziyoruz.

            // Arka planı temizle
            using (var brush = new SolidBrush(this.Parent.BackColor))
            {
                e.Graphics.FillRectangle(brush, this.ClientRectangle);
            }

            // En yüksek çizim kalitesini ayarla
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.CompositingQuality = CompositingQuality.HighQuality;
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            // Butonun gövdesini çiz
            using (var path = GetRoundPath(this.ClientRectangle, this.CornerRadius))
            using (var brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillPath(brush, path);

                // Kenarlık çiz
                if (this.FlatAppearance.BorderSize > 0)
                {
                    using (var pen = new Pen(this.FlatAppearance.BorderColor, this.FlatAppearance.BorderSize))
                    {
                        // Pen'in kalınlığından dolayı oluşacak taşmayı önlemek için hafifçe içeri
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }

            // Metni çiz (TextRenderer yerine Graphics.DrawString daha iyi Anti-Aliasing sağlar)
            TextRenderer.DrawText(e.Graphics,
                this.Text,
                this.Font,
                this.ClientRectangle,
                this.ForeColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }
    #endregion

    partial class MainForm : Form
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            #region UI Code (Unchanged)

            this.components = new System.ComponentModel.Container();
            this.SuspendLayout();
            this.Text = "RTOS Communication Panel";
            this.ClientSize = new Size(950, 600);
            this.MinimumSize = new Size(950, 600);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, ((byte)(0)));
            this.BackColor = Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(242)))), ((int)(((byte)(245)))));

            var mainTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10) };
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize)); mainTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainTlp);

            var groupBoxConnection = new GroupBox { Text = "Connection", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Padding = new Padding(20), AutoSize = true };
            mainTlp.Controls.Add(groupBoxConnection, 0, 0);

            var connectionTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, AutoSize = true };
            connectionTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F)); connectionTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); connectionTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F)); connectionTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            connectionTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            groupBoxConnection.Controls.Add(connectionTlp);
            this.btnLoadXml = CreateStyledRoundButton("Browse Configuration...", Color.FromArgb(108, 117, 125), Color.White); this.btnLoadXml.Click += this.btnLoadXml_Click; this.btnLoadXml.Dock = DockStyle.Fill;
            this.lblXmlStatus = new Label { Text = "No configuration file loaded.", ForeColor = Color.DimGray, Font = new Font("Segoe UI", 11F, FontStyle.Italic), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(10, 0, 0, 0) };
            this.Connect_Button = CreateStyledRoundButton("Connect", Color.FromArgb(40, 167, 69), Color.White); this.Connect_Button.Click += this.Connect_Button_Click; this.Connect_Button.Dock = DockStyle.Fill;
            this.Disconnect_Button = CreateStyledRoundButton("Disconnect", Color.FromArgb(220, 53, 69), Color.White); this.Disconnect_Button.Click += this.Disconnect_Button_Click; this.Disconnect_Button.Dock = DockStyle.Fill;
            connectionTlp.Controls.Add(this.btnLoadXml, 0, 0); connectionTlp.Controls.Add(this.lblXmlStatus, 1, 0); connectionTlp.Controls.Add(this.Connect_Button, 2, 0); connectionTlp.Controls.Add(this.Disconnect_Button, 3, 0);

            var contentTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            contentTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F)); contentTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTlp.Controls.Add(contentTlp, 0, 1);

            var leftPaneTlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, Padding = new Padding(0, 5, 0, 0) };
            leftPaneTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 70F)); leftPaneTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            contentTlp.Controls.Add(leftPaneTlp, 0, 0);

            var groupBoxActions = new GroupBox { Text = "Protocols & Utilities", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };

            var actionButtonsTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(8) };
            for (int i = 0; i < 5; i++) actionButtonsTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));

            groupBoxActions.Controls.Add(actionButtonsTlp);

            this.CANButton = CreateStyledRoundButton("CAN Protocol", Color.FromArgb(0, 123, 255), Color.White); this.CANButton.Dock = DockStyle.Fill; this.CANButton.Click += this.CANButton_Click;
            this.SPIButton = CreateStyledRoundButton("SPI Protocol", Color.FromArgb(0, 123, 255), Color.White); this.SPIButton.Dock = DockStyle.Fill; this.SPIButton.Click += this.SPIButton_Click;
            this.I2CButton = CreateStyledRoundButton("I2C Protocol", Color.FromArgb(0, 123, 255), Color.White); this.I2CButton.Dock = DockStyle.Fill; this.I2CButton.Click += this.I2CButton_Click;
            this.FlashButton = CreateStyledRoundButton("Flash Utility", Color.FromArgb(23, 162, 184), Color.White); this.FlashButton.Dock = DockStyle.Fill; this.FlashButton.Click += this.FlashButton_Click;
            this.MonitorButton = CreateStyledRoundButton("Data Monitor", Color.FromArgb(23, 162, 184), Color.White); this.MonitorButton.Dock = DockStyle.Fill; this.MonitorButton.Click += this.MonitorButton_Click;
            actionButtonsTlp.Controls.Add(this.CANButton, 0, 0); actionButtonsTlp.Controls.Add(this.SPIButton, 0, 1); actionButtonsTlp.Controls.Add(this.I2CButton, 0, 2); actionButtonsTlp.Controls.Add(this.FlashButton, 0, 3); actionButtonsTlp.Controls.Add(this.MonitorButton, 0, 4);

            var groupBoxSettings = new GroupBox { Text = "Settings", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Margin = new Padding(0, 5, 0, 0) };
            leftPaneTlp.Controls.Add(groupBoxActions, 0, 0); leftPaneTlp.Controls.Add(groupBoxSettings, 0, 1);

            var settingsTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 3, Padding = new Padding(8) };

            settingsTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            settingsTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            settingsTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            settingsTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            settingsTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            settingsTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            groupBoxSettings.Controls.Add(settingsTlp);

            this.SendInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            this.SendInterval.Items.AddRange(new object[] { "50", "100", "250", "500", "1000", "5000" });
            this.SendInterval.SelectedIndex = 4;

            settingsTlp.Controls.Add(new Label { Text = "Interval:", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 1);
            settingsTlp.Controls.Add(this.SendInterval, 1, 1);
            settingsTlp.Controls.Add(new Label { Text = "ms", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 2, 1);

            var groupBoxData = new GroupBox { Text = "System Data Monitor", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Padding = new Padding(5), Margin = new Padding(5, 5, 0, 0) };
            contentTlp.Controls.Add(groupBoxData, 1, 0);

            var dataTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 6 };
            dataTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dataTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); dataTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dataTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
            for (int i = 0; i < 5; i++) dataTlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            dataTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            groupBoxData.Controls.Add(dataTlp);
            Func<string, Label> createDataLabel = (text) => new Label { Text = text, Font = new Font("Segoe UI", 10F, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, AutoSize = true };
            this.ADCMinBox = new TextBox { Text = "Min", ForeColor = Color.Gray, Dock = DockStyle.Fill }; this.ADCMaxBox = new TextBox { Text = "Max", ForeColor = Color.Gray, Dock = DockStyle.Fill };

            /*********************************** ADC ***********************************************/

            var adcMinMaxPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(0, 3, 0, 3) };
            adcMinMaxPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); adcMinMaxPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            adcMinMaxPanel.Controls.Add(this.ADCMinBox, 0, 0); adcMinMaxPanel.Controls.Add(this.ADCMaxBox, 1, 0);
            this.ADCButton = CreateSetDataButton(); this.ADCButton.Click += this.ADCButton_Click;
            this.ADCSelectCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Italic), Margin = new Padding(3) };
            dataTlp.Controls.Add(createDataLabel("ADC Value:"), 0, 0); dataTlp.Controls.Add(adcMinMaxPanel, 1, 0); dataTlp.Controls.Add(this.ADCButton, 2, 0); dataTlp.Controls.Add(this.ADCSelectCombo, 3, 0);

            /***************************************************************************************/

            /************************************ IO ***********************************************/
            this.IOButton = CreateSetDataButton(); this.IOButton.Text = "TOGGLE"; this.IOButton.Click += this.IOButton_Click;
            var ioSelectionPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            ioSelectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ioSelectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            ioSelectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            ioSelectionPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            this.numIoPort = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 11,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(3, 6, 8, 6)
            };

            this.numIoPin = new NumericUpDown
            {
                Minimum = 0,
                Maximum = 15,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 6, 3, 6),
                TextAlign = HorizontalAlignment.Center
            };

            ioSelectionPanel.Controls.Add(new Label { Text = "Port", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10F) }, 0, 0);
            ioSelectionPanel.Controls.Add(this.numIoPort, 1, 0);
            ioSelectionPanel.Controls.Add(new Label { Text = "Pin", AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleRight, Font = new Font("Segoe UI", 10F), Margin = new Padding(10, 0, 0, 0) }, 2, 0);
            ioSelectionPanel.Controls.Add(this.numIoPin, 3, 0);

            dataTlp.Controls.Add(createDataLabel("IO Pin:"), 0, 1);
            dataTlp.Controls.Add(new Panel(), 1, 1);
            dataTlp.Controls.Add(this.IOButton, 2, 1); 
            dataTlp.Controls.Add(ioSelectionPanel, 3, 1);

            /***************************************************************************************/

            /*********************************** RTC ***********************************************/

            this.RTCBox = new TextBox { Dock = DockStyle.Fill, Anchor = AnchorStyles.None, Height = 26 }; this.RTCButton = CreateSetDataButton(); this.RTCButton.Click += this.RTCButton_Click;
            dataTlp.Controls.Add(createDataLabel("RTC Time:"), 0, 2); dataTlp.Controls.Add(this.RTCBox, 1, 2); dataTlp.Controls.Add(this.RTCButton, 2, 2);

            /***************************************************************************************/

            /*********************************** IRQ ***********************************************/

            this.IRQBox = new TextBox { Dock = DockStyle.Fill, Anchor = AnchorStyles.None, Height = 26 }; this.IRQButton = CreateSetDataButton(); this.IRQButton.Click += this.IRQButton_Click;
            this.IRQSelectCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Italic), Margin = new Padding(3) };
            this.IRQSelectCombo.SelectedIndexChanged += IRQSelectCombo_SelectedIndexChanged;
            dataTlp.Controls.Add(createDataLabel("IRQ Count:"), 0, 3); dataTlp.Controls.Add(this.IRQBox, 1, 3); dataTlp.Controls.Add(this.IRQButton, 2, 3); dataTlp.Controls.Add(this.IRQSelectCombo, 3, 3);

            /***************************************************************************************/

            /*********************************** GPT ***********************************************/
            this.GPTBox = new TextBox { Dock = DockStyle.Fill, Anchor = AnchorStyles.None, Height = 26 }; this.GPTButton = CreateSetDataButton(); this.GPTButton.Click += this.GPTButton_Click;
            this.GPTSelectCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F, FontStyle.Italic), Margin = new Padding(3) };
            dataTlp.Controls.Add(createDataLabel("GPT Counter:"), 0, 4); dataTlp.Controls.Add(this.GPTBox, 1, 4); dataTlp.Controls.Add(this.GPTButton, 2, 4); dataTlp.Controls.Add(this.GPTSelectCombo, 3, 4);
            this.ResumeLayout(false);
            #endregion

            /***************************************************************************************/
        }

        #endregion

        #region Component and Control Declarations

        // UI Controls
        private TextBox ADCMinBox, ADCMaxBox, IOBox, RTCBox, IRQBox, GPTBox;
        private ComboBox ADCSelectCombo, IRQSelectCombo, GPTSelectCombo, SendInterval;
        private RoundButton ADCButton, IOButton, RTCButton, IRQButton, GPTButton;
        private RoundButton Connect_Button, Disconnect_Button, btnLoadXml;
        private RoundButton CANButton, SPIButton, I2CButton, FlashButton, MonitorButton;
        private Label lblXmlStatus;
        private NumericUpDown numIoPort;
        private NumericUpDown numIoPin;

        // Dynamic CAN Window Controls
        private TextBox textBoxId, textBoxDlc;
        private TextBox[] dataBytes = new TextBox[8];
        private Button sendButton, autoStart;
        private ComboBox CanTimeCombo;

        // Modeless Form References
        private Form canForm;
        private Form flashForm;
        private Form spiFlashForm;
        private Form monitorForm;

        #endregion

        #region Helper Methods

        private Panel CreateStyledInputPanel(Control control, Color borderColor, Color focusColor)
        {
            if (control is TextBoxBase textBox)
            {
                textBox.BorderStyle = BorderStyle.None;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.FlatStyle = FlatStyle.Flat;
            }

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(0);

            var panel = new Panel
            {
                BackColor = borderColor,
                Padding = new Padding(2),
                Margin = new Padding(3, 6, 3, 6),
                Dock = DockStyle.Fill
            };

            panel.Controls.Add(control);

            control.Enter += (s, e) => { panel.BackColor = focusColor; };
            control.Leave += (s, e) => { panel.BackColor = borderColor; };

            return panel;
        }

        private RoundButton CreateSetDataButton()
        {
            var btn = new RoundButton { Text = "SET", BackColor = Color.Gainsboro, ForeColor = Color.Black, Font = new Font("Segoe UI", 8F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Size = new Size(60, 28), Anchor = AnchorStyles.None, Margin = new Padding(8, 0, 8, 0), CornerRadius = 6 };
            btn.FlatAppearance.BorderSize = 0; Color originalColor = btn.BackColor;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(originalColor);
            btn.MouseLeave += (s, e) => btn.BackColor = originalColor;
            return btn;
        }

        private RoundButton CreateStyledRoundButton(string text, Color backColor, Color foreColor)
        {
            var btn = new RoundButton { Text = text, BackColor = backColor, ForeColor = foreColor, Font = new Font("Segoe UI", 10F, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Margin = new Padding(5), CornerRadius = 8 };
            btn.FlatAppearance.BorderSize = 0; Color originalColor = backColor;
            btn.MouseEnter += (s, e) => btn.BackColor = ControlPaint.Light(originalColor);
            btn.MouseLeave += (s, e) => btn.BackColor = originalColor;
            return btn;
        }
        #endregion

        #region Event Handlers

        private void GPTButton_Click(object sender, EventArgs e) { MessageBox.Show("GET logic for GPT counter should be implemented here.", "GPT", MessageBoxButtons.OK, MessageBoxIcon.Information); }

        private void FlashButton_Click(object sender, EventArgs e)
        {
            if (flashForm == null || flashForm.IsDisposed)
            {
                // TODO GA: Make relative path.
                string dataFlashPath = @"C:\Users\GOKHANAKK\Documents\Visual Studio 2022\Projects\e_bike_simulator\threadx\ports\win32\vs_2019\example_build\Battery_Simulator\dataflash.bin";

                flashForm = new FlashViewerForm("Internal Data Flash Utility", dataFlashPath);
                flashForm.Show(this);
            }
            else
            {
                flashForm.Activate(); // Bring to front if already open
            }
        }

        private void MonitorButton_Click(object sender, EventArgs e)
        {
            if (monitorForm == null || monitorForm.IsDisposed)
            {
                monitorForm = new Form
                {
                    Text = "Data Monitor",
                    Size = new Size(800, 600),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.Sizable,
                    Controls = { new Label { Text = "Data Monitor Window - Content to be implemented.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter } }
                };
                monitorForm.Show(this);
            }
            else
            {
                monitorForm.Activate(); // Bring to front if already open
            }
        }

        private void CANButton_Click(object sender, EventArgs e)
        {
            if (canForm == null || canForm.IsDisposed)
            {
                canForm = new Form
                {
                    Text = "CAN Message Configuration",
                    Size = new Size(520, 420),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    Padding = new Padding(15),
                    Font = new Font("Segoe UI", 9F)
                };

                // Add an event handler to nullify the form reference when it's closed by the user
                canForm.FormClosed += (s, args) => { canForm = null; };

                var formTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
                formTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                formTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
                formTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                canForm.Controls.Add(formTlp);

                var topTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = new Padding(0, 0, 0, 10), AutoSize = true };
                topTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); topTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                topTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F)); // Spacer
                topTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); topTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                formTlp.Controls.Add(topTlp, 0, 0);

                topTlp.Controls.Add(new Label { Text = "CAN ID (Hex):", Anchor = AnchorStyles.Left, AutoSize = true }, 0, 0);
                this.textBoxId = new TextBox { Text = "0", Dock = DockStyle.Fill };
                topTlp.Controls.Add(this.textBoxId, 1, 0);

                topTlp.Controls.Add(new Label { Text = "Interval (ms):", Anchor = AnchorStyles.Left, AutoSize = true }, 3, 0);
                this.CanTimeCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill };
                this.CanTimeCombo.Items.AddRange(new object[] { "100", "250", "500", "1000", "5000" }); this.CanTimeCombo.SelectedIndex = 3;
                topTlp.Controls.Add(this.CanTimeCombo, 4, 0);

                var dataGroupBox = new GroupBox { Dock = DockStyle.Fill };
                var dataGroupTlp = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = 2 };
                dataGroupTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // DLC controls
                dataGroupTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // Data Bytes
                dataGroupBox.Controls.Add(dataGroupTlp);
                this.textBoxDlc = new TextBox { Text = "8", MaxLength = 1 }; this.textBoxDlc.TextChanged += TextBoxDlc_TextChanged;
                var dlcPanel = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10, 15, 10, 10), WrapContents = false, AutoSize = true };
                dlcPanel.Controls.Add(new Label { Text = "DLC", AutoSize = true }); dlcPanel.Controls.Add(this.textBoxDlc);
                dataGroupTlp.Controls.Add(dlcPanel, 0, 0);
                dataGroupBox.Text = "Message Data (Hex)";

                var dataBytesTlp = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10), ColumnCount = 4, RowCount = 4 };
                dataBytesTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dataBytesTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                dataBytesTlp.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dataBytesTlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
                for (int i = 0; i < 4; i++) dataBytesTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                dataGroupTlp.Controls.Add(dataBytesTlp, 1, 0);
                formTlp.Controls.Add(dataGroupBox, 0, 1);

                this.dataBytes = new TextBox[8];
                for (int i = 0; i < 8; i++) { this.dataBytes[i] = new TextBox { Text = "00", Dock = DockStyle.Fill, MaxLength = 2 }; dataBytesTlp.Controls.Add(new Label { Text = $"Byte {i}:", Anchor = AnchorStyles.Left, AutoSize = true }, (i % 2) * 2, i / 2); dataBytesTlp.Controls.Add(this.dataBytes[i], (i % 2) * 2 + 1, i / 2); }
                var bottomFlowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, WrapContents = false, AutoSize = true };
                formTlp.Controls.Add(bottomFlowPanel, 0, 2);
                this.sendButton = new Button { Text = "Send Once", Size = new Size(110, 32) }; this.sendButton.Click += sendButton_Click;
                this.autoStart = new Button { Text = "Auto-Send", Size = new Size(110, 32) }; this.autoStart.Click += autoStart_Click;
                Button randomizeButton = new Button { Text = "Randomize", Size = new Size(110, 32) };
                randomizeButton.Click += (s, ev) => { Random rnd = new Random(); int dlc = 8; int.TryParse(textBoxDlc.Text, out dlc); if (dlc > 8) dlc = 8; for (int i = 0; i < dlc; i++) dataBytes[i].Text = $"{rnd.Next(0, 256):X2}"; };
                bottomFlowPanel.Controls.Add(randomizeButton); bottomFlowPanel.Controls.Add(this.autoStart); bottomFlowPanel.Controls.Add(this.sendButton);

                canForm.Show(this);
            }
            else
            {
                canForm.Activate(); // Bring to front if already open
            }
        }

        private void SPIButton_Click(object sender, EventArgs e)
        {
            if (spiFlashForm == null || spiFlashForm.IsDisposed)
            {
               // TODO GA: Make relative path.
                string spiFlashPath = @"C:\Users\GOKHANAKK\Documents\Visual Studio 2022\Projects\e_bike_simulator\threadx\ports\win32\vs_2019\example_build\Battery_Simulator\external_flash.bin";

                spiFlashForm = new FlashViewerForm("External SPI Flash Utility (MX25)", spiFlashPath);
                spiFlashForm.Show(this);
            }
            else
            {
                spiFlashForm.Activate();
            }
        }

        private void I2CButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("I2C configuration window logic should be implemented here.", "I2C Protocol", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}