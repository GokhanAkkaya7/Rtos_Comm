using Rtos_Comm.application.Configuration;
using Rtos_Comm.application.Driver;
using Rtos_Comm.application.JSON;
using Rtos_Comm.application.PipeConnection;
using Rtos_Comm.application.XMLParser;
using Rtos_Comm.simulation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Rtos_Comm
{
    public partial class MainForm : Form
    {
        private string default_string = "Channels (Default)";
        private Int32 adc_min_value = 0;
        private Int32 adc_max_value = 0;
        private UInt16 previous_pin_number = UInt16.MaxValue;

        private Json_Class _json_Class;
        private PipeClient _pipeClient;
        private XML_Parser _xml_parser;

        private ADC_Simulator _adc_Simulator;
        private CAN_Simulator _can_Simulator;

        private AdcData _adc_data;
        private IrqData _irq_data;
        private GPTData _gpt_data;
        private IoData _io_data;
        private RTCData _rtc_data;
        private BatterySimulator _bmsSimulator;

        private string Pipe_Name = "SimplePipe";
        private List<Message_Format> message_buffer = new List<Message_Format>();

        private bool b_is_process_running = false;
        private bool b_release_oneshot_can_message = false;
        private bool b_release_priodic_can_message = false;
        private bool b_release_irq_message = false;
        private bool b_button_toggle = true;
        private bool b_gpt_set = false;
        private bool b_io_toggle = false;
        private bool b_release_io_message = false;
        private bool b_release_rtc_message = false;
        private bool b_release_bms_message = false;

        private int DEFAULT_SEND_INTERVAL_IN_MS = 1000;

        private int can_time_holder = 5000;

        // Producer-Consumer Pattern Fields
        private BlockingCollection<string> _messageQueue;
        private CancellationTokenSource _taskCts;
        private Task _producerTask;
        private Task _consumerTask;
        private Task _listenerTask;

        public MainForm()
        {
            InitializeComponent();
            _pipeClient = new PipeClient(Pipe_Name);
            _json_Class = new Json_Class();

            _adc_data = new AdcData();
            _irq_data = new IrqData();
            _irq_data.channel_list = new List<ushort>(); // Initialize to prevent null reference.
            _can_Simulator = new CAN_Simulator();
            _gpt_data = new GPTData();
            _io_data = new IoData();
            _rtc_data = new RTCData();

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

            if (b_gpt_set)
            {
                list_filler("gpt", _gpt_data);
                b_gpt_set = false;
            }
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

            if (b_release_io_message)
            {
                try
                {
                    if (_io_data != null)
                    {
                        list_filler("io", _io_data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"IO Data Processor Error: {ex.Message}");
                }
                finally
                {
                    b_release_io_message = false;
                }
            }

            if (b_release_rtc_message)
            {
                try
                {
                    list_filler("rtc", _rtc_data);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"RTC Data Processor Error: {ex.Message}");
                }
                finally
                {
                    b_release_rtc_message = false;
                }
            }

            if (b_release_bms_message && _bmsSimulator != null)
            {
                try
                {
                    // Get the latest complete register data from the simulator
                    var bmsData = _bmsSimulator.CurrentState.BmsRegisters;
                    list_filler("bms", bmsData); // Use "bms" as the driver name
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"BMS Data Processor Error: {ex.Message}");
                }
                finally
                {
                    // Reset the flag. It will be set to true again on the next simulator update.
                    b_release_bms_message = false;
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
            foreach (var message in _messageQueue.GetConsumingEnumerable(token))
            {
                try
                {
                    await _pipeClient.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ConsumerLoopAsync/Send Error: {ex.Message}");
                }
            }
        }

        private async Task ListenerLoopAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    string receivedMessage = await _pipeClient.ReceiveMessageAsync();
                    if (receivedMessage == null)
                    {
                        Debug.WriteLine("ListenerLoopAsync: Pipe disconnected.");
                        if (b_is_process_running)
                        {
                            this.Invoke((Action)(() => Disconnect_Button_Click(this, EventArgs.Empty)));
                        }
                        break;
                    }
                    ProcessIncomingMessage(receivedMessage);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"ListenerLoopAsync Exception: {ex.Message}");
            }
        }

        private void ProcessIncomingMessage(string in_pipe_data)
        {
            try
            {
                List<Message_Format> message = _json_Class.Json_Parser(in_pipe_data);

                foreach (Message_Format message_format in message)
                {
                    if ((null != message_format) && ("rtc" == message_format.driver))
                    {
                        string jsonData = System.Text.Json.JsonSerializer.Serialize(message_format.data);
                        RTCData rtcData = System.Text.Json.JsonSerializer.Deserialize<RTCData>(jsonData);

                        if (rtcData != null)
                        {
                            if (rtc_event_t.RTC_SET == rtcData.rtc_event)
                            {
                                _rtc_data = rtcData;
                                UpdateRtcLabel(rtcData);
                            }
                        }
                    }

                    if ((null != message_format) && ("can" == message_format.driver))
                    {
                        string jsonData = System.Text.Json.JsonSerializer.Serialize(message_format.data);
                        CanData canData = System.Text.Json.JsonSerializer.Deserialize<CanData>(jsonData);

                        if (canData != null)
                        {

                        }
                    }
                    // Add here the other received drivers if conditions.
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ProcessIncomingMessage Error: {ex.Message} on message: {in_pipe_data}");
            }
        }
        private void UpdateRtcLabel(RTCData timeData)
        {
            string timeString = $"{timeData.hour:D2}:{timeData.minute:D2}:{timeData.second:D2}";

            if (this.lblRtcTime.InvokeRequired)
            {
                this.lblRtcTime.Invoke(new Action(() =>
                {
                    this.lblRtcTime.Text = timeString;
                }));
            }
            else
            {
                this.lblRtcTime.Text = timeString;
            }
        }

        private void btnRtcSync_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            _rtc_data.year = now.Year;
            _rtc_data.month = now.Month;
            _rtc_data.day = now.Day;
            _rtc_data.hour = now.Hour;
            _rtc_data.minute = now.Minute;
            _rtc_data.second = now.Second;

            UpdateRtcLabel(_rtc_data);
        }
        private void btnRtcGet_Click(object sender, EventArgs e)
        {
            _rtc_data.rtc_event = rtc_event_t.RTC_GET;
            _rtc_data.second = 0;
            _rtc_data.minute = 0;
            _rtc_data.hour = 0;
            _rtc_data.day = 0;
            _rtc_data.month = 0;
            _rtc_data.year = 0;
            b_release_rtc_message = true;
        }
        private void btnRtcSet_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;

            _rtc_data.rtc_event = rtc_event_t.RTC_SET;
            _rtc_data.year = now.Year;
            _rtc_data.month = now.Month;
            _rtc_data.day = now.Day;
            _rtc_data.hour = now.Hour;
            _rtc_data.minute = now.Minute;
            _rtc_data.second = now.Second;
            b_release_rtc_message = true;
        }

        private async void Connect_Button_Click(object sender, EventArgs e)
        {
            if (b_is_process_running) return;

            Connect_Button.Enabled = false;

            bool isConnected = await _pipeClient.ConnectAsync();

            if (isConnected)
            {
                b_is_process_running = true;
                _messageQueue = new BlockingCollection<string>();
                _taskCts = new CancellationTokenSource();
                CancellationToken token = _taskCts.Token;

                _listenerTask = Task.Run(() => ListenerLoopAsync(token), token);
                _producerTask = Task.Run(() => ProducerLoopAsync(token), token);
                _consumerTask = Task.Run(() => ConsumerLoopAsync(token), token);

                Disconnect_Button.Enabled = true;
                Console.WriteLine("Pipe connected and all tasks started.");
            }
            else
            {
                MessageBox.Show("Failed to connect to the C simulation. Please ensure the simulation is running.", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Connect_Button.Enabled = true;
            }
        }

        private async void Disconnect_Button_Click(object sender, EventArgs e)
        {
            if (!b_is_process_running) return;

            Disconnect_Button.Enabled = false;

            if (_taskCts != null)
            {
                _taskCts.Cancel();
                try
                {
                    await Task.WhenAll(_producerTask, _consumerTask);
                    await Task.Run(() => _listenerTask.Wait(1000));
                }
                catch { }
                finally
                {
                    _taskCts.Dispose();
                    _taskCts = null;
                }
            }

            _pipeClient.Disconnect();
            b_is_process_running = false;
            Connect_Button.Enabled = true;
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
            if (IRQSelectCombo.SelectedItem == null) 
                return;
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
            if (IRQBox.Text != "")
                b_release_irq_message = true;
        }
        private void sendButton_Click(object sender, EventArgs e)
        {
            b_release_oneshot_can_message = true;
        }

        private void autoStart_Click(object sender, EventArgs e)
        {
            b_button_toggle = !b_button_toggle;
            if (b_button_toggle)
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

        private void IOButton_Click(object sender, EventArgs e)
        {
            int port = (int)this.numIoPort.Value;
            int pin = (int)this.numIoPin.Value;

            // pin value is (port_number * 16) + pin_number since there 16 pins every port in s3a6.
            ushort pinValue = (ushort)((port * 16) + pin);
            _io_data.pin = pinValue;

            if (previous_pin_number == pinValue)
            {
                if (b_io_toggle)
                    _io_data.state = app_io_level_t.APP_IO_LEVEL_HIGH;
                else
                    _io_data.state = app_io_level_t.APP_IO_LEVEL_LOW;

                b_io_toggle = !b_io_toggle;
            }
            else
                _io_data.state = app_io_level_t.APP_IO_LEVEL_HIGH;

            b_release_io_message = true;

            previous_pin_number = pinValue;
        }
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
                        IRQSelectCombo.SelectedIndex = 0; ;

                        var allgpts = _xml_parser.GetConfigs<GPT_Config_Class>();
                        _gpt_data.channel_count = allgpts.Count;

                        _gpt_data.Channel = new int[_gpt_data.channel_count];
                        _gpt_data.Period = new int[_gpt_data.channel_count];
                        _gpt_data.Unit = new GptUnit[_gpt_data.channel_count];

                        for (int gpt_index = 0; gpt_index < _gpt_data.channel_count; gpt_index++)
                        {
                            _gpt_data.Channel[gpt_index] = allgpts[gpt_index].Channel;
                            _gpt_data.Period[gpt_index] = allgpts[gpt_index].Period;
                            _gpt_data.Unit[gpt_index] = allgpts[gpt_index].Unit;
                            b_gpt_set = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("XML File Load Error:\n" + ex.Message);
                    }
                }
            }
        }

        private void GaugeButton_Click(object sender, EventArgs e)
        {
            if (gaugeForm == null || gaugeForm.IsDisposed)
            {
                // Create the simulator instance here, managed by MainForm
                _bmsSimulator = new BatterySimulator();

                // When the simulator updates, set a flag to send data
                _bmsSimulator.StateUpdated += (state) => {
                    b_release_bms_message = true;
                };

                // Pass the simulator instance to the GaugeForm
                gaugeForm = new GaugeForm(_bmsSimulator);
                gaugeForm.Show(this);
            }
            else
            {
                gaugeForm.Activate();
            }
        }
    }
}