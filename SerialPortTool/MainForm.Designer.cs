namespace SerialPortTool;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
        this.topPanel = new System.Windows.Forms.Panel();
        this.bottomPanel = new System.Windows.Forms.Panel();
        this.grpReceive = new System.Windows.Forms.GroupBox();
        this.txtReceive = new System.Windows.Forms.TextBox();
        this.btnClearReceive = new System.Windows.Forms.Button();
        this.chkHexReceive = new System.Windows.Forms.CheckBox();
        this.grpSend = new System.Windows.Forms.GroupBox();
        this.sendTableLayout = new System.Windows.Forms.TableLayoutPanel();
        this.txtSend = new System.Windows.Forms.TextBox();
        this.sendOptionsPanel = new System.Windows.Forms.Panel();
        this.chkHexSend = new System.Windows.Forms.CheckBox();
        this.chkAddCrc = new System.Windows.Forms.CheckBox();
        this.chkAutoSend = new System.Windows.Forms.CheckBox();
        this.numAutoSendInterval = new System.Windows.Forms.NumericUpDown();
        this.lblInterval = new System.Windows.Forms.Label();
        this.btnSend = new System.Windows.Forms.Button();
        this.btnClearSend = new System.Windows.Forms.Button();
        this.grpSerialSettings = new System.Windows.Forms.GroupBox();
        this.cmbPort = new System.Windows.Forms.ComboBox();
        this.cmbBaudRate = new System.Windows.Forms.ComboBox();
        this.cmbDataBits = new System.Windows.Forms.ComboBox();
        this.cmbStopBits = new System.Windows.Forms.ComboBox();
        this.cmbParity = new System.Windows.Forms.ComboBox();
        this.lblPort = new System.Windows.Forms.Label();
        this.lblBaudRate = new System.Windows.Forms.Label();
        this.lblDataBits = new System.Windows.Forms.Label();
        this.lblStopBits = new System.Windows.Forms.Label();
        this.lblParity = new System.Windows.Forms.Label();
        this.btnRefreshPorts = new System.Windows.Forms.Button();
        this.btnConnect = new System.Windows.Forms.Button();
        this.grpCrc = new System.Windows.Forms.GroupBox();
        this.lblCrcType = new System.Windows.Forms.Label();
        this.cmbCrcType = new System.Windows.Forms.ComboBox();
        this.txtCrcInput = new System.Windows.Forms.TextBox();
        this.lblCrcInput = new System.Windows.Forms.Label();
        this.btnCalculateCrc = new System.Windows.Forms.Button();
        this.lblCrcResult = new System.Windows.Forms.Label();
        this.txtCrcVerify = new System.Windows.Forms.TextBox();
        this.lblCrcVerify = new System.Windows.Forms.Label();
        this.btnVerifyCrc = new System.Windows.Forms.Button();
        this.lblStatus = new System.Windows.Forms.Label();
        this.timer1 = new System.Windows.Forms.Timer(this.components);
        this.autoSendTimer = new System.Windows.Forms.Timer(this.components);
        this.mainLayout.SuspendLayout();
        this.topPanel.SuspendLayout();
        this.grpReceive.SuspendLayout();
        this.bottomPanel.SuspendLayout();
        this.grpSend.SuspendLayout();
        this.sendTableLayout.SuspendLayout();
        this.sendOptionsPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.numAutoSendInterval)).BeginInit();
        this.grpSerialSettings.SuspendLayout();
        this.grpCrc.SuspendLayout();
        this.SuspendLayout();
        //
        // mainLayout
        //
        this.mainLayout.ColumnCount = 2;
        this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
        this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
        this.mainLayout.Controls.Add(this.topPanel, 0, 0);
        this.mainLayout.Controls.Add(this.bottomPanel, 0, 1);
        this.mainLayout.Controls.Add(this.grpSerialSettings, 1, 0);
        this.mainLayout.Controls.Add(this.grpCrc, 1, 1);
        this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.mainLayout.Location = new System.Drawing.Point(0, 0);
        this.mainLayout.Name = "mainLayout";
        this.mainLayout.RowCount = 2;
        this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
        this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
        this.mainLayout.Size = new System.Drawing.Size(900, 700);
        //
        // topPanel
        //
        this.topPanel.Controls.Add(this.grpReceive);
        this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.topPanel.Location = new System.Drawing.Point(3, 3);
        this.topPanel.Name = "topPanel";
        this.topPanel.Size = new System.Drawing.Size(624, 402);
        this.topPanel.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
        //
        // grpReceive
        //
        this.grpReceive.Controls.Add(this.btnClearReceive);
        this.grpReceive.Controls.Add(this.chkHexReceive);
        this.grpReceive.Controls.Add(this.txtReceive);
        this.grpReceive.Dock = System.Windows.Forms.DockStyle.Fill;
        this.grpReceive.Location = new System.Drawing.Point(0, 0);
        this.grpReceive.Name = "grpReceive";
        this.grpReceive.Padding = new System.Windows.Forms.Padding(5);
        this.grpReceive.Size = new System.Drawing.Size(624, 397);
        this.grpReceive.TabIndex = 0;
        this.grpReceive.TabStop = false;
        this.grpReceive.Text = "接收数据";
        //
        // txtReceive
        //
        this.txtReceive.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtReceive.Location = new System.Drawing.Point(8, 28);
        this.txtReceive.Multiline = true;
        this.txtReceive.Name = "txtReceive";
        this.txtReceive.ReadOnly = true;
        this.txtReceive.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtReceive.Size = new System.Drawing.Size(608, 330);
        this.txtReceive.TabIndex = 0;
        this.txtReceive.Font = new System.Drawing.Font("Consolas", 10F);
        //
        // btnClearReceive
        //
        this.btnClearReceive.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.btnClearReceive.Location = new System.Drawing.Point(8, 363);
        this.btnClearReceive.Name = "btnClearReceive";
        this.btnClearReceive.Size = new System.Drawing.Size(608, 26);
        this.btnClearReceive.TabIndex = 1;
        this.btnClearReceive.Text = "清空接收区";
        this.btnClearReceive.UseVisualStyleBackColor = true;
        this.btnClearReceive.Click += new System.EventHandler(this.btnClearReceive_Click);
        //
        // chkHexReceive
        //
        this.chkHexReceive.AutoSize = true;
        this.chkHexReceive.Checked = true;
        this.chkHexReceive.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkHexReceive.Dock = System.Windows.Forms.DockStyle.Top;
        this.chkHexReceive.Location = new System.Drawing.Point(8, 20);
        this.chkHexReceive.Name = "chkHexReceive";
        this.chkHexReceive.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.chkHexReceive.Size = new System.Drawing.Size(608, 8);
        this.chkHexReceive.TabIndex = 2;
        this.chkHexReceive.Text = "HEX显示";
        this.chkHexReceive.UseVisualStyleBackColor = true;
        //
        // bottomPanel
        //
        this.bottomPanel.Controls.Add(this.grpSend);
        this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.bottomPanel.Location = new System.Drawing.Point(3, 410);
        this.bottomPanel.Name = "bottomPanel";
        this.bottomPanel.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
        this.bottomPanel.Size = new System.Drawing.Size(624, 287);
        //
        // grpSend
        //
        this.grpSend.Controls.Add(this.sendTableLayout);
        this.grpSend.Dock = System.Windows.Forms.DockStyle.Fill;
        this.grpSend.Location = new System.Drawing.Point(0, 5);
        this.grpSend.Name = "grpSend";
        this.grpSend.Padding = new System.Windows.Forms.Padding(5);
        this.grpSend.Size = new System.Drawing.Size(624, 277);
        this.grpSend.TabIndex = 1;
        this.grpSend.TabStop = false;
        this.grpSend.Text = "发送数据";
        //
        // sendTableLayout
        //
        this.sendTableLayout.ColumnCount = 2;
        this.sendTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.sendTableLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
        this.sendTableLayout.Controls.Add(this.txtSend, 0, 0);
        this.sendTableLayout.Controls.Add(this.sendOptionsPanel, 1, 0);
        this.sendTableLayout.Controls.Add(this.btnSend, 0, 1);
        this.sendTableLayout.Controls.Add(this.btnClearSend, 1, 1);
        this.sendTableLayout.Dock = System.Windows.Forms.DockStyle.Fill;
        this.sendTableLayout.Location = new System.Drawing.Point(8, 28);
        this.sendTableLayout.Name = "sendTableLayout";
        this.sendTableLayout.RowCount = 2;
        this.sendTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
        this.sendTableLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
        this.sendTableLayout.Size = new System.Drawing.Size(608, 241);
        this.sendTableLayout.TabIndex = 0;
        //
        // txtSend
        //
        this.txtSend.Dock = System.Windows.Forms.DockStyle.Fill;
        this.txtSend.Location = new System.Drawing.Point(0, 0);
        this.txtSend.Multiline = true;
        this.txtSend.Name = "txtSend";
        this.txtSend.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtSend.Size = new System.Drawing.Size(428, 206);
        this.txtSend.TabIndex = 0;
        this.txtSend.Font = new System.Drawing.Font("Consolas", 10F);
        //
        // sendOptionsPanel
        //
        this.sendOptionsPanel.Controls.Add(this.lblInterval);
        this.sendOptionsPanel.Controls.Add(this.numAutoSendInterval);
        this.sendOptionsPanel.Controls.Add(this.chkAutoSend);
        this.sendOptionsPanel.Controls.Add(this.chkAddCrc);
        this.sendOptionsPanel.Controls.Add(this.chkHexSend);
        this.sendOptionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
        this.sendOptionsPanel.Location = new System.Drawing.Point(433, 0);
        this.sendOptionsPanel.Name = "sendOptionsPanel";
        this.sendOptionsPanel.Size = new System.Drawing.Size(175, 206);
        this.sendOptionsPanel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
        //
        // chkHexSend
        //
        this.chkHexSend.AutoSize = true;
        this.chkHexSend.Checked = true;
        this.chkHexSend.CheckState = System.Windows.Forms.CheckState.Checked;
        this.chkHexSend.Location = new System.Drawing.Point(10, 8);
        this.chkHexSend.Name = "chkHexSend";
        this.chkHexSend.Size = new System.Drawing.Size(62, 19);
        this.chkHexSend.TabIndex = 0;
        this.chkHexSend.Text = "HEX发送";
        this.chkHexSend.UseVisualStyleBackColor = true;
        //
        // chkAddCrc
        //
        this.chkAddCrc.AutoSize = true;
        this.chkAddCrc.Location = new System.Drawing.Point(10, 35);
        this.chkAddCrc.Name = "chkAddCrc";
        this.chkAddCrc.Size = new System.Drawing.Size(80, 19);
        this.chkAddCrc.TabIndex = 1;
        this.chkAddCrc.Text = "添加CRC校验";
        this.chkAddCrc.UseVisualStyleBackColor = true;
        //
        // chkAutoSend
        //
        this.chkAutoSend.AutoSize = true;
        this.chkAutoSend.Location = new System.Drawing.Point(10, 60);
        this.chkAutoSend.Name = "chkAutoSend";
        this.chkAutoSend.Size = new System.Drawing.Size(85, 19);
        this.chkAutoSend.TabIndex = 2;
        this.chkAutoSend.Text = "自动发送";
        this.chkAutoSend.UseVisualStyleBackColor = true;
        this.chkAutoSend.CheckedChanged += new System.EventHandler(this.chkAutoSend_CheckedChanged);
        //
        // numAutoSendInterval
        //
        this.numAutoSendInterval.Location = new System.Drawing.Point(10, 85);
        this.numAutoSendInterval.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
        this.numAutoSendInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
        this.numAutoSendInterval.Name = "numAutoSendInterval";
        this.numAutoSendInterval.Size = new System.Drawing.Size(70, 23);
        this.numAutoSendInterval.TabIndex = 3;
        this.numAutoSendInterval.Value = new decimal(new int[] { 1, 0, 0, 0 });
        //
        // lblInterval
        //
        this.lblInterval.AutoSize = true;
        this.lblInterval.Location = new System.Drawing.Point(80, 88);
        this.lblInterval.Name = "lblInterval";
        this.lblInterval.Size = new System.Drawing.Size(85, 15);
        this.lblInterval.TabIndex = 4;
        this.lblInterval.Text = "秒/次";
        //
        // btnSend
        //
        this.btnSend.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Highlight);
        this.btnSend.Dock = System.Windows.Forms.DockStyle.Fill;
        this.btnSend.ForeColor = System.Drawing.Color.White;
        this.btnSend.Location = new System.Drawing.Point(3, 209);
        this.btnSend.Name = "btnSend";
        this.btnSend.Size = new System.Drawing.Size(422, 29);
        this.btnSend.TabIndex = 1;
        this.btnSend.Text = "发送";
        this.btnSend.UseVisualStyleBackColor = false;
        this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
        //
        // btnClearSend
        //
        this.btnClearSend.Dock = System.Windows.Forms.DockStyle.Fill;
        this.btnClearSend.Location = new System.Drawing.Point(431, 209);
        this.btnClearSend.Name = "btnClearSend";
        this.btnClearSend.Size = new System.Drawing.Size(174, 29);
        this.btnClearSend.TabIndex = 2;
        this.btnClearSend.Text = "清空";
        this.btnClearSend.UseVisualStyleBackColor = true;
        this.btnClearSend.Click += new System.EventHandler(this.btnClearSend_Click);
        //
        // grpSerialSettings
        //
        this.grpSerialSettings.Controls.Add(this.btnRefreshPorts);
        this.grpSerialSettings.Controls.Add(this.btnConnect);
        this.grpSerialSettings.Controls.Add(this.lblParity);
        this.grpSerialSettings.Controls.Add(this.cmbParity);
        this.grpSerialSettings.Controls.Add(this.lblStopBits);
        this.grpSerialSettings.Controls.Add(this.cmbStopBits);
        this.grpSerialSettings.Controls.Add(this.lblDataBits);
        this.grpSerialSettings.Controls.Add(this.cmbDataBits);
        this.grpSerialSettings.Controls.Add(this.lblBaudRate);
        this.grpSerialSettings.Controls.Add(this.cmbBaudRate);
        this.grpSerialSettings.Controls.Add(this.lblPort);
        this.grpSerialSettings.Controls.Add(this.cmbPort);
        this.grpSerialSettings.Dock = System.Windows.Forms.DockStyle.Fill;
        this.grpSerialSettings.Location = new System.Drawing.Point(633, 3);
        this.grpSerialSettings.Name = "grpSerialSettings";
        this.grpSerialSettings.Padding = new System.Windows.Forms.Padding(5);
        this.grpSerialSettings.Size = new System.Drawing.Size(264, 397);
        this.grpSerialSettings.TabIndex = 2;
        this.grpSerialSettings.TabStop = false;
        this.grpSerialSettings.Text = "串口设置";
        //
        // cmbPort
        //
        this.cmbPort.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbPort.FormattingEnabled = true;
        this.cmbPort.Location = new System.Drawing.Point(8, 28);
        this.cmbPort.Name = "cmbPort";
        this.cmbPort.Size = new System.Drawing.Size(248, 23);
        this.cmbPort.TabIndex = 0;
        //
        // cmbBaudRate
        //
        this.cmbBaudRate.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbBaudRate.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbBaudRate.FormattingEnabled = true;
        this.cmbBaudRate.Location = new System.Drawing.Point(8, 51);
        this.cmbBaudRate.Name = "cmbBaudRate";
        this.cmbBaudRate.Size = new System.Drawing.Size(248, 23);
        this.cmbBaudRate.TabIndex = 1;
        //
        // cmbDataBits
        //
        this.cmbDataBits.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbDataBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbDataBits.FormattingEnabled = true;
        this.cmbDataBits.Location = new System.Drawing.Point(8, 74);
        this.cmbDataBits.Name = "cmbDataBits";
        this.cmbDataBits.Size = new System.Drawing.Size(248, 23);
        this.cmbDataBits.TabIndex = 2;
        //
        // cmbStopBits
        //
        this.cmbStopBits.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbStopBits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbStopBits.FormattingEnabled = true;
        this.cmbStopBits.Location = new System.Drawing.Point(8, 97);
        this.cmbStopBits.Name = "cmbStopBits";
        this.cmbStopBits.Size = new System.Drawing.Size(248, 23);
        this.cmbStopBits.TabIndex = 3;
        //
        // cmbParity
        //
        this.cmbParity.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbParity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbParity.FormattingEnabled = true;
        this.cmbParity.Location = new System.Drawing.Point(8, 120);
        this.cmbParity.Name = "cmbParity";
        this.cmbParity.Size = new System.Drawing.Size(248, 23);
        this.cmbParity.TabIndex = 4;
        //
        // lblPort
        //
        this.lblPort.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblPort.Location = new System.Drawing.Point(8, 145);
        this.lblPort.Name = "lblPort";
        this.lblPort.Size = new System.Drawing.Size(248, 20);
        this.lblPort.TabIndex = 5;
        this.lblPort.Text = "串口号";
        this.lblPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // lblBaudRate
        //
        this.lblBaudRate.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblBaudRate.Location = new System.Drawing.Point(8, 165);
        this.lblBaudRate.Name = "lblBaudRate";
        this.lblBaudRate.Size = new System.Drawing.Size(248, 20);
        this.lblBaudRate.TabIndex = 6;
        this.lblBaudRate.Text = "波特率";
        this.lblBaudRate.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // lblDataBits
        //
        this.lblDataBits.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblDataBits.Location = new System.Drawing.Point(8, 185);
        this.lblDataBits.Name = "lblDataBits";
        this.lblDataBits.Size = new System.Drawing.Size(248, 20);
        this.lblDataBits.TabIndex = 7;
        this.lblDataBits.Text = "数据位";
        this.lblDataBits.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // lblStopBits
        //
        this.lblStopBits.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblStopBits.Location = new System.Drawing.Point(8, 205);
        this.lblStopBits.Name = "lblStopBits";
        this.lblStopBits.Size = new System.Drawing.Size(248, 20);
        this.lblStopBits.TabIndex = 8;
        this.lblStopBits.Text = "停止位";
        this.lblStopBits.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // lblParity
        //
        this.lblParity.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblParity.Location = new System.Drawing.Point(8, 225);
        this.lblParity.Name = "lblParity";
        this.lblParity.Size = new System.Drawing.Size(248, 20);
        this.lblParity.TabIndex = 9;
        this.lblParity.Text = "校验位";
        this.lblParity.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        //
        // btnRefreshPorts
        //
        this.btnRefreshPorts.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.btnRefreshPorts.Location = new System.Drawing.Point(8, 339);
        this.btnRefreshPorts.Name = "btnRefreshPorts";
        this.btnRefreshPorts.Size = new System.Drawing.Size(248, 28);
        this.btnRefreshPorts.TabIndex = 10;
        this.btnRefreshPorts.Text = "刷新串口";
        this.btnRefreshPorts.UseVisualStyleBackColor = true;
        this.btnRefreshPorts.Click += new System.EventHandler(this.btnRefreshPorts_Click);
        //
        // btnConnect
        //
        this.btnConnect.BackColor = System.Drawing.Color.FromKnownColor(System.Drawing.KnownColor.Highlight);
        this.btnConnect.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.btnConnect.ForeColor = System.Drawing.Color.White;
        this.btnConnect.Location = new System.Drawing.Point(8, 367);
        this.btnConnect.Name = "btnConnect";
        this.btnConnect.Size = new System.Drawing.Size(248, 28);
        this.btnConnect.TabIndex = 11;
        this.btnConnect.Text = "连接";
        this.btnConnect.UseVisualStyleBackColor = false;
        this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
        //
        // grpCrc
        //
        this.grpCrc.Controls.Add(this.btnVerifyCrc);
        this.grpCrc.Controls.Add(this.txtCrcVerify);
        this.grpCrc.Controls.Add(this.lblCrcVerify);
        this.grpCrc.Controls.Add(this.lblCrcResult);
        this.grpCrc.Controls.Add(this.btnCalculateCrc);
        this.grpCrc.Controls.Add(this.txtCrcInput);
        this.grpCrc.Controls.Add(this.lblCrcInput);
        this.grpCrc.Controls.Add(this.cmbCrcType);
        this.grpCrc.Controls.Add(this.lblCrcType);
        this.grpCrc.Dock = System.Windows.Forms.DockStyle.Fill;
        this.grpCrc.Location = new System.Drawing.Point(633, 410);
        this.grpCrc.Name = "grpCrc";
        this.grpCrc.Padding = new System.Windows.Forms.Padding(5);
        this.grpCrc.Size = new System.Drawing.Size(264, 282);
        this.grpCrc.TabIndex = 3;
        this.grpCrc.TabStop = false;
        this.grpCrc.Text = "CRC校验工具";
        //
        // lblCrcType
        //
        this.lblCrcType.AutoSize = true;
        this.lblCrcType.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblCrcType.Location = new System.Drawing.Point(8, 28);
        this.lblCrcType.Name = "lblCrcType";
        this.lblCrcType.Size = new System.Drawing.Size(53, 15);
        this.lblCrcType.TabIndex = 0;
        this.lblCrcType.Text = "CRC类型";
        //
        // cmbCrcType
        //
        this.cmbCrcType.Dock = System.Windows.Forms.DockStyle.Top;
        this.cmbCrcType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbCrcType.FormattingEnabled = true;
        this.cmbCrcType.Location = new System.Drawing.Point(8, 43);
        this.cmbCrcType.Name = "cmbCrcType";
        this.cmbCrcType.Size = new System.Drawing.Size(248, 23);
        this.cmbCrcType.TabIndex = 1;
        //
        // txtCrcInput
        //
        this.txtCrcInput.Dock = System.Windows.Forms.DockStyle.Top;
        this.txtCrcInput.Location = new System.Drawing.Point(8, 66);
        this.txtCrcInput.Name = "txtCrcInput";
        this.txtCrcInput.Size = new System.Drawing.Size(248, 23);
        this.txtCrcInput.TabIndex = 2;
        //
        // lblCrcInput
        //
        this.lblCrcInput.AutoSize = true;
        this.lblCrcInput.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblCrcInput.Location = new System.Drawing.Point(8, 89);
        this.lblCrcInput.Name = "lblCrcInput";
        this.lblCrcInput.Size = new System.Drawing.Size(53, 15);
        this.lblCrcInput.TabIndex = 3;
        this.lblCrcInput.Text = "输入数据";
        //
        // btnCalculateCrc
        //
        this.btnCalculateCrc.Dock = System.Windows.Forms.DockStyle.Top;
        this.btnCalculateCrc.Location = new System.Drawing.Point(8, 112);
        this.btnCalculateCrc.Name = "btnCalculateCrc";
        this.btnCalculateCrc.Size = new System.Drawing.Size(248, 28);
        this.btnCalculateCrc.TabIndex = 4;
        this.btnCalculateCrc.Text = "计算CRC";
        this.btnCalculateCrc.UseVisualStyleBackColor = true;
        this.btnCalculateCrc.Click += new System.EventHandler(this.btnCalculateCrc_Click);
        //
        // lblCrcResult
        //
        this.lblCrcResult.AutoSize = true;
        this.lblCrcResult.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblCrcResult.Font = new System.Drawing.Font("Consolas", 10F, System.Drawing.FontStyle.Bold);
        this.lblCrcResult.ForeColor = System.Drawing.Color.Blue;
        this.lblCrcResult.Location = new System.Drawing.Point(8, 140);
        this.lblCrcResult.Name = "lblCrcResult";
        this.lblCrcResult.Size = new System.Drawing.Size(0, 15);
        this.lblCrcResult.TabIndex = 5;
        //
        // txtCrcVerify
        //
        this.txtCrcVerify.Dock = System.Windows.Forms.DockStyle.Top;
        this.txtCrcVerify.Location = new System.Drawing.Point(8, 155);
        this.txtCrcVerify.Name = "txtCrcVerify";
        this.txtCrcVerify.Size = new System.Drawing.Size(248, 23);
        this.txtCrcVerify.TabIndex = 6;
        //
        // lblCrcVerify
        //
        this.lblCrcVerify.AutoSize = true;
        this.lblCrcVerify.Dock = System.Windows.Forms.DockStyle.Top;
        this.lblCrcVerify.Location = new System.Drawing.Point(8, 178);
        this.lblCrcVerify.Name = "lblCrcVerify";
        this.lblCrcVerify.Size = new System.Drawing.Size(53, 15);
        this.lblCrcVerify.TabIndex = 7;
        this.lblCrcVerify.Text = "CRC验证";
        //
        // btnVerifyCrc
        //
        this.btnVerifyCrc.Dock = System.Windows.Forms.DockStyle.Top;
        this.btnVerifyCrc.Location = new System.Drawing.Point(8, 201);
        this.btnVerifyCrc.Name = "btnVerifyCrc";
        this.btnVerifyCrc.Size = new System.Drawing.Size(248, 28);
        this.btnVerifyCrc.TabIndex = 8;
        this.btnVerifyCrc.Text = "验证CRC";
        this.btnVerifyCrc.UseVisualStyleBackColor = true;
        this.btnVerifyCrc.Click += new System.EventHandler(this.btnVerifyCrc_Click);
        //
        // lblStatus
        //
        this.lblStatus.AutoSize = false;
        this.lblStatus.Dock = System.Windows.Forms.DockStyle.Bottom;
        this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
        this.lblStatus.Location = new System.Drawing.Point(0, 680);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(900, 20);
        this.lblStatus.TabIndex = 4;
        this.lblStatus.Text = "未连接";
        this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        this.lblStatus.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
        //
        // MainForm
        //
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(900, 700);
        this.Controls.Add(this.mainLayout);
        this.Controls.Add(this.lblStatus);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox = true;
        this.MinimumSize = new System.Drawing.Size(800, 600);
        this.Name = "MainForm";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "串口通信工具";
        this.mainLayout.ResumeLayout(false);
        this.topPanel.ResumeLayout(false);
        this.grpReceive.ResumeLayout(false);
        this.grpReceive.PerformLayout();
        this.bottomPanel.ResumeLayout(false);
        this.grpSend.ResumeLayout(false);
        this.grpSend.PerformLayout();
        this.sendTableLayout.ResumeLayout(false);
        this.sendOptionsPanel.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.numAutoSendInterval)).EndInit();
        this.grpSerialSettings.ResumeLayout(false);
        this.grpSerialSettings.PerformLayout();
        this.grpCrc.ResumeLayout(false);
        this.grpCrc.PerformLayout();
        this.ResumeLayout(false);
    }

    private System.Windows.Forms.TableLayoutPanel mainLayout;
    private System.Windows.Forms.Panel topPanel;
    private System.Windows.Forms.Panel bottomPanel;
    private System.Windows.Forms.GroupBox grpReceive;
    private System.Windows.Forms.TextBox txtReceive;
    private System.Windows.Forms.Button btnClearReceive;
    private System.Windows.Forms.CheckBox chkHexReceive;
    private System.Windows.Forms.GroupBox grpSend;
    private System.Windows.Forms.TableLayoutPanel sendTableLayout;
    private System.Windows.Forms.Panel sendOptionsPanel;
    private System.Windows.Forms.TextBox txtSend;
    private System.Windows.Forms.Button btnSend;
    private System.Windows.Forms.Button btnClearSend;
    private System.Windows.Forms.CheckBox chkHexSend;
    private System.Windows.Forms.CheckBox chkAddCrc;
    private System.Windows.Forms.CheckBox chkAutoSend;
    private System.Windows.Forms.NumericUpDown numAutoSendInterval;
    private System.Windows.Forms.Label lblInterval;
    private System.Windows.Forms.GroupBox grpSerialSettings;
    private System.Windows.Forms.Label lblPort;
    private System.Windows.Forms.ComboBox cmbPort;
    private System.Windows.Forms.Label lblBaudRate;
    private System.Windows.Forms.ComboBox cmbBaudRate;
    private System.Windows.Forms.Label lblDataBits;
    private System.Windows.Forms.ComboBox cmbDataBits;
    private System.Windows.Forms.Label lblStopBits;
    private System.Windows.Forms.ComboBox cmbStopBits;
    private System.Windows.Forms.Label lblParity;
    private System.Windows.Forms.ComboBox cmbParity;
    private System.Windows.Forms.Button btnRefreshPorts;
    private System.Windows.Forms.Button btnConnect;
    private System.Windows.Forms.GroupBox grpCrc;
    private System.Windows.Forms.Label lblCrcType;
    private System.Windows.Forms.ComboBox cmbCrcType;
    private System.Windows.Forms.TextBox txtCrcInput;
    private System.Windows.Forms.Label lblCrcInput;
    private System.Windows.Forms.Button btnCalculateCrc;
    private System.Windows.Forms.Label lblCrcResult;
    private System.Windows.Forms.TextBox txtCrcVerify;
    private System.Windows.Forms.Label lblCrcVerify;
    private System.Windows.Forms.Button btnVerifyCrc;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Timer timer1;
    private System.Windows.Forms.Timer autoSendTimer;
}