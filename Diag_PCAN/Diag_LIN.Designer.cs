
namespace Diag_BUS
{
    partial class Diag_LIN
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Diag_LIN));
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.cbbChannel = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnRelease = new System.Windows.Forms.Button();
            this.chbCanFD = new System.Windows.Forms.CheckBox();
            this.cbProject = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cbbBaudrates = new System.Windows.Forms.ComboBox();
            this.laBaudrate = new System.Windows.Forms.Label();
            this.laBitrate = new System.Windows.Forms.Label();
            this.btnHwRefresh = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.btnInit = new System.Windows.Forms.Button();
            this.cbbHwType = new System.Windows.Forms.ComboBox();
            this.laHwType = new System.Windows.Forms.Label();
            this.cbbInterrupt = new System.Windows.Forms.ComboBox();
            this.laInterrupt = new System.Windows.Forms.Label();
            this.cbbIO = new System.Windows.Forms.ComboBox();
            this.laIOPort = new System.Windows.Forms.Label();
            this.txtBitrate = new System.Windows.Forms.TextBox();
            this.tcDownloader = new System.Windows.Forms.TabControl();
            this.tpDownloader = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.panel2 = new System.Windows.Forms.Panel();
            this.btnResetECU = new System.Windows.Forms.Button();
            this.btnResetDID = new System.Windows.Forms.Button();
            this.btnFlashAddr = new System.Windows.Forms.Button();
            this.btnWriteDID = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnReadHexFile = new System.Windows.Forms.Button();
            this.tbDownload = new System.Windows.Forms.Button();
            this.btnInfoClear = new System.Windows.Forms.Button();
            this.pBar = new System.Windows.Forms.ProgressBar();
            this.lbFilePath = new System.Windows.Forms.Label();
            this.lbxInfo = new System.Windows.Forms.ListBox();
            this.lbPath = new System.Windows.Forms.Label();
            this.Trace = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbWholeTrace = new System.Windows.Forms.CheckBox();
            this.cbEnAPPMsg = new System.Windows.Forms.CheckBox();
            this.Test = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnTest = new System.Windows.Forms.Button();
            this.numUpDownNAD = new System.Windows.Forms.NumericUpDown();
            this.btnExportTrace = new System.Windows.Forms.Button();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.btnClearDTC = new System.Windows.Forms.Button();
            this.btnReadDTC = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.nudIdTo = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.nudIdFrom = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.chbShowPeriod = new System.Windows.Forms.CheckBox();
            this.btnClear = new System.Windows.Forms.Button();
            this.dgView = new System.Windows.Forms.DataGridView();
            this.CoID = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoDir = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoLength = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoCount = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoTime = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.CoData = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tmrDisplay = new System.Windows.Forms.Timer(this.components);
            this.tmrMsg = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.groupBox1.SuspendLayout();
            this.tcDownloader.SuspendLayout();
            this.tpDownloader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel2.SuspendLayout();
            this.Trace.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.Test.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNAD)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIdTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIdFrom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgView)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.cbbChannel);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.btnRelease);
            this.groupBox1.Controls.Add(this.chbCanFD);
            this.groupBox1.Controls.Add(this.cbProject);
            this.groupBox1.Controls.Add(this.label6);
            this.groupBox1.Controls.Add(this.cbbBaudrates);
            this.groupBox1.Controls.Add(this.laBaudrate);
            this.groupBox1.Controls.Add(this.laBitrate);
            this.groupBox1.Controls.Add(this.btnHwRefresh);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.btnInit);
            this.groupBox1.Controls.Add(this.cbbHwType);
            this.groupBox1.Controls.Add(this.laHwType);
            this.groupBox1.Controls.Add(this.cbbInterrupt);
            this.groupBox1.Controls.Add(this.laInterrupt);
            this.groupBox1.Controls.Add(this.cbbIO);
            this.groupBox1.Controls.Add(this.laIOPort);
            this.groupBox1.Controls.Add(this.txtBitrate);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(1419, 115);
            this.groupBox1.TabIndex = 43;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = " Connection ";
            // 
            // cbbChannel
            // 
            this.cbbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbChannel.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbbChannel.Items.AddRange(new object[] {
            "None",
            "DNG-Channel 1",
            "ISA-Channel 1",
            "ISA-Channel 2",
            "ISA-Channel 3",
            "ISA-Channel 4",
            "ISA-Channel 5",
            "ISA-Channel 6",
            "ISA-Channel 7",
            "ISA-Channel 8",
            "PCC-Channel 1",
            "PCC-Channel 2",
            "PCI-Channel 1",
            "PCI-Channel 2",
            "PCI-Channel 3",
            "PCI-Channel 4",
            "PCI-Channel 5",
            "PCI-Channel 6",
            "PCI-Channel 7",
            "PCI-Channel 8",
            "USB-Channel 1",
            "USB-Channel 2",
            "USB-Channel 3",
            "USB-Channel 4",
            "USB-Channel 5",
            "USB-Channel 6",
            "USB-Channel 7",
            "USB-Channel 8"});
            this.cbbChannel.Location = new System.Drawing.Point(595, 28);
            this.cbbChannel.Margin = new System.Windows.Forms.Padding(4);
            this.cbbChannel.Name = "cbbChannel";
            this.cbbChannel.Size = new System.Drawing.Size(199, 33);
            this.cbbChannel.TabIndex = 32;
            this.cbbChannel.SelectedIndexChanged += new System.EventHandler(this.cbbChannel_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(497, 32);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(91, 25);
            this.label4.TabIndex = 60;
            this.label4.Text = "Adapter";
            // 
            // btnRelease
            // 
            this.btnRelease.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnRelease.Enabled = false;
            this.btnRelease.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnRelease.Location = new System.Drawing.Point(34, 76);
            this.btnRelease.Margin = new System.Windows.Forms.Padding(4);
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Size = new System.Drawing.Size(142, 32);
            this.btnRelease.TabIndex = 35;
            this.btnRelease.Text = "Release";
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // chbCanFD
            // 
            this.chbCanFD.AutoSize = true;
            this.chbCanFD.Location = new System.Drawing.Point(1288, 29);
            this.chbCanFD.Margin = new System.Windows.Forms.Padding(4);
            this.chbCanFD.Name = "chbCanFD";
            this.chbCanFD.Size = new System.Drawing.Size(117, 29);
            this.chbCanFD.TabIndex = 59;
            this.chbCanFD.Text = "CAN-FD";
            this.chbCanFD.UseVisualStyleBackColor = true;
            this.chbCanFD.Visible = false;
            this.chbCanFD.CheckedChanged += new System.EventHandler(this.chbCanFD_CheckedChanged);
            // 
            // cbProject
            // 
            this.cbProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProject.Items.AddRange(new object[] {
            "320V Compressor",
            "400V Compressor",
            "XC2234",
            "7Kw(BIN)",
            "N2S",
            "CAN UDS(7840)",
            "CAN UDS(7801)",
            "LIN Hex",
            "Split Flash(CAN)",
            "Chery CBF"});
            this.cbProject.Location = new System.Drawing.Point(303, 28);
            this.cbProject.Margin = new System.Windows.Forms.Padding(4);
            this.cbProject.Name = "cbProject";
            this.cbProject.Size = new System.Drawing.Size(176, 33);
            this.cbProject.TabIndex = 49;
            this.cbProject.SelectedIndexChanged += new System.EventHandler(this.cbProject_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(184, 32);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(94, 28);
            this.label6.TabIndex = 53;
            this.label6.Text = "Project";
            // 
            // cbbBaudrates
            // 
            this.cbbBaudrates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbBaudrates.Items.AddRange(new object[] {
            "1 MBit/sec",
            "500 kBit/sec",
            "250 kBit/sec",
            "125 kBit/sec",
            "100 kBit/sec",
            "19.2 kBit/sec"});
            this.cbbBaudrates.Location = new System.Drawing.Point(303, 78);
            this.cbbBaudrates.Margin = new System.Windows.Forms.Padding(4);
            this.cbbBaudrates.Name = "cbbBaudrates";
            this.cbbBaudrates.Size = new System.Drawing.Size(176, 33);
            this.cbbBaudrates.TabIndex = 49;
            // 
            // laBaudrate
            // 
            this.laBaudrate.Location = new System.Drawing.Point(184, 82);
            this.laBaudrate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laBaudrate.Name = "laBaudrate";
            this.laBaudrate.Size = new System.Drawing.Size(110, 28);
            this.laBaudrate.TabIndex = 53;
            this.laBaudrate.Text = "Baudrate";
            // 
            // laBitrate
            // 
            this.laBitrate.AutoSize = true;
            this.laBitrate.Location = new System.Drawing.Point(522, 28);
            this.laBitrate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laBitrate.Name = "laBitrate";
            this.laBitrate.Size = new System.Drawing.Size(97, 25);
            this.laBitrate.TabIndex = 46;
            this.laBitrate.Text = "Bit rate:";
            this.laBitrate.Visible = false;
            // 
            // btnHwRefresh
            // 
            this.btnHwRefresh.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnHwRefresh.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnHwRefresh.Location = new System.Drawing.Point(1138, 30);
            this.btnHwRefresh.Margin = new System.Windows.Forms.Padding(4);
            this.btnHwRefresh.Name = "btnHwRefresh";
            this.btnHwRefresh.Size = new System.Drawing.Size(142, 32);
            this.btnHwRefresh.TabIndex = 45;
            this.btnHwRefresh.Text = "Refresh";
            this.btnHwRefresh.Visible = false;
            this.btnHwRefresh.Click += new System.EventHandler(this.btnHwRefresh_Click);
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(1143, 74);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(201, 32);
            this.label1.TabIndex = 40;
            this.label1.Text = "Hardware:";
            this.label1.Visible = false;
            // 
            // btnInit
            // 
            this.btnInit.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnInit.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnInit.Location = new System.Drawing.Point(31, 29);
            this.btnInit.Margin = new System.Windows.Forms.Padding(4);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(142, 32);
            this.btnInit.TabIndex = 34;
            this.btnInit.Text = "Initialize";
            this.btnInit.Click += new System.EventHandler(this.btnInit_Click);
            // 
            // cbbHwType
            // 
            this.cbbHwType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbHwType.Items.AddRange(new object[] {
            "ISA-82C200",
            "ISA-SJA1000",
            "ISA-PHYTEC",
            "DNG-82C200",
            "DNG-82C200 EPP",
            "DNG-SJA1000",
            "DNG-SJA1000 EPP",
            "PCAN",
            "TOMOSS"});
            this.cbbHwType.Location = new System.Drawing.Point(595, 70);
            this.cbbHwType.Margin = new System.Windows.Forms.Padding(4);
            this.cbbHwType.Name = "cbbHwType";
            this.cbbHwType.Size = new System.Drawing.Size(199, 33);
            this.cbbHwType.TabIndex = 50;
            this.cbbHwType.Visible = false;
            // 
            // laHwType
            // 
            this.laHwType.Location = new System.Drawing.Point(487, 73);
            this.laHwType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laHwType.Name = "laHwType";
            this.laHwType.Size = new System.Drawing.Size(111, 32);
            this.laHwType.TabIndex = 54;
            this.laHwType.Text = "Hardware Type:";
            this.laHwType.Visible = false;
            // 
            // cbbInterrupt
            // 
            this.cbbInterrupt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbInterrupt.Items.AddRange(new object[] {
            "3",
            "4",
            "5",
            "7",
            "9",
            "10",
            "11",
            "12",
            "15"});
            this.cbbInterrupt.Location = new System.Drawing.Point(1033, 70);
            this.cbbInterrupt.Margin = new System.Windows.Forms.Padding(4);
            this.cbbInterrupt.Name = "cbbInterrupt";
            this.cbbInterrupt.Size = new System.Drawing.Size(80, 33);
            this.cbbInterrupt.TabIndex = 52;
            this.cbbInterrupt.Visible = false;
            // 
            // laInterrupt
            // 
            this.laInterrupt.Location = new System.Drawing.Point(945, 70);
            this.laInterrupt.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laInterrupt.Name = "laInterrupt";
            this.laInterrupt.Size = new System.Drawing.Size(80, 32);
            this.laInterrupt.TabIndex = 56;
            this.laInterrupt.Text = "Interrupt:";
            this.laInterrupt.Visible = false;
            // 
            // cbbIO
            // 
            this.cbbIO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbIO.Items.AddRange(new object[] {
            "0100",
            "0120",
            "0140",
            "0200",
            "0220",
            "0240",
            "0260",
            "0278",
            "0280",
            "02A0",
            "02C0",
            "02E0",
            "02E8",
            "02F8",
            "0300",
            "0320",
            "0340",
            "0360",
            "0378",
            "0380",
            "03BC",
            "03E0",
            "03E8",
            "03F8"});
            this.cbbIO.Location = new System.Drawing.Point(857, 70);
            this.cbbIO.Margin = new System.Windows.Forms.Padding(4);
            this.cbbIO.Name = "cbbIO";
            this.cbbIO.Size = new System.Drawing.Size(80, 33);
            this.cbbIO.TabIndex = 51;
            this.cbbIO.Visible = false;
            // 
            // laIOPort
            // 
            this.laIOPort.Location = new System.Drawing.Point(796, 70);
            this.laIOPort.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.laIOPort.Name = "laIOPort";
            this.laIOPort.Size = new System.Drawing.Size(53, 32);
            this.laIOPort.TabIndex = 55;
            this.laIOPort.Text = "I/O Port:";
            this.laIOPort.Visible = false;
            // 
            // txtBitrate
            // 
            this.txtBitrate.Location = new System.Drawing.Point(527, 59);
            this.txtBitrate.Margin = new System.Windows.Forms.Padding(4);
            this.txtBitrate.Multiline = true;
            this.txtBitrate.Name = "txtBitrate";
            this.txtBitrate.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtBitrate.Size = new System.Drawing.Size(544, 46);
            this.txtBitrate.TabIndex = 48;
            this.txtBitrate.Visible = false;
            // 
            // tcDownloader
            // 
            this.tcDownloader.Controls.Add(this.tpDownloader);
            this.tcDownloader.Controls.Add(this.Trace);
            this.tcDownloader.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tcDownloader.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tcDownloader.Location = new System.Drawing.Point(0, 115);
            this.tcDownloader.Name = "tcDownloader";
            this.tcDownloader.SelectedIndex = 0;
            this.tcDownloader.Size = new System.Drawing.Size(1419, 782);
            this.tcDownloader.TabIndex = 44;
            // 
            // tpDownloader
            // 
            this.tpDownloader.Controls.Add(this.splitContainer2);
            this.tpDownloader.Controls.Add(this.lbPath);
            this.tpDownloader.Location = new System.Drawing.Point(4, 34);
            this.tpDownloader.Name = "tpDownloader";
            this.tpDownloader.Padding = new System.Windows.Forms.Padding(3);
            this.tpDownloader.Size = new System.Drawing.Size(1411, 744);
            this.tpDownloader.TabIndex = 0;
            this.tpDownloader.Text = "Flash";
            this.tpDownloader.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.panel2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.lbxInfo);
            this.splitContainer2.Size = new System.Drawing.Size(1405, 738);
            this.splitContainer2.SplitterDistance = 83;
            this.splitContainer2.TabIndex = 64;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.btnResetECU);
            this.panel2.Controls.Add(this.btnResetDID);
            this.panel2.Controls.Add(this.btnFlashAddr);
            this.panel2.Controls.Add(this.btnWriteDID);
            this.panel2.Controls.Add(this.btnBrowse);
            this.panel2.Controls.Add(this.btnReadHexFile);
            this.panel2.Controls.Add(this.tbDownload);
            this.panel2.Controls.Add(this.btnInfoClear);
            this.panel2.Controls.Add(this.pBar);
            this.panel2.Controls.Add(this.lbFilePath);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1405, 83);
            this.panel2.TabIndex = 64;
            // 
            // btnResetECU
            // 
            this.btnResetECU.Enabled = false;
            this.btnResetECU.Location = new System.Drawing.Point(1238, 54);
            this.btnResetECU.Name = "btnResetECU";
            this.btnResetECU.Size = new System.Drawing.Size(150, 36);
            this.btnResetECU.TabIndex = 63;
            this.btnResetECU.Text = "Reset ECU";
            this.toolTip.SetToolTip(this.btnResetECU, "Send 0x11 01 to ECU(soft reset)");
            this.btnResetECU.UseVisualStyleBackColor = true;
            this.btnResetECU.Visible = false;
            this.btnResetECU.Click += new System.EventHandler(this.btnResetECU_Click);
            // 
            // btnResetDID
            // 
            this.btnResetDID.Enabled = false;
            this.btnResetDID.Location = new System.Drawing.Point(1082, 54);
            this.btnResetDID.Name = "btnResetDID";
            this.btnResetDID.Size = new System.Drawing.Size(150, 36);
            this.btnResetDID.TabIndex = 63;
            this.btnResetDID.Text = "Reset DID";
            this.toolTip.SetToolTip(this.btnResetDID, "Reset DID counter,if project changed.");
            this.btnResetDID.UseVisualStyleBackColor = true;
            this.btnResetDID.Visible = false;
            this.btnResetDID.Click += new System.EventHandler(this.btnResetDID_Click);
            // 
            // btnFlashAddr
            // 
            this.btnFlashAddr.Enabled = false;
            this.btnFlashAddr.Location = new System.Drawing.Point(1238, 12);
            this.btnFlashAddr.Name = "btnFlashAddr";
            this.btnFlashAddr.Size = new System.Drawing.Size(150, 36);
            this.btnFlashAddr.TabIndex = 63;
            this.btnFlashAddr.Text = "Flash Addr";
            this.toolTip.SetToolTip(this.btnFlashAddr, "Flash address setting;\r\nAPP: end address of application\r\nCAL:start address of cal" +
        "ibration");
            this.btnFlashAddr.UseVisualStyleBackColor = true;
            this.btnFlashAddr.Visible = false;
            this.btnFlashAddr.Click += new System.EventHandler(this.btnFlashAddr_Click);
            // 
            // btnWriteDID
            // 
            this.btnWriteDID.Location = new System.Drawing.Point(1082, 11);
            this.btnWriteDID.Name = "btnWriteDID";
            this.btnWriteDID.Size = new System.Drawing.Size(150, 36);
            this.btnWriteDID.TabIndex = 63;
            this.btnWriteDID.Text = "Write DID";
            this.toolTip.SetToolTip(this.btnWriteDID, "Write DID info into PTC.");
            this.btnWriteDID.UseVisualStyleBackColor = true;
            this.btnWriteDID.Visible = false;
            this.btnWriteDID.Click += new System.EventHandler(this.btnWriteDID_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Enabled = false;
            this.btnBrowse.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnBrowse.Location = new System.Drawing.Point(768, 11);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(150, 38);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse";
            this.toolTip.SetToolTip(this.btnBrowse, "Import .hex/.bin file for flash PTC.");
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnReadHexFile
            // 
            this.btnReadHexFile.Enabled = false;
            this.btnReadHexFile.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReadHexFile.Location = new System.Drawing.Point(768, 55);
            this.btnReadHexFile.Name = "btnReadHexFile";
            this.btnReadHexFile.Size = new System.Drawing.Size(150, 35);
            this.btnReadHexFile.TabIndex = 62;
            this.btnReadHexFile.Text = "ReadHex";
            this.toolTip.SetToolTip(this.btnReadHexFile, "Read .hex file info.");
            this.btnReadHexFile.UseVisualStyleBackColor = true;
            this.btnReadHexFile.Click += new System.EventHandler(this.btnReadHexFile_Click);
            // 
            // tbDownload
            // 
            this.tbDownload.Enabled = false;
            this.tbDownload.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbDownload.Location = new System.Drawing.Point(926, 11);
            this.tbDownload.Name = "tbDownload";
            this.tbDownload.Size = new System.Drawing.Size(150, 37);
            this.tbDownload.TabIndex = 1;
            this.tbDownload.Text = "Download";
            this.toolTip.SetToolTip(this.tbDownload, "Excute download .hex/.bin file into PTC.");
            this.tbDownload.UseVisualStyleBackColor = true;
            this.tbDownload.Click += new System.EventHandler(this.tbDownload_Click);
            // 
            // btnInfoClear
            // 
            this.btnInfoClear.Enabled = false;
            this.btnInfoClear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnInfoClear.Font = new System.Drawing.Font("Verdana", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInfoClear.Location = new System.Drawing.Point(926, 55);
            this.btnInfoClear.Margin = new System.Windows.Forms.Padding(4);
            this.btnInfoClear.Name = "btnInfoClear";
            this.btnInfoClear.Size = new System.Drawing.Size(150, 35);
            this.btnInfoClear.TabIndex = 61;
            this.btnInfoClear.Text = "Clear";
            this.toolTip.SetToolTip(this.btnInfoClear, "Clear past messages in message window.");
            this.btnInfoClear.UseVisualStyleBackColor = true;
            this.btnInfoClear.Click += new System.EventHandler(this.btnInfoClear_Click);
            // 
            // pBar
            // 
            this.pBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            this.pBar.Location = new System.Drawing.Point(9, 54);
            this.pBar.Name = "pBar";
            this.pBar.Size = new System.Drawing.Size(743, 23);
            this.pBar.Step = 1;
            this.pBar.TabIndex = 4;
            // 
            // lbFilePath
            // 
            this.lbFilePath.AutoSize = true;
            this.lbFilePath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.lbFilePath.Location = new System.Drawing.Point(12, 91);
            this.lbFilePath.Name = "lbFilePath";
            this.lbFilePath.Size = new System.Drawing.Size(19, 25);
            this.lbFilePath.TabIndex = 5;
            this.lbFilePath.Text = " ";
            // 
            // lbxInfo
            // 
            this.lbxInfo.BackColor = System.Drawing.SystemColors.Desktop;
            this.lbxInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbxInfo.Font = new System.Drawing.Font("Verdana", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbxInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.lbxInfo.FormattingEnabled = true;
            this.lbxInfo.HorizontalScrollbar = true;
            this.lbxInfo.ItemHeight = 22;
            this.lbxInfo.Items.AddRange(new object[] {
            " "});
            this.lbxInfo.Location = new System.Drawing.Point(0, 0);
            this.lbxInfo.Name = "lbxInfo";
            this.lbxInfo.ScrollAlwaysVisible = true;
            this.lbxInfo.Size = new System.Drawing.Size(1405, 651);
            this.lbxInfo.TabIndex = 6;
            // 
            // lbPath
            // 
            this.lbPath.AutoSize = true;
            this.lbPath.Location = new System.Drawing.Point(27, 30);
            this.lbPath.Name = "lbPath";
            this.lbPath.Size = new System.Drawing.Size(0, 25);
            this.lbPath.TabIndex = 0;
            // 
            // Trace
            // 
            this.Trace.Controls.Add(this.splitContainer1);
            this.Trace.Location = new System.Drawing.Point(4, 34);
            this.Trace.Name = "Trace";
            this.Trace.Padding = new System.Windows.Forms.Padding(3);
            this.Trace.Size = new System.Drawing.Size(1411, 744);
            this.Trace.TabIndex = 1;
            this.Trace.Text = "Trace";
            this.Trace.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgView);
            this.splitContainer1.Size = new System.Drawing.Size(1405, 738);
            this.splitContainer1.SplitterDistance = 82;
            this.splitContainer1.TabIndex = 2;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbWholeTrace);
            this.panel1.Controls.Add(this.cbEnAPPMsg);
            this.panel1.Controls.Add(this.Test);
            this.panel1.Controls.Add(this.btnExportTrace);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.chbShowPeriod);
            this.panel1.Controls.Add(this.btnClear);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1405, 82);
            this.panel1.TabIndex = 2;
            // 
            // cbWholeTrace
            // 
            this.cbWholeTrace.AutoSize = true;
            this.cbWholeTrace.Location = new System.Drawing.Point(800, 85);
            this.cbWholeTrace.Name = "cbWholeTrace";
            this.cbWholeTrace.Size = new System.Drawing.Size(159, 29);
            this.cbWholeTrace.TabIndex = 83;
            this.cbWholeTrace.Text = "Whole trace";
            this.toolTip.SetToolTip(this.cbWholeTrace, "Cause NPOI library limit,when save trace into excel file,\r\nmessages count can not" +
        " beyond 65535,otherwise save action fail.\r\n");
            this.cbWholeTrace.UseVisualStyleBackColor = true;
            this.cbWholeTrace.CheckedChanged += new System.EventHandler(this.cbWholeTrace_CheckedChanged);
            // 
            // cbEnAPPMsg
            // 
            this.cbEnAPPMsg.AutoSize = true;
            this.cbEnAPPMsg.Checked = true;
            this.cbEnAPPMsg.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnAPPMsg.Location = new System.Drawing.Point(800, 21);
            this.cbEnAPPMsg.Name = "cbEnAPPMsg";
            this.cbEnAPPMsg.Size = new System.Drawing.Size(170, 29);
            this.cbEnAPPMsg.TabIndex = 83;
            this.cbEnAPPMsg.Text = "APP message";
            this.toolTip.SetToolTip(this.cbEnAPPMsg, "Display app message or not.");
            this.cbEnAPPMsg.UseVisualStyleBackColor = true;
            this.cbEnAPPMsg.Visible = false;
            this.cbEnAPPMsg.CheckedChanged += new System.EventHandler(this.cbEnAPPMsg_CheckedChanged);
            // 
            // Test
            // 
            this.Test.Controls.Add(this.label5);
            this.Test.Controls.Add(this.btnTest);
            this.Test.Controls.Add(this.numUpDownNAD);
            this.Test.Location = new System.Drawing.Point(477, 8);
            this.Test.Name = "Test";
            this.Test.Size = new System.Drawing.Size(304, 109);
            this.Test.TabIndex = 92;
            this.Test.TabStop = false;
            this.Test.Text = "Test Diag Service";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 26);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(56, 25);
            this.label5.TabIndex = 91;
            this.label5.Text = "NAD";
            // 
            // btnTest
            // 
            this.btnTest.Enabled = false;
            this.btnTest.Location = new System.Drawing.Point(147, 50);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(135, 36);
            this.btnTest.TabIndex = 0;
            this.btnTest.Text = "Test";
            this.toolTip.SetToolTip(this.btnTest, "Test UDS supported service,that has template in install path.\r\nwhich name is \'UDS" +
        "_Service.xlsx\'");
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // numUpDownNAD
            // 
            this.numUpDownNAD.Hexadecimal = true;
            this.numUpDownNAD.Location = new System.Drawing.Point(20, 54);
            this.numUpDownNAD.Margin = new System.Windows.Forms.Padding(4);
            this.numUpDownNAD.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numUpDownNAD.Name = "numUpDownNAD";
            this.numUpDownNAD.Size = new System.Drawing.Size(104, 32);
            this.numUpDownNAD.TabIndex = 90;
            this.toolTip.SetToolTip(this.numUpDownNAD, "NAD value support on LIN bus only");
            this.numUpDownNAD.Value = new decimal(new int[] {
            66,
            0,
            0,
            0});
            // 
            // btnExportTrace
            // 
            this.btnExportTrace.Location = new System.Drawing.Point(1141, 71);
            this.btnExportTrace.Name = "btnExportTrace";
            this.btnExportTrace.Size = new System.Drawing.Size(206, 36);
            this.btnExportTrace.TabIndex = 0;
            this.btnExportTrace.Text = "Export Trace";
            this.toolTip.SetToolTip(this.btnExportTrace, "Export trace to excel file.");
            this.btnExportTrace.UseVisualStyleBackColor = true;
            this.btnExportTrace.Click += new System.EventHandler(this.btnExportTrace_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btnClearDTC);
            this.groupBox4.Controls.Add(this.btnReadDTC);
            this.groupBox4.Location = new System.Drawing.Point(297, 8);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(172, 109);
            this.groupBox4.TabIndex = 84;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "DTC";
            // 
            // btnClearDTC
            // 
            this.btnClearDTC.Enabled = false;
            this.btnClearDTC.Location = new System.Drawing.Point(17, 64);
            this.btnClearDTC.Name = "btnClearDTC";
            this.btnClearDTC.Size = new System.Drawing.Size(135, 35);
            this.btnClearDTC.TabIndex = 0;
            this.btnClearDTC.Text = "Clear";
            this.toolTip.SetToolTip(this.btnClearDTC, "use to clear ECUs\' DTC");
            this.btnClearDTC.UseVisualStyleBackColor = true;
            this.btnClearDTC.Click += new System.EventHandler(this.btnClearDTC_Click);
            // 
            // btnReadDTC
            // 
            this.btnReadDTC.Enabled = false;
            this.btnReadDTC.Location = new System.Drawing.Point(17, 23);
            this.btnReadDTC.Name = "btnReadDTC";
            this.btnReadDTC.Size = new System.Drawing.Size(135, 35);
            this.btnReadDTC.TabIndex = 0;
            this.btnReadDTC.Text = "Read";
            this.toolTip.SetToolTip(this.btnReadDTC, "use to read ECUs\' DTC.");
            this.btnReadDTC.UseVisualStyleBackColor = true;
            this.btnReadDTC.Click += new System.EventHandler(this.btnReadDTC_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.nudIdTo);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.nudIdFrom);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Location = new System.Drawing.Point(6, 8);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(281, 108);
            this.groupBox3.TabIndex = 83;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Diagnostic ID";
            // 
            // nudIdTo
            // 
            this.nudIdTo.Hexadecimal = true;
            this.nudIdTo.Location = new System.Drawing.Point(115, 25);
            this.nudIdTo.Margin = new System.Windows.Forms.Padding(4);
            this.nudIdTo.Maximum = new decimal(new int[] {
            536870911,
            0,
            0,
            0});
            this.nudIdTo.Name = "nudIdTo";
            this.nudIdTo.Size = new System.Drawing.Size(151, 32);
            this.nudIdTo.TabIndex = 82;
            this.toolTip.SetToolTip(this.nudIdTo, "UDS request message\'s ID.");
            this.nudIdTo.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.label2.Location = new System.Drawing.Point(8, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 25);
            this.label2.TabIndex = 78;
            this.label2.Text = "Rqst(h)";
            // 
            // nudIdFrom
            // 
            this.nudIdFrom.Hexadecimal = true;
            this.nudIdFrom.Location = new System.Drawing.Point(115, 65);
            this.nudIdFrom.Margin = new System.Windows.Forms.Padding(4);
            this.nudIdFrom.Maximum = new decimal(new int[] {
            536870911,
            0,
            0,
            0});
            this.nudIdFrom.Name = "nudIdFrom";
            this.nudIdFrom.Size = new System.Drawing.Size(151, 32);
            this.nudIdFrom.TabIndex = 81;
            this.toolTip.SetToolTip(this.nudIdFrom, "UDS response message\'s ID.");
            this.nudIdFrom.Value = new decimal(new int[] {
            61,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.label3.Location = new System.Drawing.Point(9, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(89, 25);
            this.label3.TabIndex = 77;
            this.label3.Text = "Rsps(h)";
            // 
            // chbShowPeriod
            // 
            this.chbShowPeriod.AutoSize = true;
            this.chbShowPeriod.Location = new System.Drawing.Point(800, 53);
            this.chbShowPeriod.Margin = new System.Windows.Forms.Padding(4);
            this.chbShowPeriod.Name = "chbShowPeriod";
            this.chbShowPeriod.Size = new System.Drawing.Size(182, 29);
            this.chbShowPeriod.TabIndex = 76;
            this.chbShowPeriod.Text = "TimestampSW";
            this.toolTip.SetToolTip(this.chbShowPeriod, "Message in trace window,display relative time/real time.");
            this.chbShowPeriod.UseVisualStyleBackColor = true;
            this.chbShowPeriod.CheckedChanged += new System.EventHandler(this.chbShowPeriod_CheckedChanged);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(1139, 19);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(206, 38);
            this.btnClear.TabIndex = 0;
            this.btnClear.Text = "Clear message list";
            this.toolTip.SetToolTip(this.btnClear, "Clear trace window messages record.");
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // dgView
            // 
            this.dgView.AllowUserToAddRows = false;
            this.dgView.AllowUserToDeleteRows = false;
            this.dgView.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.Color.Blue;
            this.dgView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CoID,
            this.CoDir,
            this.CoLength,
            this.CoCount,
            this.CoTime,
            this.CoData});
            this.dgView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgView.Location = new System.Drawing.Point(0, 0);
            this.dgView.Name = "dgView";
            this.dgView.ReadOnly = true;
            this.dgView.RowHeadersWidth = 62;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.Black;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.Blue;
            this.dgView.RowsDefaultCellStyle = dataGridViewCellStyle2;
            this.dgView.RowTemplate.Height = 30;
            this.dgView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgView.Size = new System.Drawing.Size(1405, 652);
            this.dgView.TabIndex = 2;
            this.dgView.VirtualMode = true;
            this.dgView.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgView_CellValueNeeded);
            this.dgView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgView_KeyDown);
            // 
            // CoID
            // 
            this.CoID.HeaderText = "ID";
            this.CoID.MinimumWidth = 8;
            this.CoID.Name = "CoID";
            this.CoID.ReadOnly = true;
            this.CoID.Width = 160;
            // 
            // CoDir
            // 
            this.CoDir.HeaderText = "Dir";
            this.CoDir.MinimumWidth = 8;
            this.CoDir.Name = "CoDir";
            this.CoDir.ReadOnly = true;
            this.CoDir.Width = 60;
            // 
            // CoLength
            // 
            this.CoLength.HeaderText = "Length";
            this.CoLength.MinimumWidth = 8;
            this.CoLength.Name = "CoLength";
            this.CoLength.ReadOnly = true;
            this.CoLength.Width = 80;
            // 
            // CoCount
            // 
            this.CoCount.HeaderText = "Count";
            this.CoCount.MinimumWidth = 8;
            this.CoCount.Name = "CoCount";
            this.CoCount.ReadOnly = true;
            this.CoCount.Width = 80;
            // 
            // CoTime
            // 
            this.CoTime.HeaderText = "Time";
            this.CoTime.MinimumWidth = 8;
            this.CoTime.Name = "CoTime";
            this.CoTime.ReadOnly = true;
            this.CoTime.Width = 140;
            // 
            // CoData
            // 
            this.CoData.HeaderText = "Data";
            this.CoData.MinimumWidth = 8;
            this.CoData.Name = "CoData";
            this.CoData.ReadOnly = true;
            this.CoData.Width = 336;
            // 
            // tmrDisplay
            // 
            this.tmrDisplay.Interval = 2000;
            this.tmrDisplay.Tick += new System.EventHandler(this.tmrDisplay_Tick);
            // 
            // tmrMsg
            // 
            this.tmrMsg.Enabled = true;
            this.tmrMsg.Tick += new System.EventHandler(this.tmrMsg_Tick);
            // 
            // Diag_LIN
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1419, 897);
            this.Controls.Add(this.tcDownloader);
            this.Controls.Add(this.groupBox1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Diag_LIN";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Mannhui technology UDS flush test tool";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Diag_PCAN_FormClosing);
            this.Load += new System.EventHandler(this.Diag_PCAN_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tcDownloader.ResumeLayout(false);
            this.tpDownloader.ResumeLayout(false);
            this.tpDownloader.PerformLayout();
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.Trace.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.Test.ResumeLayout(false);
            this.Test.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUpDownNAD)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudIdTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudIdFrom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgView)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chbCanFD;
        private System.Windows.Forms.ComboBox cbbHwType;
        private System.Windows.Forms.ComboBox cbbInterrupt;
        private System.Windows.Forms.Label laInterrupt;
        private System.Windows.Forms.ComboBox cbbIO;
        private System.Windows.Forms.Label laIOPort;
        private System.Windows.Forms.Label laHwType;
        private System.Windows.Forms.ComboBox cbbBaudrates;
        private System.Windows.Forms.Label laBaudrate;
        private System.Windows.Forms.TextBox txtBitrate;
        private System.Windows.Forms.Label laBitrate;
        private System.Windows.Forms.Button btnHwRefresh;
        private System.Windows.Forms.ComboBox cbbChannel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnInit;
        private System.Windows.Forms.Button btnRelease;
        private System.Windows.Forms.TabControl tcDownloader;
        private System.Windows.Forms.TabPage tpDownloader;
        private System.Windows.Forms.TabPage Trace;
        private System.Windows.Forms.Label lbPath;
        private System.Windows.Forms.ListBox lbxInfo;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox Test;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnTest;
        private System.Windows.Forms.NumericUpDown numUpDownNAD;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button btnClearDTC;
        private System.Windows.Forms.Button btnReadDTC;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.NumericUpDown nudIdTo;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown nudIdFrom;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chbShowPeriod;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button tbDownload;
        private System.Windows.Forms.Button btnInfoClear;
        public System.Windows.Forms.ProgressBar pBar;
        private System.Windows.Forms.Label lbFilePath;
        private System.Windows.Forms.Button btnExportTrace;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button btnReadHexFile;
        private System.Windows.Forms.ComboBox cbProject;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.DataGridView dgView;
        private System.Windows.Forms.Timer tmrDisplay;
        private System.Windows.Forms.Button btnWriteDID;
        private System.Windows.Forms.Button btnResetDID;
        private System.Windows.Forms.CheckBox cbEnAPPMsg;
        private System.Windows.Forms.Timer tmrMsg;
        private System.Windows.Forms.CheckBox cbWholeTrace;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoID;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoDir;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoLength;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoCount;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoTime;
        private System.Windows.Forms.DataGridViewTextBoxColumn CoData;
        private System.Windows.Forms.Button btnFlashAddr;
        private System.Windows.Forms.Button btnResetECU;
    }
}

