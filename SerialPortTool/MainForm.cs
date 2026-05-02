using System.IO.Ports;

namespace SerialPortTool;

public partial class MainForm : Form
{
    private SerialPort? _serialPort;
    private bool _isConnected = false;
    private DateTime _startTime;
    private int _sendCount = 0;
    private int _receiveCount = 0;
    private delegate void UpdateReceiveCallback(string text);

    public MainForm()
    {
        InitializeComponent();
        InitializeSerialPortSettings();
        this.FormClosing += MainForm_FormClosing;
        timer1.Interval = 1000;
        timer1.Tick += Timer1_Tick;
    }

    private void InitializeSerialPortSettings()
    {
        string[] ports = SerialPort.GetPortNames();
        cmbPort.Items.AddRange(ports);
        if (cmbPort.Items.Count > 0)
            cmbPort.SelectedIndex = 0;

        cmbBaudRate.Items.AddRange(new object[] { 9600, 19200, 38400, 57600, 115200, 230400, 460800 });
        cmbBaudRate.SelectedItem = 115200;

        cmbDataBits.Items.AddRange(new object[] { 5, 6, 7, 8 });
        cmbDataBits.SelectedItem = 8;

        cmbStopBits.Items.AddRange(new object[] { "1", "1.5", "2" });
        cmbStopBits.SelectedIndex = 0;

        cmbParity.Items.AddRange(new object[] { "None", "Odd", "Even", "Mark", "Space" });
        cmbParity.SelectedIndex = 0;

        cmbCrcType.Items.AddRange(new object[] { "CRC8", "CRC16", "CRC32" });
        cmbCrcType.SelectedIndex = 1;

        chkHexSend.Checked = true;
        chkHexReceive.Checked = true;
        chkAutoSend.Checked = false;
    }

