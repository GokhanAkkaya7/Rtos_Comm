using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Rtos_Comm.application.Configuration;
using Rtos_Comm.application.Driver;
using Rtos_Comm.application.JSON;
using Rtos_Comm.application.PipeConnection;
using Rtos_Comm.application.XMLParser;
using Rtos_Comm.simulation;

namespace Rtos_Comm
{
    public partial class MainForm : Form
    {
        private string default_string = "Channels (Default)";
        private Int32 adc_min_value = 0;
        private Int32 adc_max_value = 0;

        private Json_Class _json_Class;
        private PipeClient _pipeClient;
        private XML_Parser _xml_parser;

        private ADC_Simulator _adc_Simulator;
        private CAN_Simulator _can_Simulator;

        private AdcData _adc_data;
        private IrqData _irq_data;

        private string Pipe_Name = "SimplePipe";
        private List<Message_Format> message_buffer = new List<Message_Format>();

        private bool b_is_process_running = false;
        private bool b_release_oneshot_can_message = false;
        private bool b_release_priodic_can_message = false;
        private bool b_release_irq_message = false;
        private bool button_toggle = true;

        private int DEFAULT_SEND_INTERVAL_IN_MS = 1000;

        private int can_time_holder = 5000;

        // Producer-Consumer Pattern Fields
        private BlockingCollection<string> _messageQueue;
        private CancellationTokenSource _taskCts;
        private Task _producerTask;
        private Task _consumerTask;

        public MainForm()
        {
            InitializeComponent();
            _pipeClient = new PipeClient(Pipe_Name);
            _json_Class = new Json_Class();

            _adc_data = new AdcData();
            _irq_data = new IrqData();
            _irq_data.channel_list = new List<ushort>(); // Initialize to prevent null reference.
            _can_Simulator = new CAN_Simulator();

            _xml_parser = new XML_Parser();
        }

        private void list_filler(string s_type, object obj_value)
        {
            var data = new Message_Format();
            data.driver = s_type;
            data.data = obj_value;
            message_buffer.Add(data);
        }

        private int find_active_channel()
        {
            int ret_value = 0xFF; // Default to an invalid channel.
            string selectedItem = "";

            this.Invoke((Action)(() =>
            {
                if (ADCSelectCombo.SelectedItem != null)
                {
                    selectedItem = ADCSelectCombo.SelectedItem.ToString();
                }
            }));

            // Don't even try if "Channels (Default)" is selected or nothing is selected.
            if (!string.IsNullOrEmpty(selectedItem) && selectedItem != default_string)
            {
                // Try to parse the string into a number.
                if (int.TryParse(selectedItem, out int channel))
                {
                    ret_value = channel;
                }
            }
            return ret_value; // Returns a valid channel number.
        }

