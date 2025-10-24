
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Diag_LIN));
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chbBRS = new System.Windows.Forms.CheckBox();
            this.chbFD = new System.Windows.Forms.CheckBox();
            this.chbRemote = new System.Windows.Forms.CheckBox();
            this.chbExtended = new System.Windows.Forms.CheckBox();
            this.cbbChannel = new System.Windows.Forms.ComboBox();
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
            this.lbPath = new System.Windows.Forms.Label();
            this.Trace = new System.Windows.Forms.TabPage();
            this.tmrDisplay = new System.Windows.Forms.Timer(this.components);
            this.tmrMsg = new System.Windows.Forms.Timer(this.components);
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.panel2.SuspendLayout();
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
            this.groupBox1.SuspendLayout();
            this.tcDownloader.SuspendLayout();
            this.tpDownloader.SuspendLayout();
            this.Trace.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer2
            // 
            resources.ApplyResources(this.splitContainer2, "splitContainer2");
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.panel2);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.lbxInfo);
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
            resources.ApplyResources(this.panel2, "panel2");
            this.panel2.Name = "panel2";
            // 
            // btnResetECU
            // 
            resources.ApplyResources(this.btnResetECU, "btnResetECU");
            this.btnResetECU.Name = "btnResetECU";
            this.toolTip.SetToolTip(this.btnResetECU, resources.GetString("btnResetECU.ToolTip"));
            this.btnResetECU.UseVisualStyleBackColor = true;
            this.btnResetECU.Click += new System.EventHandler(this.btnResetECU_Click);
            // 
            // btnResetDID
            // 
            resources.ApplyResources(this.btnResetDID, "btnResetDID");
            this.btnResetDID.Name = "btnResetDID";
            this.toolTip.SetToolTip(this.btnResetDID, resources.GetString("btnResetDID.ToolTip"));
            this.btnResetDID.UseVisualStyleBackColor = true;
            this.btnResetDID.Click += new System.EventHandler(this.btnResetDID_Click);
            // 
            // btnFlashAddr
            // 
            resources.ApplyResources(this.btnFlashAddr, "btnFlashAddr");
            this.btnFlashAddr.Name = "btnFlashAddr";
            this.toolTip.SetToolTip(this.btnFlashAddr, resources.GetString("btnFlashAddr.ToolTip"));
            this.btnFlashAddr.UseVisualStyleBackColor = true;
            this.btnFlashAddr.Click += new System.EventHandler(this.btnFlashAddr_Click);
            // 
            // btnWriteDID
            // 
            resources.ApplyResources(this.btnWriteDID, "btnWriteDID");
            this.btnWriteDID.Name = "btnWriteDID";
            this.toolTip.SetToolTip(this.btnWriteDID, resources.GetString("btnWriteDID.ToolTip"));
            this.btnWriteDID.UseVisualStyleBackColor = true;
            this.btnWriteDID.Click += new System.EventHandler(this.btnWriteDID_Click);
            // 
            // btnBrowse
            // 
            resources.ApplyResources(this.btnBrowse, "btnBrowse");
            this.btnBrowse.Name = "btnBrowse";
            this.toolTip.SetToolTip(this.btnBrowse, resources.GetString("btnBrowse.ToolTip"));
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnReadHexFile
            // 
            resources.ApplyResources(this.btnReadHexFile, "btnReadHexFile");
            this.btnReadHexFile.Name = "btnReadHexFile";
            this.toolTip.SetToolTip(this.btnReadHexFile, resources.GetString("btnReadHexFile.ToolTip"));
            this.btnReadHexFile.UseVisualStyleBackColor = true;
            this.btnReadHexFile.Click += new System.EventHandler(this.btnReadHexFile_Click);
            // 
            // tbDownload
            // 
            resources.ApplyResources(this.tbDownload, "tbDownload");
            this.tbDownload.Name = "tbDownload";
            this.toolTip.SetToolTip(this.tbDownload, resources.GetString("tbDownload.ToolTip"));
            this.tbDownload.UseVisualStyleBackColor = true;
            this.tbDownload.Click += new System.EventHandler(this.tbDownload_Click);
            // 
            // btnInfoClear
            // 
            resources.ApplyResources(this.btnInfoClear, "btnInfoClear");
            this.btnInfoClear.Name = "btnInfoClear";
            this.toolTip.SetToolTip(this.btnInfoClear, resources.GetString("btnInfoClear.ToolTip"));
            this.btnInfoClear.UseVisualStyleBackColor = true;
            this.btnInfoClear.Click += new System.EventHandler(this.btnInfoClear_Click);
            // 
            // pBar
            // 
            this.pBar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(128)))));
            resources.ApplyResources(this.pBar, "pBar");
            this.pBar.Name = "pBar";
            this.pBar.Step = 1;
            // 
            // lbFilePath
            // 
            resources.ApplyResources(this.lbFilePath, "lbFilePath");
            this.lbFilePath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(192)))), ((int)(((byte)(0)))));
            this.lbFilePath.Name = "lbFilePath";
            // 
            // lbxInfo
            // 
            this.lbxInfo.BackColor = System.Drawing.SystemColors.Desktop;
            resources.ApplyResources(this.lbxInfo, "lbxInfo");
            this.lbxInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(128)))));
            this.lbxInfo.FormattingEnabled = true;
            this.lbxInfo.Items.AddRange(new object[] {
            resources.GetString("lbxInfo.Items")});
            this.lbxInfo.Name = "lbxInfo";
            // 
            // splitContainer1
            // 
            resources.ApplyResources(this.splitContainer1, "splitContainer1");
            this.splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.panel1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.dgView);
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
            resources.ApplyResources(this.panel1, "panel1");
            this.panel1.Name = "panel1";
            // 
            // cbWholeTrace
            // 
            resources.ApplyResources(this.cbWholeTrace, "cbWholeTrace");
            this.cbWholeTrace.Name = "cbWholeTrace";
            this.toolTip.SetToolTip(this.cbWholeTrace, resources.GetString("cbWholeTrace.ToolTip"));
            this.cbWholeTrace.UseVisualStyleBackColor = true;
            this.cbWholeTrace.CheckedChanged += new System.EventHandler(this.cbWholeTrace_CheckedChanged);
            // 
            // cbEnAPPMsg
            // 
            resources.ApplyResources(this.cbEnAPPMsg, "cbEnAPPMsg");
            this.cbEnAPPMsg.Checked = true;
            this.cbEnAPPMsg.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnAPPMsg.Name = "cbEnAPPMsg";
            this.toolTip.SetToolTip(this.cbEnAPPMsg, resources.GetString("cbEnAPPMsg.ToolTip"));
            this.cbEnAPPMsg.UseVisualStyleBackColor = true;
            this.cbEnAPPMsg.CheckedChanged += new System.EventHandler(this.cbEnAPPMsg_CheckedChanged);
            // 
            // Test
            // 
            this.Test.Controls.Add(this.label5);
            this.Test.Controls.Add(this.btnTest);
            this.Test.Controls.Add(this.numUpDownNAD);
            resources.ApplyResources(this.Test, "Test");
            this.Test.Name = "Test";
            this.Test.TabStop = false;
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.Name = "label5";
            // 
            // btnTest
            // 
            resources.ApplyResources(this.btnTest, "btnTest");
            this.btnTest.Name = "btnTest";
            this.toolTip.SetToolTip(this.btnTest, resources.GetString("btnTest.ToolTip"));
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            // 
            // numUpDownNAD
            // 
            this.numUpDownNAD.Hexadecimal = true;
            resources.ApplyResources(this.numUpDownNAD, "numUpDownNAD");
            this.numUpDownNAD.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numUpDownNAD.Name = "numUpDownNAD";
            this.toolTip.SetToolTip(this.numUpDownNAD, resources.GetString("numUpDownNAD.ToolTip"));
            this.numUpDownNAD.Value = new decimal(new int[] {
            66,
            0,
            0,
            0});
            // 
            // btnExportTrace
            // 
            resources.ApplyResources(this.btnExportTrace, "btnExportTrace");
            this.btnExportTrace.Name = "btnExportTrace";
            this.toolTip.SetToolTip(this.btnExportTrace, resources.GetString("btnExportTrace.ToolTip"));
            this.btnExportTrace.UseVisualStyleBackColor = true;
            this.btnExportTrace.Click += new System.EventHandler(this.btnExportTrace_Click);
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.btnClearDTC);
            this.groupBox4.Controls.Add(this.btnReadDTC);
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // btnClearDTC
            // 
            resources.ApplyResources(this.btnClearDTC, "btnClearDTC");
            this.btnClearDTC.Name = "btnClearDTC";
            this.toolTip.SetToolTip(this.btnClearDTC, resources.GetString("btnClearDTC.ToolTip"));
            this.btnClearDTC.UseVisualStyleBackColor = true;
            this.btnClearDTC.Click += new System.EventHandler(this.btnClearDTC_Click);
            // 
            // btnReadDTC
            // 
            resources.ApplyResources(this.btnReadDTC, "btnReadDTC");
            this.btnReadDTC.Name = "btnReadDTC";
            this.toolTip.SetToolTip(this.btnReadDTC, resources.GetString("btnReadDTC.ToolTip"));
            this.btnReadDTC.UseVisualStyleBackColor = true;
            this.btnReadDTC.Click += new System.EventHandler(this.btnReadDTC_Click);
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.nudIdTo);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.nudIdFrom);
            this.groupBox3.Controls.Add(this.label3);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // nudIdTo
            // 
            this.nudIdTo.Hexadecimal = true;
            resources.ApplyResources(this.nudIdTo, "nudIdTo");
            this.nudIdTo.Maximum = new decimal(new int[] {
            536870911,
            0,
            0,
            0});
            this.nudIdTo.Name = "nudIdTo";
            this.toolTip.SetToolTip(this.nudIdTo, resources.GetString("nudIdTo.ToolTip"));
            this.nudIdTo.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.label2.Name = "label2";
            // 
            // nudIdFrom
            // 
            this.nudIdFrom.Hexadecimal = true;
            resources.ApplyResources(this.nudIdFrom, "nudIdFrom");
            this.nudIdFrom.Maximum = new decimal(new int[] {
            536870911,
            0,
            0,
            0});
            this.nudIdFrom.Name = "nudIdFrom";
            this.toolTip.SetToolTip(this.nudIdFrom, resources.GetString("nudIdFrom.ToolTip"));
            this.nudIdFrom.Value = new decimal(new int[] {
            61,
            0,
            0,
            0});
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.label3.Name = "label3";
            // 
            // chbShowPeriod
            // 
            resources.ApplyResources(this.chbShowPeriod, "chbShowPeriod");
            this.chbShowPeriod.Name = "chbShowPeriod";
            this.toolTip.SetToolTip(this.chbShowPeriod, resources.GetString("chbShowPeriod.ToolTip"));
            this.chbShowPeriod.UseVisualStyleBackColor = true;
            this.chbShowPeriod.CheckedChanged += new System.EventHandler(this.chbShowPeriod_CheckedChanged);
            // 
            // btnClear
            // 
            resources.ApplyResources(this.btnClear, "btnClear");
            this.btnClear.Name = "btnClear";
            this.toolTip.SetToolTip(this.btnClear, resources.GetString("btnClear.ToolTip"));
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // dgView
            // 
            this.dgView.AllowUserToAddRows = false;
            this.dgView.AllowUserToDeleteRows = false;
            this.dgView.AllowUserToResizeRows = false;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.WhiteSmoke;
            dataGridViewCellStyle7.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.MediumSeaGreen;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.Color.RoyalBlue;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.dgView.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle7;
            this.dgView.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.CoID,
            this.CoDir,
            this.CoLength,
            this.CoCount,
            this.CoTime,
            this.CoData});
            resources.ApplyResources(this.dgView, "dgView");
            this.dgView.Name = "dgView";
            this.dgView.ReadOnly = true;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("Verdana", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.DodgerBlue;
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.Color.RoyalBlue;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.dgView.RowsDefaultCellStyle = dataGridViewCellStyle8;
            this.dgView.RowTemplate.Height = 30;
            this.dgView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgView.VirtualMode = true;
            this.dgView.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dgView_CellValueNeeded);
            this.dgView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.dgView_KeyDown);
            // 
            // CoID
            // 
            resources.ApplyResources(this.CoID, "CoID");
            this.CoID.Name = "CoID";
            this.CoID.ReadOnly = true;
            // 
            // CoDir
            // 
            resources.ApplyResources(this.CoDir, "CoDir");
            this.CoDir.Name = "CoDir";
            this.CoDir.ReadOnly = true;
            // 
            // CoLength
            // 
            resources.ApplyResources(this.CoLength, "CoLength");
            this.CoLength.Name = "CoLength";
            this.CoLength.ReadOnly = true;
            // 
            // CoCount
            // 
            resources.ApplyResources(this.CoCount, "CoCount");
            this.CoCount.Name = "CoCount";
            this.CoCount.ReadOnly = true;
            // 
            // CoTime
            // 
            resources.ApplyResources(this.CoTime, "CoTime");
            this.CoTime.Name = "CoTime";
            this.CoTime.ReadOnly = true;
            // 
            // CoData
            // 
            resources.ApplyResources(this.CoData, "CoData");
            this.CoData.Name = "CoData";
            this.CoData.ReadOnly = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chbBRS);
            this.groupBox1.Controls.Add(this.chbFD);
            this.groupBox1.Controls.Add(this.chbRemote);
            this.groupBox1.Controls.Add(this.chbExtended);
            this.groupBox1.Controls.Add(this.cbbChannel);
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
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // chbBRS
            // 
            this.chbBRS.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.chbBRS, "chbBRS");
            this.chbBRS.Name = "chbBRS";
            this.toolTip.SetToolTip(this.chbBRS, resources.GetString("chbBRS.ToolTip"));
            // 
            // chbFD
            // 
            this.chbFD.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.chbFD, "chbFD");
            this.chbFD.Name = "chbFD";
            this.toolTip.SetToolTip(this.chbFD, resources.GetString("chbFD.ToolTip"));
            this.chbFD.CheckedChanged += new System.EventHandler(this.chbFD_CheckedChanged);
            // 
            // chbRemote
            // 
            this.chbRemote.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.chbRemote, "chbRemote");
            this.chbRemote.Name = "chbRemote";
            this.toolTip.SetToolTip(this.chbRemote, resources.GetString("chbRemote.ToolTip"));
            // 
            // chbExtended
            // 
            this.chbExtended.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.chbExtended, "chbExtended");
            this.chbExtended.Name = "chbExtended";
            this.toolTip.SetToolTip(this.chbExtended, resources.GetString("chbExtended.ToolTip"));
            // 
            // cbbChannel
            // 
            this.cbbChannel.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.cbbChannel, "cbbChannel");
            this.cbbChannel.Items.AddRange(new object[] {
            resources.GetString("cbbChannel.Items"),
            resources.GetString("cbbChannel.Items1"),
            resources.GetString("cbbChannel.Items2"),
            resources.GetString("cbbChannel.Items3"),
            resources.GetString("cbbChannel.Items4"),
            resources.GetString("cbbChannel.Items5"),
            resources.GetString("cbbChannel.Items6"),
            resources.GetString("cbbChannel.Items7"),
            resources.GetString("cbbChannel.Items8"),
            resources.GetString("cbbChannel.Items9"),
            resources.GetString("cbbChannel.Items10"),
            resources.GetString("cbbChannel.Items11"),
            resources.GetString("cbbChannel.Items12"),
            resources.GetString("cbbChannel.Items13"),
            resources.GetString("cbbChannel.Items14"),
            resources.GetString("cbbChannel.Items15"),
            resources.GetString("cbbChannel.Items16"),
            resources.GetString("cbbChannel.Items17"),
            resources.GetString("cbbChannel.Items18"),
            resources.GetString("cbbChannel.Items19"),
            resources.GetString("cbbChannel.Items20"),
            resources.GetString("cbbChannel.Items21"),
            resources.GetString("cbbChannel.Items22"),
            resources.GetString("cbbChannel.Items23"),
            resources.GetString("cbbChannel.Items24"),
            resources.GetString("cbbChannel.Items25"),
            resources.GetString("cbbChannel.Items26"),
            resources.GetString("cbbChannel.Items27")});
            this.cbbChannel.Name = "cbbChannel";
            this.toolTip.SetToolTip(this.cbbChannel, resources.GetString("cbbChannel.ToolTip"));
            this.cbbChannel.SelectedIndexChanged += new System.EventHandler(this.cbbChannel_SelectedIndexChanged);
            // 
            // btnRelease
            // 
            this.btnRelease.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.btnRelease, "btnRelease");
            this.btnRelease.Name = "btnRelease";
            this.btnRelease.Click += new System.EventHandler(this.btnRelease_Click);
            // 
            // chbCanFD
            // 
            resources.ApplyResources(this.chbCanFD, "chbCanFD");
            this.chbCanFD.Name = "chbCanFD";
            this.toolTip.SetToolTip(this.chbCanFD, resources.GetString("chbCanFD.ToolTip"));
            this.chbCanFD.UseVisualStyleBackColor = true;
            this.chbCanFD.CheckedChanged += new System.EventHandler(this.chbCanFD_CheckedChanged);
            // 
            // cbProject
            // 
            this.cbProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProject.Items.AddRange(new object[] {
            resources.GetString("cbProject.Items"),
            resources.GetString("cbProject.Items1"),
            resources.GetString("cbProject.Items2"),
            resources.GetString("cbProject.Items3"),
            resources.GetString("cbProject.Items4"),
            resources.GetString("cbProject.Items5"),
            resources.GetString("cbProject.Items6"),
            resources.GetString("cbProject.Items7"),
            resources.GetString("cbProject.Items8"),
            resources.GetString("cbProject.Items9"),
            resources.GetString("cbProject.Items10"),
            resources.GetString("cbProject.Items11"),
            resources.GetString("cbProject.Items12")});
            resources.ApplyResources(this.cbProject, "cbProject");
            this.cbProject.Name = "cbProject";
            this.toolTip.SetToolTip(this.cbProject, resources.GetString("cbProject.ToolTip"));
            this.cbProject.SelectedIndexChanged += new System.EventHandler(this.cbProject_SelectedIndexChanged);
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.Name = "label6";
            // 
            // cbbBaudrates
            // 
            this.cbbBaudrates.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbBaudrates.Items.AddRange(new object[] {
            resources.GetString("cbbBaudrates.Items"),
            resources.GetString("cbbBaudrates.Items1"),
            resources.GetString("cbbBaudrates.Items2"),
            resources.GetString("cbbBaudrates.Items3"),
            resources.GetString("cbbBaudrates.Items4"),
            resources.GetString("cbbBaudrates.Items5")});
            resources.ApplyResources(this.cbbBaudrates, "cbbBaudrates");
            this.cbbBaudrates.Name = "cbbBaudrates";
            this.toolTip.SetToolTip(this.cbbBaudrates, resources.GetString("cbbBaudrates.ToolTip"));
            // 
            // laBaudrate
            // 
            resources.ApplyResources(this.laBaudrate, "laBaudrate");
            this.laBaudrate.Name = "laBaudrate";
            // 
            // laBitrate
            // 
            resources.ApplyResources(this.laBitrate, "laBitrate");
            this.laBitrate.Name = "laBitrate";
            // 
            // btnHwRefresh
            // 
            this.btnHwRefresh.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.btnHwRefresh, "btnHwRefresh");
            this.btnHwRefresh.Name = "btnHwRefresh";
            this.btnHwRefresh.Click += new System.EventHandler(this.btnHwRefresh_Click);
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // btnInit
            // 
            this.btnInit.Cursor = System.Windows.Forms.Cursors.Default;
            resources.ApplyResources(this.btnInit, "btnInit");
            this.btnInit.Name = "btnInit";
            this.btnInit.Click += new System.EventHandler(this.btnInit_Click);
            // 
            // cbbHwType
            // 
            this.cbbHwType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbHwType.Items.AddRange(new object[] {
            resources.GetString("cbbHwType.Items"),
            resources.GetString("cbbHwType.Items1"),
            resources.GetString("cbbHwType.Items2"),
            resources.GetString("cbbHwType.Items3"),
            resources.GetString("cbbHwType.Items4"),
            resources.GetString("cbbHwType.Items5"),
            resources.GetString("cbbHwType.Items6")});
            resources.ApplyResources(this.cbbHwType, "cbbHwType");
            this.cbbHwType.Name = "cbbHwType";
            this.toolTip.SetToolTip(this.cbbHwType, resources.GetString("cbbHwType.ToolTip"));
            this.cbbHwType.SelectedIndexChanged += new System.EventHandler(this.cbbHwType_SelectedIndexChanged);
            // 
            // laHwType
            // 
            resources.ApplyResources(this.laHwType, "laHwType");
            this.laHwType.Name = "laHwType";
            // 
            // cbbInterrupt
            // 
            this.cbbInterrupt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbInterrupt.Items.AddRange(new object[] {
            resources.GetString("cbbInterrupt.Items"),
            resources.GetString("cbbInterrupt.Items1"),
            resources.GetString("cbbInterrupt.Items2"),
            resources.GetString("cbbInterrupt.Items3"),
            resources.GetString("cbbInterrupt.Items4"),
            resources.GetString("cbbInterrupt.Items5"),
            resources.GetString("cbbInterrupt.Items6"),
            resources.GetString("cbbInterrupt.Items7"),
            resources.GetString("cbbInterrupt.Items8")});
            resources.ApplyResources(this.cbbInterrupt, "cbbInterrupt");
            this.cbbInterrupt.Name = "cbbInterrupt";
            this.toolTip.SetToolTip(this.cbbInterrupt, resources.GetString("cbbInterrupt.ToolTip"));
            // 
            // laInterrupt
            // 
            resources.ApplyResources(this.laInterrupt, "laInterrupt");
            this.laInterrupt.Name = "laInterrupt";
            // 
            // cbbIO
            // 
            this.cbbIO.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbbIO.Items.AddRange(new object[] {
            resources.GetString("cbbIO.Items"),
            resources.GetString("cbbIO.Items1"),
            resources.GetString("cbbIO.Items2"),
            resources.GetString("cbbIO.Items3"),
            resources.GetString("cbbIO.Items4"),
            resources.GetString("cbbIO.Items5"),
            resources.GetString("cbbIO.Items6"),
            resources.GetString("cbbIO.Items7"),
            resources.GetString("cbbIO.Items8"),
            resources.GetString("cbbIO.Items9"),
            resources.GetString("cbbIO.Items10"),
            resources.GetString("cbbIO.Items11"),
            resources.GetString("cbbIO.Items12"),
            resources.GetString("cbbIO.Items13"),
            resources.GetString("cbbIO.Items14"),
            resources.GetString("cbbIO.Items15"),
            resources.GetString("cbbIO.Items16"),
            resources.GetString("cbbIO.Items17"),
            resources.GetString("cbbIO.Items18"),
            resources.GetString("cbbIO.Items19"),
            resources.GetString("cbbIO.Items20"),
            resources.GetString("cbbIO.Items21"),
            resources.GetString("cbbIO.Items22"),
            resources.GetString("cbbIO.Items23")});
            resources.ApplyResources(this.cbbIO, "cbbIO");
            this.cbbIO.Name = "cbbIO";
            this.toolTip.SetToolTip(this.cbbIO, resources.GetString("cbbIO.ToolTip"));
            // 
            // laIOPort
            // 
            resources.ApplyResources(this.laIOPort, "laIOPort");
            this.laIOPort.Name = "laIOPort";
            // 
            // txtBitrate
            // 
            resources.ApplyResources(this.txtBitrate, "txtBitrate");
            this.txtBitrate.Name = "txtBitrate";
            this.toolTip.SetToolTip(this.txtBitrate, resources.GetString("txtBitrate.ToolTip"));
            // 
            // tcDownloader
            // 
            this.tcDownloader.Controls.Add(this.tpDownloader);
            this.tcDownloader.Controls.Add(this.Trace);
            resources.ApplyResources(this.tcDownloader, "tcDownloader");
            this.tcDownloader.Name = "tcDownloader";
            this.tcDownloader.SelectedIndex = 0;
            // 
            // tpDownloader
            // 
            this.tpDownloader.Controls.Add(this.splitContainer2);
            this.tpDownloader.Controls.Add(this.lbPath);
            resources.ApplyResources(this.tpDownloader, "tpDownloader");
            this.tpDownloader.Name = "tpDownloader";
            this.tpDownloader.UseVisualStyleBackColor = true;
            // 
            // lbPath
            // 
            resources.ApplyResources(this.lbPath, "lbPath");
            this.lbPath.Name = "lbPath";
            // 
            // Trace
            // 
            this.Trace.Controls.Add(this.splitContainer1);
            resources.ApplyResources(this.Trace, "Trace");
            this.Trace.Name = "Trace";
            this.Trace.UseVisualStyleBackColor = true;
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
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tcDownloader);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            this.KeyPreview = true;
            this.Name = "Diag_LIN";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Diag_PCAN_FormClosing);
            this.Load += new System.EventHandler(this.Diag_PCAN_Load);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Diag_LIN_KeyDown);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
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
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tcDownloader.ResumeLayout(false);
            this.tpDownloader.ResumeLayout(false);
            this.tpDownloader.PerformLayout();
            this.Trace.ResumeLayout(false);
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
        private System.Windows.Forms.CheckBox chbBRS;
        private System.Windows.Forms.CheckBox chbFD;
        private System.Windows.Forms.CheckBox chbRemote;
        private System.Windows.Forms.CheckBox chbExtended;
    }
}

