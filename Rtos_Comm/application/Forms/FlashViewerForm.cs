using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public partial class FlashViewerForm : Form
    {
        private string _simulationFilePath;

        private TextBox txtFilePath;
        private RichTextBox rtbHexView;

        public FlashViewerForm(string windowTitle, string simulationFilePath)
        {
            _simulationFilePath = simulationFilePath;

            this.Text = windowTitle;
            this.Size = new Size(800, 600);
            this.MinimumSize = new Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Font = new Font("Segoe UI", 9F);

            InitializeComponent();

            txtFilePath.Text = _simulationFilePath;


            if (File.Exists(_simulationFilePath))          
                DisplayFlashFile(_simulationFilePath);
            
            else
            {
                rtbHexView.ForeColor = Color.Red;
                rtbHexView.Text = $"\n\nERROR: The simulation file was not found at the expected path.\n\n" +
                                  $"Path: {_simulationFilePath}\n\n" +
                                  $"Please run the C simulation application at least once to generate this file.";
            }

        }

        private void InitializeComponent()
        {
            var mainTlp = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(10) };
            mainTlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainTlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            this.Controls.Add(mainTlp);

            var topPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, AutoSize = true, Margin = new Padding(0, 0, 0, 5) };
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            topPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            mainTlp.Controls.Add(topPanel, 0, 0);

            var btnLoadFile = new Button { Text = "Load & View File...", AutoSize = true, Padding = new Padding(5) };
            btnLoadFile.Click += BtnLoadFile_Click;
            txtFilePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(5, 0, 5, 0) };
            var btnLoadPreset = new Button { Text = "Load Preset to Simulation...", AutoSize = true, Padding = new Padding(5) };
            btnLoadPreset.Click += BtnLoadPreset_Click;

            topPanel.Controls.Add(btnLoadFile, 0, 0);
            topPanel.Controls.Add(txtFilePath, 1, 0);
            topPanel.Controls.Add(btnLoadPreset, 2, 0);

            rtbHexView = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9.75F),
                WordWrap = false,
                ReadOnly = true, 
                BackColor = Color.White
            };
            mainTlp.Controls.Add(rtbHexView, 0, 1);
        }

        private void BtnLoadFile_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Binary Files (*.bin)|*.bin|All files (*.*)|*.*";
                ofd.Title = "Select a Flash Simulation File to View";
                ofd.InitialDirectory = Path.GetDirectoryName(_simulationFilePath);

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = ofd.FileName;
                    DisplayFlashFile(ofd.FileName);
                }
            }
        }

        private void BtnLoadPreset_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Preset Flash Files (*.bin)|*.bin";
                ofd.Title = "Select a Preset to Load into Simulation";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.Copy(ofd.FileName, _simulationFilePath, true); // Overwrite
                        MessageBox.Show("Preset successfully loaded into simulation.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        txtFilePath.Text = _simulationFilePath;
                        DisplayFlashFile(_simulationFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to load preset: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void DisplayFlashFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    rtbHexView.ForeColor = Color.Red;
                    rtbHexView.Text = $"\n\nERROR: The specified file does not exist.\n\nPath: {filePath}";
                    return;
                }
                rtbHexView.ForeColor = Color.Black;

                byte[] fileBytes = File.ReadAllBytes(filePath);

                if (fileBytes.Length == 0)
                {
                    rtbHexView.Text = "\n\nFile is empty.";
                    return;
                }

                var sb = new StringBuilder();
                for (int i = 0; i < fileBytes.Length; i += 16)
                {
                    sb.Append($"{i:X8}: ");
                    for (int j = 0; j < 16; j++)
                    {
                        if (i + j < fileBytes.Length) sb.Append($"{fileBytes[i + j]:X2} ");
                        else sb.Append("   ");
                    }
                    sb.Append(" | ");
                    for (int j = 0; j < 16; j++)
                    {
                        if (i + j < fileBytes.Length)
                        {
                            char c = (char)fileBytes[i + j];
                            sb.Append(char.IsControl(c) ? '.' : c);
                        }
                    }
                    sb.AppendLine();
                }
                rtbHexView.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                rtbHexView.ForeColor = Color.Red;
                rtbHexView.Text = $"\n\nAn error occurred while reading the file:\n\n{ex.Message}\n\nPath: {filePath}";
            }
        }
    }
}