        private void data_preprocessor(int active_time_interval)
        {
            message_buffer.Clear();

            // --- ADC Data Preparation (Isolated Block) ---
            try
            {
                int activeChannel = find_active_channel();
                // Only generate ADC data if a valid channel is selected
                if (activeChannel != -1 && _adc_Simulator != null)
                {
                    _adc_data = _adc_Simulator.AdcSimulateOneSample(adc_min_value, adc_max_value, activeChannel);
                    list_filler("adc", _adc_data);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ADC Data Processor Error: {ex.Message}");
            }

            // --- CAN Data Preparation (Isolated Block) ---
            if (b_release_oneshot_can_message || b_release_priodic_can_message)
            {
                can_time_holder += active_time_interval;

                int selectedValue = 0;

                if (CanTimeCombo.InvokeRequired)
                {
                    CanTimeCombo.Invoke(new MethodInvoker(delegate
                    {
                        selectedValue = Convert.ToInt32(CanTimeCombo.SelectedItem);
                    }));
                }
                else
                {
                    selectedValue = Convert.ToInt32(CanTimeCombo.SelectedItem);
                }

                if (can_time_holder >= selectedValue || b_release_oneshot_can_message)
                    try
                    {
                        string id = "", dlc = "";
                        this.Invoke((Action)(() =>
                        {
                            id = textBoxId.Text;
                            dlc = textBoxDlc.Text;
                        }));

                        // Only proceed if ID and DLC are valid
                        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(dlc))
                        {
                            message_buffer.Add(_can_Simulator.can_data_processor(Convert.ToUInt32(id, 16), dataBytes, dlc));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"CAN Data Processor Error: {ex.Message}");
                    }
                    finally
                    {
                        b_release_oneshot_can_message = false;
                        can_time_holder = 0;
                    }
            }

            // --- IRQ Data Preparation (Isolated Block) ---
            if (b_release_irq_message)
            {
                try
                {
                    if (_irq_data.channel_list != null)
                    {
                        list_filler("irq", _irq_data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"IRQ Data Processor Error: {ex.Message}");
                }
                finally
                {
                    b_release_irq_message = false;
                }
            }
        }

        // --- Producer and Consumer Tasks ---

        /// <summary>
        /// DATA PRODUCER: Generates data at a specified interval and adds it to the queue.
        /// </summary>
        private async Task ProducerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    int sendInterval = DEFAULT_SEND_INTERVAL_IN_MS;
                    this.Invoke((Action)(() =>
                    {
                        if (int.TryParse(SendInterval.Text, out int interval))
                            sendInterval = interval;
                    }
                    ));

                    data_preprocessor(sendInterval);

                    // Only convert to JSON if the buffer has data to send.
                    if (message_buffer.Any())
                    {
                        string jsonMessage = _json_Class.Json_Processor(message_buffer);
                        _messageQueue.Add(jsonMessage, token);
                    }

                    await Task.Delay(sendInterval, token);
                }
                catch (OperationCanceledException) { break; } // Exit loop if cancellation is requested.
                catch (Exception ex)
                {
                    Debug.WriteLine($"ProducerLoopAsync Error: {ex.Message}");
                    await Task.Delay(1000, token); // Wait briefly after an error.
                }
            }
        }

        /// <summary>
        /// DATA CONSUMER: Takes data from the queue, connects to the Pipe, sends, and disconnects.
        /// </summary>
        private async Task ConsumerLoopAsync(CancellationToken token)
        {
            // GetConsumingEnumerable starts listening to the queue.
            // It blocks until a new item is available or the task is canceled.
            foreach (var message in _messageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    await _pipeClient.ConnectAsync();
                    await _pipeClient.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ConsumerLoopAsync/Send Error: {ex.Message}");
                }
                finally
                {
                    // Attempt to disconnect after every try
                    _pipeClient.Disconnect();
                }
            }
        }

        private void Connect_Button_Click(object sender, EventArgs e)
        {
            if (b_is_process_running) return;

            b_is_process_running = true;
            _messageQueue = new BlockingCollection<string>();
            _taskCts = new CancellationTokenSource();
            CancellationToken token = _taskCts.Token;

            _producerTask = Task.Run(() => ProducerLoopAsync(token), token);
            _consumerTask = Task.Run(() => ConsumerLoopAsync(token), token);

            Connect_Button.Enabled = false;
            Disconnect_Button.Enabled = true;
        }

        private async void Disconnect_Button_Click(object sender, EventArgs e)
        {
            if (!b_is_process_running) return;

            if (_taskCts != null)
            {
                _taskCts.Cancel(); // Signal cancellation to the tasks.
                try
                {
                    // Wait for both tasks to complete gracefully.
                    await Task.WhenAll(_producerTask, _consumerTask);
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    _taskCts.Dispose();
                    _taskCts = null;
                    b_is_process_running = false;
                }
            }

            Connect_Button.Enabled = true;
            Disconnect_Button.Enabled = false;
        }

        private void TextBoxDlc_TextChanged(object sender, EventArgs e)
        {
            if (int.TryParse(textBoxDlc.Text, out int dlc) && dlc > 8)
            {
                MessageBox.Show("DLC Value Must be lower or equal to 8");
                textBoxDlc.Text = "8";
            }
        }

        private void IRQSelectCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (IRQSelectCombo.SelectedItem == null) return;
            string selectedItem = IRQSelectCombo.SelectedItem.ToString();

            if (selectedItem == default_string)
            {
                IRQBox.Text = "";
                _irq_data.channel_list.Clear();
            }
            else
            {
                if (!IRQBox.Text.Contains(selectedItem))
                {
                    IRQBox.AppendText(string.IsNullOrEmpty(IRQBox.Text) ? selectedItem : Environment.NewLine + selectedItem);
                    if (ushort.TryParse(selectedItem, out ushort channel))
                    {
                        _irq_data.channel_list.Add(channel);
                        _irq_data.channel_count = _irq_data.channel_list.Count;
                    }
                }
            }
        }

        private void IRQButton_Click(object sender, EventArgs e)
        { 
            b_release_irq_message = true; 
        }
        private void sendButton_Click(object sender, EventArgs e)
        {
            b_release_oneshot_can_message = true; 
        }

        private void autoStart_Click(object sender, EventArgs e)
        {
            button_toggle = !button_toggle;
            if (button_toggle)
            {
                autoStart.Text = "Auto Start";
                b_release_priodic_can_message = false;
            }
            else
            {
                autoStart.Text = "Auto Stop";
                b_release_priodic_can_message = true;
            }
        }

        private void IOButton_Click(object sender, EventArgs e) { }
        private void ADCButton_Click(object sender, EventArgs e)
        {
            try
            {
                adc_min_value = Convert.ToInt32(ADCMinBox.Text);
                adc_max_value = Convert.ToInt32(ADCMaxBox.Text);
            }
            catch
            {
            }
        }

        private void RTCButton_Click(object sender, EventArgs e) { }

        private void btnLoadXml_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "XML Files (*.xml)|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _xml_parser.ParseConfigurationXml(ofd.FileName);
                        lblXmlStatus.Text = "XML File Loaded.";

                        // Populate ADC ComboBox
                        var allAdcs = _xml_parser.GetConfigs<ADC_Config_Class>();
                        _adc_Simulator = new ADC_Simulator(allAdcs[0]);
                        ADCSelectCombo.Items.Clear();
                        ADCSelectCombo.Items.Add(default_string);
                        foreach (var channel in allAdcs[0].UsedChannels)
                        {
                            ADCSelectCombo.Items.Add(channel.ToString());
                        }
                        ADCSelectCombo.SelectedIndex = 0;

                        // Populate IRQ ComboBox
                        var allIrqs = _xml_parser.GetConfigs<IRQ_Config_Class>();
                        IRQSelectCombo.Items.Clear();
                        IRQSelectCombo.Items.Add(default_string);
                        IRQSelectCombo.Items.Add(allIrqs[0].Used_Irq_Channels);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("XML File Load Error:\n" + ex.Message);
                    }
                }
            }
        }
    }
}