    private void Timer1_Tick(object? sender, EventArgs e)
    {
        if (_isConnected)
        {
            TimeSpan elapsed = DateTime.Now - _startTime;
            lblStatus.Text = $"已连接 | 运行时间: {elapsed:hh\\:mm\\:ss} | 发送: {_sendCount} | 接收: {_receiveCount}";
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        Disconnect();
    }

    private void btnConnect_Click(object sender, EventArgs e)
    {
        if (!_isConnected)
        {
            Connect();
        }
        else
        {
            Disconnect();
        }
    }

    private void Connect()
    {
        try
        {
            _serialPort = new SerialPort
            {
                PortName = cmbPort.SelectedItem?.ToString() ?? "COM1",
                BaudRate = int.Parse(cmbBaudRate.SelectedItem?.ToString() ?? "115200"),
                DataBits = int.Parse(cmbDataBits.SelectedItem?.ToString() ?? "8"),
                ReadTimeout = 1000,
                WriteTimeout = 1000
            };

            string parity = cmbParity.SelectedItem?.ToString() ?? "None";
            _serialPort.Parity = parity switch
            {
                "Odd" => Parity.Odd,
                "Even" => Parity.Even,
                "Mark" => Parity.Mark,
                "Space" => Parity.Space,
                _ => Parity.None
            };

            string stopBits = cmbStopBits.SelectedItem?.ToString() ?? "1";
            _serialPort.StopBits = stopBits switch
            {
                "1.5" => StopBits.OnePointFive,
                "2" => StopBits.Two,
                _ => StopBits.One
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();

            _isConnected = true;
            _startTime = DateTime.Now;
            _sendCount = 0;
            _receiveCount = 0;

            btnConnect.Text = "断开";
            btnConnect.BackColor = Color.Red;
            grpSerialSettings.Enabled = false;
            timer1.Start();

            txtReceive.AppendText($"[{DateTime.Now:HH:mm:ss}] 串口已打开\r\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"连接失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Disconnect()
    {
        try
        {
            timer1.Stop();
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }

            _isConnected = false;
            btnConnect.Text = "连接";
            btnConnect.BackColor = Color.FromKnownColor(KnownColor.Control);
            grpSerialSettings.Enabled = true;

            txtReceive.AppendText($"[{DateTime.Now:HH:mm:ss}] 串口已关闭\r\n");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"断开失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            Thread.Sleep(50);
            int bytesToRead = _serialPort.BytesToRead;
            if (bytesToRead == 0) return;

            byte[] buffer = new byte[bytesToRead];
            _serialPort.Read(buffer, 0, bytesToRead);
            _receiveCount += bytesToRead;

            string receivedText;
            if (chkHexReceive.Checked)
            {
                receivedText = BitConverter.ToString(buffer).Replace("-", " ");
            }
            else
            {
                receivedText = System.Text.Encoding.ASCII.GetString(buffer);
            }

            this.Invoke(new UpdateReceiveCallback(AppendReceiveText), $"[{DateTime.Now:HH:mm:ss}] 接收: {receivedText}\r\n");
        }
        catch (Exception ex)
        {
            this.Invoke(new UpdateReceiveCallback(AppendReceiveText), $"[{DateTime.Now:HH:mm:ss}] 接收错误: {ex.Message}\r\n");
        }
    }

    private void AppendReceiveText(string text)
    {
        txtReceive.AppendText(text);
    }

    private void btnSend_Click(object sender, EventArgs e)
    {
        if (!_isConnected || _serialPort == null || !_serialPort.IsOpen)
        {
            MessageBox.Show("请先连接串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string input = txtSend.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            MessageBox.Show("请输入要发送的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            byte[] data;
            if (chkHexSend.Checked)
            {
                data = HexStringToBytes(input);
            }
            else
            {
                data = System.Text.Encoding.ASCII.GetBytes(input);
            }

            bool addCrc = chkAddCrc.Checked;
            if (addCrc)
            {
                CrcType crcType = cmbCrcType.SelectedIndex switch
                {
                    0 => CrcType.CRC8,
                    2 => CrcType.CRC32,
                    _ => CrcType.CRC16
                };

                byte[] crc = CrcHelper.Calculate(data, crcType);
                byte[] dataWithCrc = new byte[data.Length + crc.Length];
                Buffer.BlockCopy(data, 0, dataWithCrc, 0, data.Length);
                Buffer.BlockCopy(crc, 0, dataWithCrc, data.Length, crc.Length);
                data = dataWithCrc;

                string crcInfo = $" [CRC-{crcType}:{CrcHelper.BytesToHex(crc)}]";
                txtReceive.AppendText($"[{DateTime.Now:HH:mm:ss}] 发送: {BitConverter.ToString(data.Take(data.Length - crc.Length).ToArray()).Replace("-", " ")}{crcInfo}\r\n");
            }
            else
            {
                txtReceive.AppendText($"[{DateTime.Now:HH:mm:ss}] 发送: {BitConverter.ToString(data).Replace("-", " ")}\r\n");
            }

            _serialPort.Write(data, 0, data.Length);
            _sendCount += data.Length;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"发送失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnClearReceive_Click(object sender, EventArgs e)
    {
        txtReceive.Clear();
    }

    private void btnClearSend_Click(object sender, EventArgs e)
    {
        txtSend.Clear();
    }

    private void btnRefreshPorts_Click(object sender, EventArgs e)
    {
        cmbPort.Items.Clear();
        string[] ports = SerialPort.GetPortNames();
        cmbPort.Items.AddRange(ports);
        if (cmbPort.Items.Count > 0)
            cmbPort.SelectedIndex = 0;
        else
            cmbPort.Text = "";
    }

    private void chkAutoSend_CheckedChanged(object sender, EventArgs e)
    {
        if (chkAutoSend.Checked)
        {
            if (!_isConnected)
            {
                MessageBox.Show("请先连接串口", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkAutoSend.Checked = false;
                return;
            }
            if (string.IsNullOrWhiteSpace(txtSend.Text))
            {
                MessageBox.Show("请输入要发送的数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                chkAutoSend.Checked = false;
                return;
            }
            autoSendTimer.Interval = (int)numAutoSendInterval.Value * 1000;
            autoSendTimer.Start();
            btnSend.Enabled = false;
        }
        else
        {
            autoSendTimer.Stop();
            btnSend.Enabled = true;
        }
    }

    private void autoSendTimer_Tick(object sender, EventArgs e)
    {
        btnSend_Click(sender, e);
    }

    private void btnCalculateCrc_Click(object sender, EventArgs e)
    {
        string input = txtCrcInput.Text.Trim();
        if (string.IsNullOrEmpty(input))
        {
            MessageBox.Show("请输入数据", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            byte[] data = HexStringToBytes(input);
            CrcType crcType = cmbCrcType.SelectedIndex switch
            {
                0 => CrcType.CRC8,
                2 => CrcType.CRC32,
                _ => CrcType.CRC16
            };

            byte[] crc = CrcHelper.Calculate(data, crcType);
            lblCrcResult.Text = $"{CrcHelper.BytesToHex(crc)} ({crc.Length} bytes)";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CRC计算失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnVerifyCrc_Click(object sender, EventArgs e)
    {
        string dataInput = txtCrcInput.Text.Trim();
        string crcInput = txtCrcVerify.Text.Trim();

        if (string.IsNullOrEmpty(dataInput) || string.IsNullOrEmpty(crcInput))
        {
            MessageBox.Show("请输入数据和CRC值", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            byte[] data = HexStringToBytes(dataInput);
            byte[] crc = HexStringToBytes(crcInput);

            CrcType crcType = cmbCrcType.SelectedIndex switch
            {
                0 => CrcType.CRC8,
                2 => CrcType.CRC32,
                _ => CrcType.CRC16
            };

            bool isValid = CrcHelper.Verify(data, crc, crcType);
            MessageBox.Show(isValid ? "CRC校验通过!" : "CRC校验失败!", "结果", MessageBoxButtons.OK, 
                isValid ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"CRC校验失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static byte[] HexStringToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("-", "");
        if (hex.Length % 2 != 0)
            throw new FormatException("HEX字符串长度必须为偶数");

        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return bytes;
    }
}