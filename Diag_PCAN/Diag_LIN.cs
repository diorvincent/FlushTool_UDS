/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Nov 1, 2023
 * Modify on: Feb 17, 2024
 * Author: He Jingchi
 * Modifier: He Jingchi
 */
using Peak.Can.Basic;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;
using USB2XXX;
using TPCANHandle = System.UInt16;

namespace Diag_BUS
{
    public partial class Diag_LIN : Form
    {
        #region global bus object
        Bus m_bus;
        CAN_Bus m_canBus;
        LIN_Bus m_linBus;
        #endregion

        #region Delegates
        /// <summary>
        /// Read-Delegate Handler
        /// </summary>
        private delegate void ReadDelegateHandler(/*byte[] respMsg*/);
        private delegate bool FlashFirewareHandler(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize);
        
        private delegate bool DoChkUDSSvrDelegate(DataTable data, string strUDSFilePath, int nRow, int nColumn);
        private delegate int FlashFirewareHandlerForTP90(byte[] buff, int nCurrIndex, int n0x36PackCnt);

        public delegate bool N2S_FlashFirewareHandler(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack);
        #endregion

        #region Member Variables

        public struct _Bin_Addr_Len
        {
            public uint StartAddress;
            public uint BlockLen;
            public uint MaxBlockSize;
            public FileStream FS; //for 7Kw project use
            public byte[] TotalData; //for CheryCBF project use
        }
        public List<_Bin_Addr_Len> m_lstBinInfo;

        /// <summary>
        /// project type
        /// </summary>
        internal enum PRJTYPE : byte
        {
            _320vCompresor = 0,
            _400vCompresor = 1,
            _xc2234 = 2,
            _7Kw = 3,
            _N2S = 4,
            _CANUDS40 = 5,
            _CANUDS01 = 6,
            _LINHex = 7,
            _SplitFlash_CAN = 8,
            _Chery_CBF = 9,
        }
        PRJTYPE PRODUCT_TYPE;
        int P2_ServerTime = 30;
        int FIXED_DATA_SIZE = 0x1000;
        int PACK_SIZE = 0x80;
        UInt32 RT_MaxNumber = 65535;//this count means trace rows can not beyong this,otherwise save to excel file will failure.
        UInt32 MASK = 0x7D3EFD82;
        UInt32 BIN_START_POS = 0x14C;
        UInt32 MEMORY_ADDR2 = 0x00C00000;
        UInt32 MEMORY_SIZE2 = 0x1000;

        public UInt32 N2S_MASK = 0xEDB88320;
        //for transfer data
        public UInt32 MEMORY_ADDR = 0x00000000;
        public UInt32 MEMORY_SIZE = 0x1000;
        //for earse memory
        public UInt32 CAN_ADDR = 0x08009000;
        public UInt32 CAN_SIZE = 0x00017000;

        //for transfer data
        public UInt32 MEMORY_ADDR1 = 0x00000000;
        public UInt32 MEMORY_SIZE1 = 0x1000;
        //for earse memory
        public UInt32 CAN_ADDR1 = 0x08009000;
        public UInt32 CAN_SIZE1 = 0x00017000;

        //st_min intever response time throhold
        public int ST_MIN = 0;

        public int m_nWriteDID_Times = 0;
        /// <summary>
        /// Break thread & loop excute among the downloading when press 'release' button on UI
        /// </summary>
        public bool m_bBreakInDownloading;
        /// <summary>
        /// enable/disable 0x3E service
        /// </summary>
        public bool m_bEnable_0x3E;
        /// <summary>
        /// show trace in UI?
        /// </summary>
        public bool m_bEnable_Trace;
        public volatile byte[] m_Total36Data = new byte[] { };
        //hex file block info(block start address, block length, block end address..)
        public List<HexParser.RecordAddrInfo> m_RecInfo;
        public List<HexParser.RecordAddrInfo> m_RecData;
        ///<summary>
        ///marked transfer data finished and no error occured during flash
        ///</summary>
        public bool m_bTransferDataOK;
        public bool m_b36SvrOneBlockOver;
        public int gAddrOffset = 0;
        public int m_nSourceIndex = 0; //copy data index from hex data buffer in 0x36 svr
        /// <summary>
        /// global synchronization object
        /// </summary>
        public static object m_obj;
        /// <summary>
        /// Thread for message writting
        /// </summary>
        public Thread m_WriteThread;

        public int m_nTP90_ReadAddr_Times;
        public uint m_FirmwareFileSize;
        public int m_n0x36PackNum;

        ExcelHelper m_ExcleObj;
        public CBFParser m_CBFParser;

        #region split paragraph flash variables(can single select app/cal flash)
        /// <summary>
        /// chip page size 
        /// </summary>
        readonly uint CHIP_PAGESIZE = 0x800;
        /// <summary>
        /// end of app data address(enable flash app data)
        /// </summary>
        public bool m_bAppAddr_Enable;
        /// <summary>
        /// start of calibration data address(enable flash cal data)
        /// </summary>
        public bool m_bCalAddr_Enable;
        /// <summary>
        /// .hex app data end address
        /// </summary>
        uint m_uAppEndAddr;
        /// <summary>
        /// .hex calibration start address
        /// </summary>
        uint m_uCalStartAddr;
        /// <summary>
        /// App data end block number
        /// </summary>
        public int m_nAppBlockNum = 0; //
        /// <summary>
        /// calibration data start block number
        /// </summary>
        public int m_nCalBlockNum = 0; //
        #endregion

        /// <summary>
        /// display app message or not?
        /// </summary>
        public bool m_DisplayAppMsg;

        /// <summary>
        /// need parsed hex file path
        /// </summary>
        string m_strHexFile;

        /// <summary>
        /// determine collect whole trace or not, cause message number beyong 65535 can not be save as excel file(NPOI libraray's limite)
        /// </summary>
        bool m_WholeTrace;

        #region TOMOSS MEMBER

        Int32[] m_DevHandles = new Int32[10];
        Int32 m_DevHandle;
        Byte m_LINIndex;
        bool m_state;
        Int32 m_LINDevNum;
        String[] m_MSGTypeStr;
        String[] m_CKTypeStr;

        //record lin message time
        long FristEnterRT_ticks;
        string T_timestamp;
        bool m_bRelativeTime;

        //CAN message class
        CANMsgs m_can_msg;
        //LIN message class
        LINMsg m_lin_msg;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        byte[] m_LINMsg;

        Thread UDS_Test_TH;
        /// <summary>
        /// LIN message list
        /// </summary>
        private static List<LINMsg> m_lstLINMsg;

        #endregion

        #region PCAN MEMBER

        /// <summary>
        /// this delegate will be use on N2S and CANUDS flashing 
        /// </summary>
        public N2S_FlashFirewareHandler m_CANUDS_ffHandler;

        /// <summary>
        /// process 0x36 data
        /// </summary>
        struct N2S_DataPack
        {
            public int nMaxNumOfBlock;
            public byte[] All0x36PackData;
        };
        N2S_DataPack m_N2SDataPack;

        public bool m_bReadWriteDID;
        /// <summary>
        /// process 0x36 service counter
        /// </summary>
        bool m_b1stFrm;
        int m_n36SvrPackNum; //0x36 service send data counter
        
        int gCurrPackPos; //0x36 service frame index;
        /// <summary>
        /// mutiple frame on CAN
        /// </summary>
        byte m_FC0, m_FC1;   //follow control byte0,byte1
        byte m_FollowControl;//follow control mark byte
        byte m_BlockSize;    //follow control block size
        byte m_WaitTime;     //responsed waitting time after a group package sent
        /// <summary>
        /// Create an AutoResetEvent to signal the timeout threshold in the timer callback has been reached.
        /// </summary>
        //AutoResetEvent svr3E_AutoEvent;
        /// <summary>
        /// waitting for 0x31 response
        /// </summary>
        const int RESP_0x31_WAITTING_TIME = 6500;
        /// <summary>
        /// 0x3E service request message interval
        /// </summary>
        const int REQ_3E_INTERVAL = 100;
        /// <summary>
        /// UDS 0x34 dynamic start address
        /// </summary>
        //private uint m_nDynStartAddr;
        /// <summary>
        /// Saves the desired connection mode
        /// </summary>
        private bool m_IsFD;
        /// <summary>
        /// Saves the handle of a PCAN hardware
        /// </summary>
        //private TPCANHandle m_PcanHandle;
        ///// <summary>
        ///// Saves the baudrate register for a conenction
        ///// </summary>
        //private TPCANBaudrate m_Baudrate;
        ///// <summary>
        ///// Saves the type of a non-plug-and-play hardware
        ///// </summary>
        //private TPCANType m_HwType;
        /// <summary>
        /// Stores the status of received messages for its display
        /// </summary>
        private System.Collections.ArrayList m_LastMsgsList;
        /// <summary>
        /// Read Delegate for calling the function "ReadMessages"
        /// </summary>
        private ReadDelegateHandler m_ReadDelegate;
        /// <summary>
        /// Send-Event
        /// </summary>
        //private System.Threading.AutoResetEvent m_SendEvent;
        /// <summary>
        /// Receive-Event
        /// </summary>
        private System.Threading.AutoResetEvent m_ReceiveEvent;
        /// <summary>
        /// ReadDTC-Event
        /// </summary>
        public System.Threading.ManualResetEvent m_ReadDTCEvent;
        /// <summary>
        /// Thread for read dtc message sending (using evens)
        /// </summary>
        private Thread m_ReadDTCThread;
        ///<summary>
        ///sychronous read and writ message thread
        /// </summary>
        /// 
        private System.Threading.ReaderWriterLockSlim _rw;
        /// <summary>
        /// Thread for message reading (using events)
        /// </summary>
        private System.Threading.Thread m_ReadThread;
        /// <summary>
        /// Handles of non plug and play PCAN-Hardware
        /// </summary>
        //private TPCANHandle[] m_NonPnPHandles;
        /// <summary>
        /// Hex file name
        /// </summary>
        //private string m_strBootloadFileName = "";
        /// <summary>
        /// Hex file name
        /// </summary>
        public string m_strHexFileName = "";
        /// <summary>
        /// used to judgement input file type
        /// </summary>
        private string m_strHexBinExtension = "";
        /// <summary>
        /// Diagnostic request data
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private byte[] m_ReqMsg;
        /// <summary>
        /// Diagnostic response data
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] m_RespMsg;
        /// <summary>
        /// Use to store response message
        /// </summary>
        //TPCANMsg m_CANMsg;
        /// <summary>
        /// Download started? then active 0x3E service
        /// </summary>
        //bool m_bStartDownload = false;
        /// <summary>
        /// CAN message list
        /// </summary>
        private static List<CANMsgs> m_lstCANMsg;
        /// <summary>
        /// record message count
        /// </summary>
        private uint m_MsgCount;
        #endregion

        #endregion

        public Diag_LIN()
        {
            InitializeComponent();
            // Initializes specific components
            //
            InitializeBasicComponents();
        }


        /// <summary>
        /// Gets the formated text for a PCAN-Basic channel handle
        /// </summary>
        /// <param name="handle">PCAN-Basic Handle to format</param>
        /// <param name="isFD">If the channel is FD capable</param>
        /// <returns>The formatted text for a channel</returns>
        private string FormatChannelName(TPCANHandle handle, bool isFD)
        {
            TPCANDevice devDevice;
            byte byChannel;

            // Gets the owner device and channel for a 
            // PCAN-Basic handle
            //
            if (handle < 0x100)
            {
                devDevice = (TPCANDevice)(handle >> 4);
                byChannel = (byte)(handle & 0xF);
            }
            else
            {
                devDevice = (TPCANDevice)(handle >> 8);
                byChannel = (byte)(handle & 0xFF);
            }

            // Constructs the PCAN-Basic Channel name and return it
            //
            if (isFD)
                return string.Format("{0}:FD {1} ({2:X2}h)", devDevice, byChannel, handle);
            else
                return string.Format("{0} {1} ({2:X2}h)", devDevice, byChannel, handle);
        }

        private void btnHwRefresh_Click(object sender, EventArgs e)
        {
            string strHardwareName = string.Empty;
            cbbChannel.Items.Clear();

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                strHardwareName = FormatChannelName(m_canBus.PCANHANDLE, false);
                cbbChannel.Items.Add(strHardwareName);
                cbbChannel.SelectedIndex = 0;

                GetPCANVersion();
                SetConnectionStatus(true);
            }

            if(m_bus.BusType == Bus.Type.LIN_BUS)
            {
                // Clears the Channel comboBox and fill it again with 
                // the PCAN-Basic handles for no-Plug&Play hardware and
                // the detected Plug&Play hardware
                //               
                try
                {
                    #region Insert USBXXX(tomoss can device into list)
                    if(m_linBus.State)
                    {
                        bool bInfo = false;
                        StringBuilder sbr = null;

                        USB_DEVICE.DEVICE_INFO DevInfo;
                        DevInfo = m_linBus.DevInfo;
                        sbr = new StringBuilder(256);
                        
                        bInfo = USB_DEVICE.DEV_GetDeviceInfo(m_linBus.DeviceHandle, ref DevInfo, sbr);
                        if(bInfo)
                        {
                            String strFireware = Encoding.Default.GetString(DevInfo.FirmwareName);
                            cbbChannel.Items.Add(strFireware);
                            cbbChannel.SelectedIndex = 0;

                            IncludeTextMessage("Firmware Info:");
                            IncludeTextMessage("    Name:" + Encoding.Default.GetString(DevInfo.FirmwareName));
                            String strDeviceInfo;
                            strDeviceInfo = "    Build Date:" + Encoding.Default.GetString(DevInfo.BuildDate);
                            IncludeTextMessage(strDeviceInfo);
                            strDeviceInfo = String.Format("    Firmware Version:v{0}.{1}.{2}", (DevInfo.FirmwareVersion >> 24) & 0xFF, (DevInfo.FirmwareVersion >> 16) & 0xFF, DevInfo.FirmwareVersion & 0xFFFF);
                            IncludeTextMessage(strDeviceInfo);
                            strDeviceInfo = String.Format("    Hardware Version:v{0}.{1}.{2}", (DevInfo.HardwareVersion >> 24) & 0xFF, (DevInfo.HardwareVersion >> 16) & 0xFF, DevInfo.HardwareVersion & 0xFFFF);
                            IncludeTextMessage(strDeviceInfo);
                            IncludeTextMessage("    Functions:" + DevInfo.Functions.ToString("X8"));
                            IncludeTextMessage("    Functions String:" + sbr);
                        }

                        SetConnectionStatus(true);

                        SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                        m_nWriteDID_Times = 0; //reset DID write times
                    }
                    #endregion
                }
                catch (DllNotFoundException)
                {
                    MessageBox.Show("Unable to find the library: USB2XXX.dll !", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(-1);
                }
            }  
        }

        #region Help functions
        /// <summary>
        /// Convert a CAN DLC value into the actual data length of the CAN/CAN-FD frame.
        /// </summary>
        /// <param name="dlc">A value between 0 and 15 (CAN and FD DLC range)</param>
        /// <param name="isSTD">A value indicating if the msg is a standard CAN (FD Flag not checked)</param>
        /// <returns>The length represented by the DLC</returns>
        public static int GetLengthFromDLC(int dlc, bool isSTD)
        {
            if (dlc <= 8)
                return dlc;

            if (isSTD)
            {
                switch (dlc)
                {
                    case 9: return 12;
                    case 10: return 16;
                    case 11: return 20;
                    case 12: return 24;
                    case 13: return 32;
                    case 14: return 48;
                    case 15: return 64;
                    default: return dlc;
                }
            }
            return dlc;
        }

        /// <summary>
        /// Initialization of PCAN-Basic components
        /// </summary>
        private void InitializeBasicComponents()
        {
            // Creates the list for received messages
            //
            m_LastMsgsList = new System.Collections.ArrayList();
            // Creates the delegate used for message reading
            //
            m_ReadDelegate = new ReadDelegateHandler(ReadMessages);
            //Creates the synchronization object for thread(send/receive)
            //
            m_obj = new object();
            // Creates the event used for signalize incomming messages 
            //
            m_ReceiveEvent = new AutoResetEvent(false);
            //Create the event used for recieve read DTC response message
            //
            m_ReadDTCEvent = new ManualResetEvent(false);
            //Create the delegate used for N2S and CANUDS flashing
            //
            m_CANUDS_ffHandler = new N2S_FlashFirewareHandler(N2S_canFlashFirmware_TH);
            //ReadWriteLock Init
            //
            _rw = new System.Threading.ReaderWriterLockSlim();

            //Creates the diagnostic request/response message
            //
            m_ReqMsg = new byte[8];
            m_RespMsg = new byte[8];
            m_LINMsg = new byte[8];
            
            // Fills and configures the Data of several comboBox components
            //
            FillComboBoxData();

            // Init flashing parameter(app address, calibration address and each enabled or not?)
            //
            FlashAdressSet.GetFlashInfo();
            //for split flash(app & cal) on CAN
            m_nAppBlockNum = 0; //
            m_nCalBlockNum = 0; //

            //USBXXX
            m_DevHandle = 0;
            m_LINIndex = 0;
            m_LINDevNum = 0; 
            
            m_MSGTypeStr = new String[10] { "UN", "MW", "MR", "SW", "SR", "BK", "SY", "ID", "DT", "CK" };
            m_CKTypeStr = new String[5] { "STD", "EXT", "USER", "NONE", "ERROR" };

            m_lin_msg = new LINMsg();
            m_lin_msg.data = new byte[8];
            m_lin_msg.WaitTime = 100;

            m_can_msg = new CANMsgs();
            m_can_msg.CANMsg.DATA = new byte[8];

            FristEnterRT_ticks = 0;
            cbbChannel.SelectedIndex = 0;
            cbProject.SelectedIndex = 9;

            //m_nDynStartAddr = 0;
            m_n36SvrPackNum = 0x21;
            gCurrPackPos = 0;
            m_MsgCount = 0;
            m_nTP90_ReadAddr_Times = 0;
            m_n0x36PackNum = 1;
            m_FirmwareFileSize = 0;

            m_bTransferDataOK = true;
            m_bEnable_0x3E = false;
            m_bEnable_Trace = false;
            m_bReadWriteDID = false;
            m_DisplayAppMsg = true;
            //cbEnAPPMsg.Checked = false;
            cbWholeTrace.Checked = true;
            m_WholeTrace = true;

            m_bBreakInDownloading = false;
            ST_MIN = 50;
        }

        /// <summary>
        /// Configures the Debug-Log file of PCAN-Basic
        /// </summary>
        private void ConfigureLogFile()
        {
            UInt32 iBuffer;

            // Sets the mask to catch all events
            //
            iBuffer = PCANBasic.LOG_FUNCTION_ALL;

            // Configures the log file. 
            // NOTE: The Log capability is to be used with the NONEBUS Handle. Other handle than this will 
            // cause the function fail.
            //
            PCANBasic.SetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_LOG_CONFIGURE, ref iBuffer, sizeof(UInt32));
        }

        /// <summary>
        /// Configures the PCAN-Trace file for a PCAN-Basic Channel
        /// </summary>
        private void ConfigureTraceFile()
        {
            UInt32 iBuffer;
            TPCANStatus stsResult;

            // Configure the maximum size of a trace file to 5 megabytes
            //
            iBuffer = 5;
            stsResult = PCANBasic.SetValue(m_canBus.PCANHANDLE, TPCANParameter.PCAN_TRACE_SIZE, ref iBuffer, sizeof(UInt32));
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage(GetFormatedError(stsResult));

            // Configure the way how trace files are created: 
            // * Standard name is used
            // * Existing file is ovewritten, 
            // * Only one file is created.
            // * Recording stopts when the file size reaches 5 megabytes.
            //
            iBuffer = PCANBasic.TRACE_FILE_SINGLE | PCANBasic.TRACE_FILE_OVERWRITE;
            stsResult = PCANBasic.SetValue(m_canBus.PCANHANDLE, TPCANParameter.PCAN_TRACE_CONFIGURE, ref iBuffer, sizeof(UInt32));
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                IncludeTextMessage(GetFormatedError(stsResult));
        }

        /// <summary>
        /// Includes a new line of text into the information Listview
        /// </summary>
        /// <param name="strMsg">Text to be included</param>
        public void IncludeTextMessage(string strMsg)
        {
            lbxInfo.Items.Add(strMsg);
            lbxInfo.SelectedIndex = lbxInfo.Items.Count - 1;
        }

        /// <summary>
        /// Configures the data of all ComboBox components of the main-form
        /// </summary>
        private void FillComboBoxData()
        {
            // Channels will be check
            //
           // btnHwRefresh_Click(this, new EventArgs());

            // FD Bitrate: 
            //      Arbitration: 1 Mbit/sec 
            //      Data: 2 Mbit/sec
            //
            txtBitrate.Text = "f_clock_mhz=20, nom_brp=5, nom_tseg1=2, nom_tseg2=1, nom_sjw=1, data_brp=2, data_tseg1=3, data_tseg2=1, data_sjw=1";

            // Baudrates 
            //
            cbbBaudrates.SelectedIndex = 1; // 500 K

            // Hardware Type for no plugAndplay hardware
            //
            cbbHwType.SelectedIndex = 0;

            // Interrupt for no plugAndplay hardware
            //
            cbbInterrupt.SelectedIndex = 0;

            // IO Port for no plugAndplay hardware
            //
            cbbIO.SelectedIndex = 0;

            // Parameters for GetValue and SetValue function calls
            //
            //cbbParameter.SelectedIndex = 0;
        }

        /// <summary>
        /// Activates/deaactivates the different controls of the main-form according
        /// with the current connection status
        /// </summary>
        /// <param name="bConnected">Current status. True if connected, false otherwise</param>
        private void SetConnectionStatus(bool bConnected)
        {
            // Buttons
            //
            btnInit.Enabled = !bConnected;
            btnHwRefresh.Enabled = !bConnected;
            btnRelease.Enabled = bConnected;

            btnReadDTC.Enabled = bConnected;
            btnClearDTC.Enabled = bConnected;
            //tbDownload.Enabled = bConnected;
            btnTest.Enabled = bConnected;
            btnBrowse.Enabled = bConnected;
            btnReadHexFile.Enabled = bConnected;
            btnInfoClear.Enabled = bConnected;
            //btnWriteDID.Enabled = bConnected;
            btnResetDID.Enabled = bConnected;
            //btnExportTrace.Enabled = bConnected;
            btnFlashAddr.Enabled = bConnected;
            btnResetECU.Enabled = bConnected;
            // ComboBoxs
            //
            cbbChannel.Enabled = !bConnected;
            cbbBaudrates.Enabled = !bConnected;
            cbbHwType.Enabled = !bConnected;
            cbbIO.Enabled = !bConnected;
            cbbInterrupt.Enabled = !bConnected;
            cbProject.Enabled = !bConnected;

            cbEnAPPMsg.Enabled = bConnected;
            // Check-Buttons
            //
            chbCanFD.Enabled = !bConnected;

            // Hardware configuration and read mode
            //
            if (!bConnected)
                cbbChannel_SelectedIndexChanged(this, new EventArgs());

            tmrDisplay.Enabled = true;
            tmrMsg.Enabled = true;
        }

        /// <summary>
        /// disable buttons on UI when system do downloading
        /// </summary>
        /// <param name="bDonwloading"></param>
        public void SetDonwloadingStatus(bool bDonwloading)
        {
            btnBrowse.Enabled = !bDonwloading;
            btnReadHexFile.Enabled = !bDonwloading;
            tbDownload.Enabled = !bDonwloading;
            btnWriteDID.Enabled = !bDonwloading;
            btnReadDTC.Enabled = !bDonwloading;
            btnClearDTC.Enabled = !bDonwloading;
            btnTest.Enabled = !bDonwloading;
            btnResetDID.Enabled = !bDonwloading;
            cbEnAPPMsg.Enabled = !bDonwloading;
            btnResetECU.Enabled= !bDonwloading;
            //btnExportTrace.Enabled = !bDonwloading;
        }

        #endregion

        private void tbDownload_Click(object sender, EventArgs e)
        {
            m_MsgCount = 0;
            //if file not exists,then notify error
            if (File.Exists(m_strHexFileName) && m_bus!=null)
            {
                Flashing falshingObj = new Flashing();
                if (m_bus.BusType == Bus.Type.LIN_BUS)
                {
                    if (PRODUCT_TYPE == PRJTYPE._xc2234 && m_strHexBinExtension == ".hex")
                    {
                        LINWriteThreadFunc(); 
                    }
                    else if (PRODUCT_TYPE == PRJTYPE._7Kw && m_strHexBinExtension == ".bin" ||
                               PRODUCT_TYPE == PRJTYPE._Chery_CBF && m_strHexBinExtension == ".cbf" ||
                               PRODUCT_TYPE == PRJTYPE._LINHex && m_strHexBinExtension == ".hex")//
                    {
                        //LINWriteThreadFunc_TP90();
                        falshingObj = new Flashing(this, (byte)PRODUCT_TYPE);
                    }
                    //else if (PRODUCT_TYPE == PRJTYPE._LINHex && m_strHexBinExtension == ".hex")
                    //{
                    //    falshingObj = new Flashing(this, (byte)PRODUCT_TYPE);
                    //}
                    else
                        IncludeTextMessage("Bus adapter connect not correct,please connect P-CAN for flashing app file.");
                }
                if (m_bus.BusType == Bus.Type.CAN_BUS)
                {
                    if (PRODUCT_TYPE == PRJTYPE._320vCompresor || PRODUCT_TYPE == PRJTYPE._400vCompresor)
                    {
                        CANWriteThreadFunc();
                    }
                    else if (PRODUCT_TYPE == PRJTYPE._N2S)
                    {
                        N2S_CANWriteThreadFunc();
                        //falshingObj = new Flashing(this, (byte)PRODUCT_TYPE);
                    }
                    else if (PRODUCT_TYPE == PRJTYPE._CANUDS40 ||           //CAN UDS(ac7840)
                            PRODUCT_TYPE == PRJTYPE._CANUDS01 ||            //CAN UDS(ac7801)
                             PRODUCT_TYPE == PRJTYPE._SplitFlash_CAN ||     //split flash on CAN
                             PRODUCT_TYPE == PRJTYPE._Chery_CBF)             //Chery CBF
                    {
                        falshingObj = new Flashing(this, (byte)PRODUCT_TYPE);
                    }
                    else if (PRODUCT_TYPE == PRJTYPE._7Kw && m_strHexBinExtension == ".bin")
                        IncludeTextMessage("Bus adapter connect not correct,please connect Toomoss for flashing app file.");
                }
            }
            else
                IncludeTextMessage("Please press 'Browse' button input fireware file first.");
        }

        #region LIN message flashing paragraph (xc2234)

        ///<summary>
        ///Execute flash work flow
        ///<paramref name="nMaxBlockSize"/>singal block byte numbers<paramref >
        /// </summary>
        private void UpgrateFirmware(object nMaxBlockSize)
        {
            int nPerPackDataNum = 0;
            int nBlockNum = 0;
            int n0x36PackNum = 1;
            //ECU feedback max number of block size.
            PACK_SIZE = (int)nMaxBlockSize;
            //Here is pure data total length per package in which will download data. 
            nPerPackDataNum = (PACK_SIZE / m_RecData[0].uRecordLength);
            nBlockNum = m_RecData.Count / nPerPackDataNum /*+ m_RecData.Count % nPerPackDataNum*/;
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            FlashFirewareHandler ffHandler = new FlashFirewareHandler(FlashFirmware_TH);
            
            for (int x = 1; x < nBlockNum; x++)
            {
                this.BeginInvoke(ffHandler, new object[] { x, n0x36PackNum++, nBlockNum});
                Thread.Sleep(260);

                //wait for single block write response
                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                this.Invoke(new MethodInvoker(delegate () { bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5); }));

                this.Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x + 1)); }));

                if (n0x36PackNum > 0xFF)
                    n0x36PackNum = 1;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        private bool FlashFirmware_TH(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize)
        {
            //sending data
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            nCurrPackPos = nCurrIndex - 1;

            byte[] DataBuffer = new byte[] { };
            nPack = PACK_SIZE / m_RecData[nCurrPackPos].uRecordLength;

            if (nCurrIndex < nMaxBlockSize)
            {
                for (gAddrOffset = nCurrPackPos * nPack; gAddrOffset < (nCurrPackPos + 1) * nPack; gAddrOffset++)
                    DataBuffer = Combine(DataBuffer, m_RecData[gAddrOffset].Data);

                nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                if (nCurrIndex < nMaxBlockSize - 1)
                    UpdateProgerss(nProgress);
                else
                    UpdateProgerss(100);
            }
            else //last package size will not equal PackSize
            {
                int nLastMsgCount = nCurrIndex * nPack;
                for (int i = gAddrOffset; i < nLastMsgCount; i++)
                    DataBuffer = Combine(DataBuffer, m_RecData[i].Data);

                UpdateProgerss(100);
            }

            //int read_data_num = br.Read(DataBuffer0, gAddrOffset, PackSize);
            /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes0 = Convert.ToByte(nCurrIndex >> 8);
            byte bTimes1 = Convert.ToByte(nCurrIndex & 0xFF);
            _36Svr_Times = Combine(new byte[] { 0x36 }, new byte[] { bTimes0, bTimes1 }); 

            DataBuffer = Combine(_36Svr_Times, DataBuffer);
            Write_Message(DataBuffer);

            return true;
        }

        /// <summary>
        /// sends 5 times request to ECU for stable get response message
        /// </summary>
        /// <param name="reqMsg">UDS request message</param>
        /// <param name="respMsg">UDS response message</param>
        public int Send5TimeReqMsg(byte[] reqMsg, ref byte[] respMsg)
        {
            int k = 0, nResult = -1;
            for (int i = 0; i < respMsg.Length; i++)
                respMsg[i] = 0;

            while (++k < 6)
            {
                nResult = Write_Message(reqMsg);
                if (nResult != 0)
                    return nResult;

                if(PRODUCT_TYPE == PRJTYPE._xc2234)
                    Thread.Sleep(30);
                else if(PRODUCT_TYPE == PRJTYPE._7Kw)
                    Thread.Sleep(10);
                else
                    Thread.Sleep(10);

                nResult = (int)ReadMessage(ref respMsg);
                if (respMsg[0] == 0x78)
                {
                    for(int x = 0; x<5; x++)
                    {
                        Thread.Sleep(500);
                        nResult = (int)ReadMessage(ref respMsg);

                        if (nResult > 0 && respMsg[0] != 0x7F)
                            break;
                    }
                }

                if (nResult > 0 && respMsg[0]!=0x7F)
                    break;
            }
            return nResult;
        }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        private void LINWriteThreadFunc(/*object sender, DoWorkEventArgs e*/)
        {
            //bool bPreFlashOK = false;
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {            
                #region CAN UDS INIT OPEARATION
                /*
                //Pre flashing step
                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extend session
                Write_Message(m_ReqMsg);                
                m_RespMsg = m_lin_msg.data;
                if (m_RespMsg[1] == 0x50 && m_RespMsg[2] == 0x03)
                {
                    IncludeTextMessage("Enter Extend session.");
                    m_ReqMsg = new byte[] { 0x31, 0x01, 0xDF, 0xF0 }; // Ex: ECU SelfTest:DFF0
                    Write_Message(m_ReqMsg);
                    m_RespMsg = m_lin_msg.data;
                    if ((m_RespMsg[1] == 0x71 && m_RespMsg[2] == 0x01) ||
                        (m_RespMsg[1] != 0x10 && m_RespMsg[2] != 0x02))
                    {
                        IncludeTextMessage("ECU software selftest succeed.");
                        m_ReqMsg = new byte[] { 0x85, 0x02}; // Close DTC
                        Write_Message(m_ReqMsg);
                        m_RespMsg = m_lin_msg.data;
                        if (m_RespMsg[1] == 0xC5 && m_RespMsg[2] == 0x02)
                        {
                            IncludeTextMessage("DTC record closed.");
                            m_ReqMsg = new byte[] { 0x28, 0x03 }; // Close Communication
                            Write_Message(m_ReqMsg);
                            m_RespMsg = m_lin_msg.data;
                            if (m_RespMsg[1] == 0x68 && m_RespMsg[2] == 0x03)
                            {
                                IncludeTextMessage("CAN bus communication offline.");

                                string strData = DateTime.Today.ToString("d");
                                string[] dataArr = strData.Split('/');                               
                                string strYear = dataArr[2];
                                int nMonth = int.Parse(dataArr[0]);
                                int nDay = int.Parse(dataArr[1]);
                                int nYear1 = int.Parse(strYear.Substring(0, 2));
                                int nYear2 = int.Parse(strYear.Substring(2, 2));

                                string strYear1 = Convert.ToString(nYear1, 16);
                                string strYear2 = Convert.ToString(nYear2, 16);
                                string strMonth = Convert.ToString(nMonth, 16);
                                string strDay = Convert.ToString(nDay, 16);

                                string strConvertDate = strYear1 + strYear2 + strMonth + strDay;
                                byte[] date = ConvertHexStr2ByteArray(strConvertDate);

                                m_ReqMsg = new byte[] { 0x2E, 0xF1, 0x99 }; //ProgrammingOrConfigurationDate
                                m_ReqMsg = Combine(m_ReqMsg, date);

                                Write_Message(m_ReqMsg);
                                m_RespMsg = m_lin_msg.data;
                                if (m_RespMsg[1] == 0x6E && m_RespMsg[2] == 0xF1 && m_RespMsg[3] == 0x99)
                                {
                                    IncludeTextMessage("Programming Or Configuration Date %s write succeed." + strConvertDate);
                                    bPreFlashOK = true;
                                }
                                else
                                    NegativeMessage(0x2E, m_RespMsg);                                
                            }
                            else
                                NegativeMessage(0x28, m_RespMsg);
                        }
                        else
                            NegativeMessage(0x85, m_RespMsg);
                    }
                    else
                        NegativeMessage(0x31, m_RespMsg);
                }
                else
                    NegativeMessage(0x10, m_RespMsg);               
                    */
                #endregion

                //Main flashing step
                //if (bPreFlashOK)
                {
                    //SetConnectionStatus(false);
                    //m_bStartDownload = true; //Enable TestPresent 0x3E

                    m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programe session
                    if(Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                    {
                        MessageBox.Show("Message ID not correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    m_RespMsg = respMsg;

                    if (m_RespMsg[0] == 0x50 && m_RespMsg[1] == 0x02)
                    {
                        #region backup CODE
                        //m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                        //Send5TimeReqMsg(m_ReqMsg, ref respMsg);
                        //m_RespMsg = m_lin_msg.data;
                        //if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x01) //Not support 0x27 service now.
                        {
                            //byte[] SeedArray = new byte[4];
                            //byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                            //for (int i = 0; i < 4; i++)
                            //    SeedArray[i] = m_RespMsg[++i];
                            //fConvert.seedToKey(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                            //m_ReqMsg = new byte[6];
                            //m_ReqMsg[0] = 0x27;
                            //m_ReqMsg[1] = 0x02;
                            //m_ReqMsg[2] = KeyArray[0];
                            //m_ReqMsg[3] = KeyArray[1];
                            //m_ReqMsg[4] = KeyArray[2];
                            //m_ReqMsg[5] = KeyArray[3];
                            //////other way key sequence
                            //////m_ReqMsg[2] = KeyArray[3];
                            //////m_ReqMsg[3] = KeyArray[2];
                            //////m_ReqMsg[4] = KeyArray[1];
                            //////m_ReqMsg[5] = KeyArray[0];

                            //Write_Message(m_ReqMsg);
                            //m_RespMsg = m_lin_msg.data;

                            //if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
                            #endregion
                            {
                                IncludeTextMessage("Security access pass.");
                                IncludeTextMessage("Download bootloader file.");
                                //bMainFlashOK = Resp_Download_Finish(this, m_strBootloadFileName); //刷写文件只有1个hex(包括bootloader、application)

                                //if(bMainFlashOK)
                                {
                                    IncludeTextMessage("Bootloader file download succeed.");
                                            
                                    //EraseMemory
                                    //uint uBaseAddr = m_RecInfo[0].uBaseAddress;
                                    //int nHexTotalLen = m_RecInfo[m_RecInfo.Count - 1].nTotalLen;
                                    //MEMORY_ADDR = uBaseAddr;
                                    //MEMORY_SIZE = (uint)nHexTotalLen;

                                    //Earse command
                                    m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                                    //Memory address
                                    string strDownloadADDR = Convert.ToString(MEMORY_ADDR, 16);
                                    byte[] DownloadADDR = HexStringToByteArray(strDownloadADDR);     //ConvertHexStr2ByteArray(strDownloadADDR);
                                    //byte[] DownloadADDR = Combine(new byte[1] { 0x00 }, DownloadADDR0);
                                    //Memory size
                                    string strDownloadLEN = Convert.ToString(MEMORY_SIZE, 16);
                                    byte[] DownloadLEN = HexStringToByteArray(strDownloadLEN);    // ConvertHexStr2ByteArray(strDownloadLEN);
                                    //byte[] EraseMemory0 = Combine(new byte[2] { 0x00, 0x00 }, DownloadLEN);

                                    byte[] EraseMemory1 = Combine(m_ReqMsg, DownloadADDR);
                                    byte[] EraseMemory = Combine(EraseMemory1, DownloadLEN);
                                    //Earse whole command
                                    Write_Message(EraseMemory);

                                    int nBlocks = 0;
                                    bool bGetPositiveResp = false;

                                    bGetPositiveResp = Resp_TH(ref nBlocks, 350);                    
                                    if (bGetPositiveResp)
                                    {
                                        IncludeTextMessage("Ecu's application be earsed.");
                                        IncludeTextMessage("System will download application file.");

                                        //waitting for earse finish
                                        Thread.Sleep(100);

                                        bMainFlashOK = Resp_Download_Finish();

                                        IncludeTextMessage("Application file has been finished download.");
                                    }
                                    else
                                    {
                                        NegativeMessage(0x31, m_RespMsg);
                                        IncludeTextMessage("Some issue occure when earse ecu's application file.");
                                            
                                        return;
                                    }
                                }
                                //else
                                //{
                                //    IncludeTextMessage("Some issue occure when download bootloader file.");
                                //    return;
                                //}
                        }
                        //else
                        //    NegativeMessage(0x27, m_RespMsg);
                    }
                    //else
                    //    NegativeMessage(0x27, m_RespMsg);
                }
                    else
                        NegativeMessage(0x10, m_RespMsg);
                }

                //Back flashing step
                if (bMainFlashOK)
                {
                    m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x01 }; //CheckProgrammingDependencies
                    Write_Message(m_ReqMsg);                 

                    int nBlockNum = 0;
                    bool bGetPositiveResp = false;
                    bGetPositiveResp = Resp_TH(ref nBlockNum, 10);
                    m_RespMsg = m_lin_msg.data;

                    /* Ex. Response byte5 contains:
                    0x04 = Routine Completed successfully
                    0x05 = General Error
                    0x06 = Not all mandatory blocks present
                    0x07 = Hw incompatibility
                    0x08 = Sw incompatibility
                    */
                    if (m_RespMsg[0] == 0x71 && m_RespMsg[1] == 0x01/*m_RespMsg[4] == 0x04*/)
                    {
                        IncludeTextMessage("Application file down succeed.");
                        IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset
                        Write_Message(m_ReqMsg);

                        //Task.Run(() => bGetPositiveResp = Resp_TH(m_RespMsg, ref nBlockNum, 5));
                        bGetPositiveResp = Resp_TH(ref nBlockNum, 50);
                        if (bGetPositiveResp)
                        {
                            IncludeTextMessage("ECU hard reset succeed.");      //this.Invoke(new MethodInvoker(delegate () { IncludeTextMessage("ECU hard reset succeed.");}));
                                
                            m_ReqMsg = new byte[] { 0x14, 0xFF, 0xFF, 0xFF }; // Clear dianostic info
                            Write_Message(m_ReqMsg);    //this.Invoke(new MethodInvoker(delegate () { Write_Message(m_ReqMsg);}));
                                
                            //m_RespMsg = m_lin_msg.data;
                            //m_bStartDownload = false; //stop send 0x3E
                            //m_ReqMsg = new byte[] { 0x85, 0x01 }; // Open DTC
                            //Write_Message(m_ReqMsg);
                            //m_RespMsg = m_lin_msg.data;
                            //if (m_RespMsg[1] == 0xC5 && m_RespMsg[2] == 0x01)
                            //{
                            //    IncludeTextMessage("DTC record opened.");
                            //    m_ReqMsg = new byte[] { 0x28, 0x00 }; // Open Communication
                            //    Write_Message(m_ReqMsg);
                            //    m_RespMsg = m_lin_msg.data;
                            //    if (m_RespMsg[1] == 0x68 && m_RespMsg[2] == 0x00) //EnableRxAndTx
                            //    {
                            //        IncludeTextMessage("Enable Rx and Tx message succeed.");
                            //        m_ReqMsg = new byte[] { 0x14, 0xFF, 0xFF, 0xFF }; // Clear dianostic info
                            //        Write_Message(m_ReqMsg);
                            //        m_RespMsg = m_lin_msg.data;
                            //        m_bStartDownload = false; //stop send 0x3E
                            //    }
                            //    else
                            //        NegativeMessage(0x28, m_RespMsg);
                            //}
                            //else
                            //    NegativeMessage(0x11, m_RespMsg);
                        }
                        else
                            NegativeMessage(0x11, m_RespMsg);
                           
                    }
                    else if (m_RespMsg[1] == 0x71 && m_RespMsg[4] == 0x05)
                        NegativeMessage(0x31, m_RespMsg);
                }

                IncludeTextMessage("Fireware download succeed.");
                
                //SetConnectionStatus(true);
            }
            catch (IOException ep)
            {
                MessageBox.Show(this, ep.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                //m_bStartDownload = false;
            }
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        private bool Resp_Download_Finish()
        {
            //bool bMainFlashOK = false;
            IncludeTextMessage("Security access pass.");
            #region backup code
            ////caculate CRC code
            //UInt32 checkSum = fConvert.CheckSum();
            //byte[] DriCheckSum = new byte[3];
            //DriCheckSum[0] = Convert.ToByte(checkSum >> 16);
            //DriCheckSum[1] = Convert.ToByte(checkSum >> 8);
            //DriCheckSum[2] = Convert.ToByte(checkSum);

            ////Split each request byte array(some hardware traciver need like this way)        
            //byte[] reqID_Addr = Combine(m_ReqMsg, DownloadAddr);
            //m_ReqMsg = Combine(reqID_Addr, MemorySize);
            //byte[] req0 = { 0 };
            //byte[] req1 = { 0 };
            //byte[] req2 = { 0 };
            //Split_ReqData(m_ReqMsg, 5, ref req0, ref req1, ref req2);
            //Write_Message(req0);
            //Thread.Sleep(30);
            //Write_Message(req1);
            //Thread.Sleep(30);
            //Write_Message(req2);
            #endregion

            //memory address for download fireware
            m_ReqMsg = new byte[] { 0x34,0x00,0x44 }; 
            string strDownloadAddr = Convert.ToString(MEMORY_ADDR, 16);
            byte[] DownloadAddr0 = HexStringToByteArray(strDownloadAddr); // ConvertHexStr2ByteArray(strDownloadAddr);
            byte[] DownloadAddr = Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            int nFirmwareFileSize = (int)MEMORY_SIZE;
            string strDownloadLEN = Convert.ToString(MEMORY_SIZE, 16);
            byte[] MemorySize = HexStringToByteArray(strDownloadLEN);        // ConvertHexStr2ByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = Combine(DownloadAddr, MemorySize);
            Write_Message(Total34Req);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5); 
            if (bGetPositiveResp)
            {
                IncludeTextMessage("Data transfer start.");
                try
                {                    
                    m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                    m_WriteThread.IsBackground = true;
                    m_WriteThread.Start(nMaxNumOfBlock);                    

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && m_WriteThread!=null)
                    {
                        IfTimesEnd = m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver)
                        {
                            m_WriteThread.Interrupt();
                            m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));                    
                }
                finally
                {
                    IncludeTextMessage(string.Format("Transfer data succeed."));                    
                }
            }
            else
                NegativeMessage(0x34, m_RespMsg);
           
            //download finish
            byte[] respMsg = new byte[] { 0 };
            m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed
            
            Write_Message(m_ReqMsg);
            Send5TimeReqMsg(m_ReqMsg, ref respMsg);

            if (respMsg[0] == 0x77)
            {
                IncludeTextMessage("Download finished!");
                //bMainFlashOK = true;

                ////routine control 0x0202 check memory by CRC code
                //byte[] CRC = new byte[8] { 0x31, 0x01, 0x02, 0x02, 0x00, 0x00, 0x00, 0x00 };
                //CRC[4] = DriCheckSum[0];
                //CRC[5] = DriCheckSum[1];
                //CRC[6] = DriCheckSum[2];

                //Write_Message(m_ReqMsg);
                //m_RespMsg = m_lin_msg.data;

                //bGetPositiveResp = Resp_TH(m_RespMsg, ref nMaxNumOfBlock, 5);
                //if (bGetPositiveResp)
                {
                    //if (m_RespMsg[4] != DriCheckSum[2])
                    //    IncludeTextMessage("Checksum code check not correct.");
                    //else
                    //    IncludeTextMessage("Checksum code check correct.");
                }
                //else
                //    NegativeMessage(0x31, m_RespMsg);

                return true;
            }
            else
                NegativeMessage(0x37, respMsg);
            

            return false;
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        private bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0)
        {
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;
            while (nLoop < nBlocks)
            {
                if (nLoop > nBlocks)
                    return false;

                ReadMessage(ref resp);

                //finish 0x31 routine control wait
                if (resp[0] == 0x71 && resp[1] == 0x01 && resp[2] == 0xFF 
                    && resp[3] == 0x44 && resp[4] == 0x00)
                {
                    return true;
                }
                if (resp[0] == 0x74 && resp[1] == 0x40) //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = resp[5] - 2;        //(resp[1] >> 4) + resp[5] - 2; 
                    return true;
                }
                if(resp[0] == 0x36 + 0x40 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    return true;
                }
                if(resp[0] == 0x37 + 0x40)
                {
                    return true;
                }
                if (resp[0] == 0x41 && resp[1] == 0x01) //hard reset
                {
                    return true;
                }
                if (resp[0] == 0x50 && resp[1] == 0x02) //service mode switch
                {
                    return true;
                }
                if (resp[0] == 0x51 && resp[1] == 0x01) //hard reset
                {
                    return true;
                }
                else if (resp[1] == 0xC5 && (resp[2] == 0x01 || resp[2] == 0x02)) //DTC enable/disable
                {
                    return true;
                }
                else if (resp[1] == 0x68 || (resp[2] == 0x00 || resp[2] == 0x01)) //communication ON/OFF
                {
                    return true;
                }

                if (resp[0] == 0x7F)
                        nNegResp++;
                if (nNegResp > 5)
                    return false;

                Thread.Sleep(10);
                nLoop++;
            }
            return false;
        }

        #endregion

        #region LIN message flashing paragraph (TP90)

        ///<summary>
        ///Execute flash work flow
        ///<paramref name="nMaxBlockSize"/>singal block byte numbers<paramref >
        /// </summary>
        private void UpgrateFirmware_TP90(object BINADDRINFO)
        {                    
            try
            {                
                FileStream FS = null;
                int n0x36PackNum0 = 1;
                int nMaxNumOfBlock = 0;               
                int nProgressStep = 0;
                int read_data_num = 0;
                int AddrOffset = 0;
                bool bGetPositiveResp = false;
                byte[] DataBuffer = new Byte[] { };

                FlashFirewareHandlerForTP90 ffhHandler = new FlashFirewareHandlerForTP90(FlashFirmware_TH_TP90);

                //ECU feedback max number of block size.
                _Bin_Addr_Len binAddrInfo = (_Bin_Addr_Len)BINADDRINFO;
                PACK_SIZE = (int)binAddrInfo.MaxBlockSize;
                FS = binAddrInfo.FS;

                if (m_nTP90_ReadAddr_Times == 0)
                { 
                    FS.Seek(0, SeekOrigin.Begin);
                    m_FirmwareFileSize = binAddrInfo.BlockLen;
                }
                else if(m_nTP90_ReadAddr_Times == 1)
                {
                    FS.Seek(m_FirmwareFileSize, SeekOrigin.Begin);
                    m_FirmwareFileSize = binAddrInfo.BlockLen;
                }

                lock (m_obj)
                {
                    for (AddrOffset = 0; AddrOffset < m_FirmwareFileSize;)
                    {
                        DataBuffer = new Byte[PACK_SIZE];
                        read_data_num = FS.Read(DataBuffer, 0, PACK_SIZE);
                        Thread.Sleep(20);

                        nProgressStep = (int)(((float)(AddrOffset + read_data_num) / (float)m_FirmwareFileSize) * 100.0f);

                        if (read_data_num != PACK_SIZE) //if last package size not equal PACK_SIZE(0x80)
                        {
                            byte[] LastBlock = new byte[read_data_num];
                            Array.Copy(DataBuffer, LastBlock, read_data_num);
                            this.BeginInvoke(ffhHandler, new object[] { LastBlock, 100, m_n0x36PackNum++ });
                        }
                        else
                        {
                            this.BeginInvoke(ffhHandler, new object[] { DataBuffer, nProgressStep, m_n0x36PackNum++ });
                        }

                        Thread.Sleep(100);

                        //wait for single block write response
                        this.Invoke(new MethodInvoker(delegate () { bGetPositiveResp = Resp_TH_TP90(ref nMaxNumOfBlock, 50, m_n0x36PackNum); }));
                        this.Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", n0x36PackNum0++)); }));

                        if (!bGetPositiveResp)
                        {
                            m_bTransferDataOK = false;
                            this.Invoke(new MethodInvoker(delegate () { IncludeTextMessage("downloading fireware failure."); }));
                            break;
                        }

                        if (m_n0x36PackNum > 0xFF)
                            m_n0x36PackNum = 0;

                        AddrOffset += read_data_num;
                    }
                }
                
                m_nTP90_ReadAddr_Times++;
            }
            catch(IOException ioEx)
            {
                MessageBox.Show(this, ioEx.Message, "Warnning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="syncFsOBJ">FileSteam object for read .bin file's content</param>
        /// <param name="buffer">store readed bytes</param>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        public int FlashFirmware_TH_TP90(/*object syncFsOBJ,*/ byte[] buffer, int nCurrIndex, int n0x36PackCnt)
        {
            /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes0 = Convert.ToByte(n0x36PackCnt);
            _36Svr_Times = Combine(new byte[] { 0x36 }, new byte[] { bTimes0 });

            //byte[] buffer = new byte[PACK_SIZE];
            //FileStream FS = (FileStream)syncFsOBJ;
            //int read_data_num = FS.Read(buffer, 0, PACK_SIZE);

            //if (read_data_num != PACK_SIZE) //if last package size not equal PACK_SIZE(0x80)
            //{
            //    byte[] LastBlock = new byte[read_data_num];
            //    Array.Copy(buffer, LastBlock, read_data_num);
            //    buffer = Combine(_36Svr_Times, LastBlock);
            //}
            //else
                buffer = Combine(_36Svr_Times, buffer);

            Write_Message(buffer);
            UpdateProgerss(nCurrIndex);

            return buffer.Length; //read_data_num;
        }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        private void LINWriteThreadFunc_TP90()
        {
            //bool bPreFlashOK = false;
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {
                //Main flashing step
                //if (bPreFlashOK)
                {
                    SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                    m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                    if (Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                    {
                        MessageBox.Show("Message ID not correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    if (m_RespMsg[0] == 0x50 && m_RespMsg[1] == 0x03)
                    {
                        m_ReqMsg = new byte[] { 0x27, 0x05 }; //SubFunc05、06

                        byte[] resp0x27 = new byte[18];
                        Send5TimeReqMsg(m_ReqMsg, ref resp0x27);
 
                        if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x05) //SubFunc05、06
                        {
                            byte[] reqKEY = new byte[18];
                            byte[] SeedArray = new byte[16];
                            byte[] KeyArray = new byte[16];
                            for (int i = 0; i < 16; i++)
                                SeedArray[i] = resp0x27[i + 2];
                            fConvert.seedToKey3(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                            reqKEY[0] = 0x27;
                            reqKEY[1] = 0x06; //                  SubFunc05、06
                            reqKEY[2] = KeyArray[0];
                            reqKEY[3] = KeyArray[1];
                            reqKEY[4] = KeyArray[2];
                            reqKEY[5] = KeyArray[3];

                            reqKEY[6] = 0x31;
                            reqKEY[7] = 0x01;
                            reqKEY[8] = 0xFF;
                            reqKEY[9] = 0x00;

                            reqKEY[10] = 0x08;
                            reqKEY[11] = 0x00;
                            reqKEY[12] = 0x90;
                            reqKEY[13] = 0x00;

                            reqKEY[14] = 0x00;
                            reqKEY[15] = 0x17;
                            reqKEY[16] = 0x00;
                            reqKEY[17] = 0xFF;

                            Send5TimeReqMsg(reqKEY, ref respMsg);
                            if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x06)
                            {
                                IncludeTextMessage("Security access pass.");

                                m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                                Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                                if (m_RespMsg[0] == 0x50 && m_RespMsg[1] == 0x02)
                                {
                                    //session mode changed, so need send 0x27 0x05, 0x27 0x06 once more
                                    m_ReqMsg = new byte[] { 0x27, 0x05 }; //SubFunc05、06

                                    byte[] resp0x27_2 = new byte[18];
                                    Send5TimeReqMsg(m_ReqMsg, ref resp0x27_2);

                                    if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x05) //SubFunc05、06
                                    {
                                        Send5TimeReqMsg(reqKEY, ref respMsg);
                                        if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x06)
                                        {
                                            //Enable TestPresent 0x3E  & message view rolling
                                            m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                            Write_Message(m_ReqMsg);
                                            Thread.Sleep(10);
                                            ReadMessage(ref m_RespMsg);
                                            if (m_RespMsg[0] == 0x7E && m_RespMsg[1] == 0x00)
                                            {
                                                lock (this)
                                                {
                                                    m_bEnable_0x3E = true;
                                                }
                                            }
                                            else
                                            {
                                                IncludeTextMessage("0x3E service not work normally.");
                                                return;
                                            }

                                            //Earse command
                                            m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x00, 0x10 };
                                            //cause test following diagnostic service,so marked now20240119
                                            Write_Message(m_ReqMsg); // new earsing command   
                                            //waitting for earse finish
                                            Thread.Sleep(100);

                                            int nBlocks = 0;
                                            bool bGetPositiveResp = false;
                                             bGetPositiveResp = Resp_TH_TP90(ref nBlocks, 350);
                                            if (bGetPositiveResp)
                                            {
                                                IncludeTextMessage("Ecu's application be earsed.");
                                                IncludeTextMessage("System will download application file.");

                                                bMainFlashOK = Resp_Download_Finish_TP90();
                                                if(bMainFlashOK)
                                                    IncludeTextMessage("Application file has been finished download.");
                                            }
                                            else
                                            {
                                                NegativeMessage(0x31, m_RespMsg);
                                                IncludeTextMessage("Some issue occure when earse ecu's application file.");
                                                return;
                                            }
                                        }
                                    }
                                    else
                                        NegativeMessage(0x27, m_RespMsg);
                                }
                                else
                                {
                                    IncludeTextMessage("Enter programme session failure.");
                                    return;
                                }
                            }
                            else
                                NegativeMessage(0x27, m_RespMsg);
                        }
                        else
                            NegativeMessage(0x27, m_RespMsg);
                    }
                    else
                        NegativeMessage(0x10, m_RespMsg);
                }

                //Back flashing step
                if (bMainFlashOK)
                {
                    //m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x01 }; //CheckProgrammingDependencies
                    //Write_Message(m_ReqMsg);

                    int nBlockNum = 0;
                    bool bGetPositiveResp = false;
                    //bGetPositiveResp = Resp_TH_TP90(ref nBlockNum, 10);
                    //m_RespMsg = m_lin_msg.data;

                    //if (m_RespMsg[0] == 0x71 && m_RespMsg[1] == 0x01/*m_RespMsg[4] == 0x04*/)
                    {
                        IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU soft reset
                        Write_Message(m_ReqMsg);

                        bGetPositiveResp = Resp_TH_TP90(ref nBlockNum, 50);
                        if (bGetPositiveResp)
                        {
                            IncludeTextMessage("ECU soft reset succeed."); 
                            IncludeTextMessage("Fireware download succeed.");
                            
                            lock (this)
                            {
                                m_bEnable_0x3E = false;
                                Thread.Sleep(500);
                                m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs

                                UpdateProgerss(100);
                                RefreshDBGridView(); //refresh trace grid view for display newest message

                                SetDonwloadingStatus(false);
                            }
                        }
                        else
                            NegativeMessage(0x11, m_RespMsg);

                    }
                    //else if (m_RespMsg[1] == 0x71 && m_RespMsg[4] == 0x05)
                    //    NegativeMessage(0x31, m_RespMsg);
                }
            }
            catch (IOException ep)
            {
                MessageBox.Show(this, ep.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
               
            }
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        private bool Resp_Download_Finish_TP90()
        {
            #region old way for combine 0x34 service request message
            //memory address for download fireware    
            //m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            //string strDownloadAddr = Convert.ToString(MEMORY_ADDR, 16);
            //byte[] DownloadAddr0 = HexStringToByteArray(strDownloadAddr);
            //byte[] DownloadAddr = Combine(m_ReqMsg, DownloadAddr0);

            ////download size & address combine
            //int nFirmwareFileSize = (int)MEMORY_SIZE;
            //string strDownloadLEN = Convert.ToString(bal.BlockLen, 16);
            //byte[] MemorySize = HexStringToByteArray(strDownloadLEN);

            ////request download command + memory address + memory size
            //byte[] Total34Req = Combine(DownloadAddr, MemorySize);
            //Write_Message(Total34Req);
            #endregion

            _Bin_Addr_Len BAL = new _Bin_Addr_Len();
            byte[] newBlockSize = new byte[4];
            m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            FileStream fs = new FileStream(m_strHexFileName, FileMode.Open, FileAccess.Read);

            foreach (_Bin_Addr_Len bal in m_lstBinInfo)
            {
                //memory address for download fireware
                string strDownloadAddr = Convert.ToString(bal.StartAddress, 16);
                byte[] DownloadAddr0 = HexStringToByteArray(strDownloadAddr);
                byte[] DownloadAddr = Combine(m_ReqMsg, DownloadAddr0);

                //download size & address combine
                string strDownloadLEN = Convert.ToString(bal.BlockLen, 16);
                byte[] MemorySize = HexStringToByteArray(strDownloadLEN);

                //request download command + memory address + memory size
                byte[] Total34Req = Combine(DownloadAddr, MemorySize);
                Write_Message(Total34Req);

                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                bGetPositiveResp = Resp_TH_TP90(ref nMaxNumOfBlock, 5);
                if (bGetPositiveResp)
                {
                    IncludeTextMessage("Data transfer start.");

                    try
                    {
                        //close auto send 0x3E 0x80 when transfer data start
                        m_bEnable_0x3E = false;

                        BAL.StartAddress = bal.StartAddress;
                        BAL.BlockLen = bal.BlockLen;
                        BAL.MaxBlockSize = (uint)nMaxNumOfBlock;
                        BAL.FS = fs;

                        m_WriteThread = new System.Threading.Thread(UpgrateFirmware_TP90);
                        m_WriteThread.IsBackground = true;
                        m_WriteThread.Start(BAL);

                        bool IfTimesEnd = false;
                        bool IfRunOver = false;
                        while (!IfRunOver && m_WriteThread != null || !m_bTransferDataOK)
                        {
                            IfTimesEnd = m_WriteThread.IsAlive;
                            Application.DoEvents();
                            if (!IfTimesEnd || IfRunOver)
                            {
                                m_WriteThread.Interrupt();
                                m_WriteThread.Abort();
                                IfTimesEnd = false;
                                gAddrOffset = 0;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                        fs.Close();
                        return false;
                    }
                    finally
                    {
                        //Open auto send 0x3E 0x80 after 0x36 tansfer data finish.
                        m_bEnable_0x3E = true;
                        IncludeTextMessage(string.Format("Transfer data succeed."));
                    }
                }
                else
                {
                    NegativeMessage(0x34, m_RespMsg);
                    fs.Close();
                    return false;
                }

                //reset 0x36 transfer data sequence number after 1 block transfered.
                m_n0x36PackNum = 1;
                SetWriteDID_ButtonColor("Write DID", Color.Transparent);
            }

            m_nTP90_ReadAddr_Times = 0;
            m_FirmwareFileSize = 0;
            fs.Close();// close .bin file handle after use

            if (m_bTransferDataOK)
            {
                //download finish
                byte[] respMsg = new byte[] { 0 };
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed
                Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                if (respMsg[0] == 0x77)
                {
                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02, 0x10 }; //Integrity check package 
                    Write_Message(m_ReqMsg);

                    int nMaxNumOfBlock = 0;
                    bool bGetPositiveResp = false;
                    bGetPositiveResp = Resp_TH_TP90(ref nMaxNumOfBlock, 5);
                    if (bGetPositiveResp)
                    {
                        IncludeTextMessage("Application file down succeed!");
                    }
                    else
                        IncludeTextMessage("Download fauilure!");
                }
                else
                {
                    NegativeMessage(0x37, respMsg);
                    return false;
                }
            }
            return true;
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        private bool Resp_TH_TP90(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0)
        {
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;

            while (nLoop < nBlocks)
            {
                ReadMessage(ref resp);

                //finish 0x31 routine control wait
                if (resp[0] == 0x71 && resp[1] == 0x01 && resp[2] == 0xFF
                    && resp[3] == 0x00 && resp[4] == 0x00)
                {
                    return true;
                }
                if (resp[0] == 0x71 && resp[5] == 0x00) //integrity check(0x31, 0x01, 0x02, 0x02, 0x10, 0x00)
                {
                    return true;
                }
                if (resp[0] == 0x74 && resp[1] == 0x40) //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = resp[5] - 2;        //(resp[1] >> 4) + resp[5] - 2; 
                    return true;
                }
                if (resp[0] == 0x76 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    //send 0x3E 0x80 manaully when 0x36 transfering data
                    byte[] _0x3E80 = new byte[2] { 0x3E, 0x80 };
                    Write_Message(_0x3E80);

                    return true;
                }
                if (resp[0] == 0x37 + 0x40)
                {
                    return true;
                }
                if (resp[0] == 0x50 && resp[1] == 0x02) //service mode switch
                {
                    return true;
                }
                if (resp[0] == 0x51 && resp[1] == 0x01) //software reset
                {
                    return true;
                }
                if (resp[0] == 0x51 && resp[1] == 0x03) //hard reset
                {
                    return true;
                }

                if (resp[0] == 0x7F)
                    nNegResp++;
                if (nNegResp > 5)
                    return false;

                Thread.Sleep(10);
                nLoop++;
            }
            return false;
        }

        #endregion

        #region CAN message flashing paragraph

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte numbers<paramref >
        /// </summary>
        private void canUpgrateFirmware_TH(object nMaxBlockSize)
        {
            //int n0x34Result = -1;
            int nPerPackDataNum = 0;
            int nBlockNum = 0;
            int nMaxNumOfBlock = 0;
            int nDuring0x34BlockSize = 0;
            int n0x36PackNum = 0x01;
            //bool bHasRemainder = false;
            bool bGetPositiveResp = false;
            
            //ECU feedback max number of block size.
            PACK_SIZE = (int)nMaxBlockSize;
            if (PACK_SIZE == 0)
                return;
            nDuring0x34BlockSize = PACK_SIZE;

            //Here is pure data total length per package in which will download data. 
            nPerPackDataNum = (PACK_SIZE / m_RecData[gCurrPackPos].uRecordLength);
            if (m_RecData.Count % nPerPackDataNum == 0)
            {
                nBlockNum = m_RecData.Count / nPerPackDataNum;
                //bHasRemainder = false;
            }
            else
            {
                nBlockNum = m_RecData.Count / nPerPackDataNum + 1;
                //bHasRemainder = true;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            FlashFirewareHandler ffHandler = new FlashFirewareHandler(canFlashFirmware_TH);
            lock (m_obj)
            {
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    if (x == gCurrPackPos)//1st frame
                        m_b1stFrm = true;
                    else
                        m_b1stFrm = false;
                    #region 0x34 request change to once befor through 0x36 transfer data ①

                    //send 0x34 request before 1st 0x36 request
                    //if (x == gCurrPackPos)
                    //{
                    //    m_nDynStartAddr = 0;    //clear 0x34 package start address
                    //    m_nDynStartAddr += MEMORY_ADDR;
                    //    this.Invoke(new MethodInvoker(delegate () { n0x34Result = Send34Request(m_nDynStartAddr, (uint)PACK_SIZE, 1); }));
                    //    Thread.Sleep(10);
                    //}
                    //else if (x > gCurrPackPos)
                    //{
                    //    if(bHasRemainder)
                    //    {
                    //        int nNextRecordPos = x * nPerPackDataNum;
                    //        nDuring0x34BlockSize = Math.Abs(m_RecData[x].uRecordLength * (m_RecData.Count - nNextRecordPos));

                    //        if (nDuring0x34BlockSize - PACK_SIZE > PACK_SIZE)
                    //            nDuring0x34BlockSize = PACK_SIZE;
                    //        else
                    //        {
                    //            if (nDuring0x34BlockSize / PACK_SIZE != 0)//greater than on block data
                    //                nDuring0x34BlockSize = PACK_SIZE;
                    //            else
                    //            {
                    //                nDuring0x34BlockSize = 0;
                    //                for (int i = nNextRecordPos; i < m_RecData.Count; i++)
                    //                {
                    //                    nDuring0x34BlockSize += m_RecData[i].uRecordLength;
                    //                }
                    //            }
                    //        }                                
                    //    }
                    //    else
                    //        nDuring0x34BlockSize = PACK_SIZE;

                    //    m_nDynStartAddr += (uint)PACK_SIZE;
                    //    this.Invoke(new MethodInvoker(delegate () { n0x34Result = Send34Request(m_nDynStartAddr, (uint)nDuring0x34BlockSize, 1); }));
                    //    Thread.Sleep(30);
                    //}
                    ////for waitting 0x34 service response
                    //Invoke(new MethodInvoker(delegate () { bGetPositiveResp = canResp_TH(ref nMaxNumOfBlock, 50, 0, 0x34); }));
                    
                    //if (n0x34Result == 0 && bGetPositiveResp)
#endregion
                    {
                        this.BeginInvoke(ffHandler, new object[] { x, n0x36PackNum++, nBlockNum });

                        while (!m_b36SvrOneBlockOver)
                        {
                            Thread.Sleep(10);
                        }
                        m_b36SvrOneBlockOver = false;
#if _CheckSum
                        Thread.Sleep(150);
#else
                        Thread.Sleep(10);
#endif

                        //wait for single block write response(0x36)
                        Invoke(new MethodInvoker(delegate () { bGetPositiveResp = canResp_TH(ref nMaxNumOfBlock, 50, x+1, 0x36); }));
                        Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x)); }));

                        if(!bGetPositiveResp)
                        {
                            Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                            break;
                        }
                    }

                    #region 0x34 request change to once befor through 0x36 transfer data ②
                    //else
                    //{
                    //    Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Can not receive 0x34 service response in transfering data, thread exited.")); }));
                    //    break;
                    //}
                    #endregion

                    if (!bGetPositiveResp)
                        break;

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x01;

                }

                if (!bGetPositiveResp)
                    m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        private bool canFlashFirmware_TH(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize)
        {
            //sending data
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };
            
            nCurrPackPos = nCurrIndex;           
            nPack = PACK_SIZE / m_RecData[nCurrPackPos].uRecordLength;
            if (nCurrIndex < nMaxBlockSize-1)
            {
                for (gAddrOffset = nCurrPackPos * nPack; gAddrOffset < (nCurrPackPos + 1) * nPack; gAddrOffset++)
                    DataBuffer = Combine(DataBuffer, m_RecData[gAddrOffset].Data);

                nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                UpdateProgerss(nProgress);
            }
            else //last package size will not equal PackSize
            {
                int nLastMsgCount = m_RecData.Count;
                for (int i = gAddrOffset; i < nLastMsgCount; i++)
                    DataBuffer = Combine(DataBuffer, m_RecData[i].Data);
                nLastMsgByteCount = DataBuffer.Length;

                UpdateProgerss(100);
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = Combine(_36Svr_Times, DataBuffer);

            Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            //single block 0x36 data package sent
            m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return true;
        }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        private void CANWriteThreadFunc()
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {
                //SetConnectionStatus(false);             
                gCurrPackPos = 0;   //reset 0x36 sent package counter

                m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                nSendResult = Write_CANMessage(m_ReqMsg, true);

                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                bGetPositiveResp = canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);

                if(bGetPositiveResp)
                {
                    //Enable TestPresent 0x3E  & message view rolling
                    m_ReqMsg = new byte[] { 0x3E, 0x00 };                  
                    Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(100);
                    if (m_RespMsg[1] == 0x7E && m_RespMsg[2] == 0x00)
                    {
                        m_ReqMsg = new byte[] { 0x3E, 0x80 };
                        Write_CANMessage(m_ReqMsg, true);
                        Thread.Sleep(100);

                        lock(this)
                        {
                            m_bEnable_0x3E = true;
                        }                    
                    }
                    else
                    {
                        IncludeTextMessage("0x3E service not work normally.");
                        return;
                    }

#if _SecurityAccess

                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(150);
                    if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x01) //Not support 0x27 service now.
                    {
                        byte[] SeedArray = new byte[4];
                        byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                        for (int i = 0; i < SeedArray.Length; i++)
                            SeedArray[i] = m_RespMsg[i + 3];
                        fConvert.seedToKey2(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                        m_ReqMsg = new byte[6];
                        m_ReqMsg[0] = 0x27;
                        m_ReqMsg[1] = 0x02;
                        m_ReqMsg[2] = KeyArray[0];
                        m_ReqMsg[3] = KeyArray[1];
                        m_ReqMsg[4] = KeyArray[2];
                        m_ReqMsg[5] = KeyArray[3];

                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                        Thread.Sleep(100);
                        if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
#endif
                        {
                            IncludeTextMessage("Security access pass.");

                            //Earse command
                            m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                            //Memory address MEMORY_ADDR, MEMORY_SIZE
                            string strDownloadADDR = Convert.ToString(CAN_ADDR, 16);
                            byte[] DownloadADDR = HexStringToByteArray(strDownloadADDR);

                            //Memory size
                            string strDownloadLEN = Convert.ToString(CAN_SIZE, 16);
                            byte[] DownloadLEN = HexStringToByteArray(strDownloadLEN);

                            byte[] EraseMemory1 = Combine(m_ReqMsg, DownloadADDR);
                            byte[] EraseMemory = Combine(EraseMemory1, DownloadLEN);

                            //Earse whole command
                            nSendResult = Write_CANMessage(EraseMemory);
                            IncludeTextMessage("Now earsing flash,please wait for amoument...");

                            int nWaitTime = 0;
                            m_ReqMsg = new byte[] { 0x3E, 0x00 };
                            while (nWaitTime * REQ_3E_INTERVAL < RESP_0x31_WAITTING_TIME) //earsing need expenditure about 6000ms
                            {
                                Write_CANMessage(m_ReqMsg, true);
                                Thread.Sleep(REQ_3E_INTERVAL);
                                nWaitTime++;
                            }
                          
                            int nBlocks = 0;
                            bGetPositiveResp = false;
                            bGetPositiveResp = canResp_TH(ref nBlocks, 100, 0, 0x31);
                            if (bGetPositiveResp)
                            {
                                IncludeTextMessage("Ecu's application be earsed.");
                                IncludeTextMessage("System will download application file.");

                                bMainFlashOK = canResp_Download_Finish();
                                if (bMainFlashOK)
                                    IncludeTextMessage("Application file has been finished download.");
                                else
                                    IncludeTextMessage("Dowload has interupted.");
                            }
                            else
                            {
                                NegativeMessage(0x31, m_RespMsg);
                                IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                return;
                            }
#if _SecurityAccess
                    }
                        else
                            NegativeMessage(0x27, m_RespMsg);
#endif
                   }
                }
                else
                    NegativeMessage(0x10, m_RespMsg);


                //Back flashing step
                if (bMainFlashOK)
                {
                    //m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x01 }; //CheckProgrammingDependencies
                    //Write_CANMessage(m_ReqMsg, true);

                    int nBlockNum = 0;
                    //bGetPositiveResp = false;
                    //bGetPositiveResp = canResp_TH(ref nBlockNum, 10, 0, 0x31);
                    //m_RespMsg = m_lin_msg.data;

                    ///* Ex. Response byte5 contains:
                    //0x04 = Routine Completed successfully
                    //0x05 = General Error
                    //0x06 = Not all mandatory blocks present
                    //0x07 = Hw incompatibility
                    //0x08 = Sw incompatibility
                    //*/
                    //if (m_RespMsg[1] == 0x71 && m_RespMsg[2] == 0x01/*m_RespMsg[4] == 0x04*/)
                    {
                        IncludeTextMessage("Application file down succeed.");
                        IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x03 }; //ECU reset(SoftReset)
                        Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = canResp_TH(ref nBlockNum, 300, 0, 0x11);
                        if (bGetPositiveResp)
                        {
                            IncludeTextMessage("ECU hard reset succeed.");

                            //m_ReqMsg = new byte[] { 0x14, 0xFF, 0xFF, 0xFF }; // Clear dianostic info
                            //Write_CANMessage(m_ReqMsg, true);

                            IncludeTextMessage("Fireware download succeed.");
                        }
                        else
                            NegativeMessage(0x11, m_RespMsg);

                    }
                    //else if (m_RespMsg[1] == 0x71 && m_RespMsg[4] == 0x05)
                    //    NegativeMessage(0x31, m_RespMsg);
                }
                else
                    tmrDisplay.Enabled = false;

                //SetConnectionStatus(true);
            }
            catch (IOException ep)
            {
                MessageBox.Show(this, ep.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                //m_bStartDownload = false;
            }
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        private bool canResp_Download_Finish()
        {
            IncludeTextMessage("Security access pass.");
            //foreach (HexParser.RecordAddrInfo rdi in m_RecInfo)//mutiple block need embended program support
            {
                //   MEMORY_ADDR = rdi.uBaseAddress;
                //   MEMORY_SIZE = (uint)rdi.uRecordLength;

                Send34Request(MEMORY_ADDR, MEMORY_SIZE, 0);
                Thread.Sleep(30);

                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                bGetPositiveResp = canResp_TH(ref nMaxNumOfBlock, 200, 0, 0x34);
                if (bGetPositiveResp)
                {
                    IncludeTextMessage("Data transfer start.");
                    try
                    {
                        m_WriteThread = new System.Threading.Thread(canUpgrateFirmware_TH);
                        m_WriteThread.IsBackground = true;
                        m_WriteThread.Start(nMaxNumOfBlock);

                        bool IfTimesEnd = false;
                        bool IfRunOver = false;
                        while (!IfRunOver && m_WriteThread != null)
                        {
                            IfTimesEnd = m_WriteThread.IsAlive;
                            Application.DoEvents();
                            if (!IfTimesEnd || IfRunOver || !m_bTransferDataOK)
                            {
                                m_WriteThread.Interrupt();
                                m_WriteThread.Abort();
                                IfTimesEnd = false;
                                gAddrOffset = 0;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                    }
                    finally
                    {
                        //IncludeTextMessage(string.Format("Transfer data succeed."));
                    }
                }
                else
                    NegativeMessage(0x34, m_RespMsg);

                gCurrPackPos++;
            }

            if(m_bTransferDataOK)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(30);
                if (m_RespMsg[1] == 0x77)
                {
                    IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    lock (this)
                    { m_bEnable_0x3E = false; }
                    return true;
                }
                else
                    NegativeMessage(0x37, m_RespMsg);
            }        

            return false;
        }

        /// <summary>
        /// UDS 34 service
        /// </summary>
        /// <param name="startAddr">UDS request start address</param>
        /// <param name="dataLen">UDS request data length</param>
        /// <param name="nRequestTime">send 0x34 request. 0: is first whole hex length request; 1: is following data transfer request</param>
        /// <returns></returns>
        private int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if(nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        public bool canResp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, byte reqID = 0x00)
        {
            bool bResult = false;
            byte[] resp = new byte[8]; 
            int nLoop = 0, nNegResp = 0;
            while (true)
            {
                if (nLoop > nBlocks)
                {
                    bResult = false;
                    break;
                }
                ProcessFollowCtrl(reqID);

                if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    ReadMessage(ref resp);
                    m_RespMsg = resp;
                }
                resp = m_RespMsg;

                //finish 0x31 routine control wait
                if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0xFF
                    && resp[4] == 0x44 && resp[5] == 0xFF)
                {
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x74 && resp[2] == 0x40)//for LIN bus //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = resp[5] - 2;        //(resp[1] >> 4) + resp[5] - 2; 
                    bResult = true;
                    break;
                }
                if(resp[1] == 0x74 && resp[2] == 0x10) //for CAN bus
                {
                    nMaxNumOfBlockLen = /*(resp[1] & 0x0F) +*/ resp[3];
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x76 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x37 + 0x40)
                {
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x41 && resp[2] == 0x01) //hard reset
                {
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x50 && resp[2] == 0x02) //service mode switch
                {
                    bResult = true;
                    break;
                }
                if (resp[1] == 0x51 && resp[2] == 0x01) //soft reset
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0xC5 && (resp[2] == 0x01 || resp[2] == 0x02)) //DTC enable/disable
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x68 || (resp[2] == 0x00 || resp[2] == 0x01)) //communication ON/OFF
                {
                    bResult = true;
                    break;
                }

                if (resp[1] == 0x7F)
                    nNegResp++;

                if (nNegResp > 5)
                    break;

                Thread.Sleep(10);
                nLoop++;
            }

            return bResult;
        }

        /// <summary>
        /// Waitting for ecu response after execute PCAN_Write func
        /// </summary>
        /// <param name="nWaittingTime">wait time(millisecond)</param>
        /// <returns></returns>
        private byte[] WaitForCANResp(int nWaittingTime)
        {
            byte[] recv_msg = new byte[] { };
            byte[] resp = new byte[8];

            Thread.Sleep(nWaittingTime);

            int nRespose = (int)ReadMessage(ref resp);
            if (nRespose == (int)TPCANStatus.PCAN_ERROR_OK)
            {
                recv_msg = m_can_msg.CANMsg.DATA;
            }
            return recv_msg;
        }

        #endregion

        #region  N2s project:: CAN message flashing paragraph

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte number<paramref >
        /// </summary>
        private void N2S_canUpgrateFirmware_TH(object o0x36DataPack)
        {
            int nPerPackDataNum = 0;
            int nBlockNum = 0;
            int nMaxNumOfBlock = 0;
            int nDuring0x34BlockSize = 0;
            int n0x36PackNum = 0x01;
            bool bGetPositiveResp = false;

            //ECU feedback max number of block size.
            N2S_DataPack n2s_datapack = (N2S_DataPack)o0x36DataPack;
            PACK_SIZE = n2s_datapack.nMaxNumOfBlock;
            byte[] Total36Data = n2s_datapack.All0x36PackData;

            if (PACK_SIZE == 0)
                return;
            nDuring0x34BlockSize = PACK_SIZE;

            //Here is pure data total length per package in which will download data. 
            if (PACK_SIZE >= 0x80)
            {
                nPerPackDataNum = (PACK_SIZE / m_RecData[gCurrPackPos].uRecordLength);
                if (m_RecData.Count % nPerPackDataNum == 0)
                {
                    nBlockNum = m_RecData.Count / nPerPackDataNum;
                }
                else
                {
                    nBlockNum = m_RecData.Count / nPerPackDataNum + 1;
                }
            }
            else if(PACK_SIZE == 0x3A)
            {
                PACK_SIZE -= 2;
                if (m_Total36Data.Length % PACK_SIZE == 0)
                    nBlockNum = Total36Data.Length / PACK_SIZE;
                else
                    nBlockNum = Total36Data.Length / PACK_SIZE + 1;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));
            
            N2S_FlashFirewareHandler ffHandler = new N2S_FlashFirewareHandler(N2S_canFlashFirmware_TH);
            lock (m_obj)
            {
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    if (x == gCurrPackPos)//1st frame
                        m_b1stFrm = true;
                    else
                        m_b1stFrm = false;
                    
                    this.BeginInvoke(ffHandler, new object[] { x, n0x36PackNum++, nBlockNum, Total36Data });

                    while (!m_b36SvrOneBlockOver)
                    {
                        Thread.Sleep(10);
                    }
                    m_b36SvrOneBlockOver = false;
#if _CheckSum
                    Thread.Sleep(150);
#else
                    //Thread.Sleep(10);
#endif
                    //wait for single block write response(0x36)
                    Invoke(new MethodInvoker(delegate () { bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, P2_ServerTime*10, x + 1, 0x36); }));
                    Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x+1)); }));

                    if (!bGetPositiveResp)
                    {
                        Invoke(new MethodInvoker(delegate () { IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                        break;
                    }                    

                    if (!bGetPositiveResp)
                        break;

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x00;
                }

                if (!bGetPositiveResp)
                    m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        public bool N2S_canFlashFirmware_TH(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack)
        {
            //sending data
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };

            nCurrPackPos = nCurrIndex;
            if (PACK_SIZE >= 0x80)
            {
                nPack = PACK_SIZE / m_RecData[nCurrPackPos].uRecordLength;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    for (gAddrOffset = nCurrPackPos * nPack; gAddrOffset < (nCurrPackPos + 1) * nPack; gAddrOffset++)
                        DataBuffer = Combine(DataBuffer, m_RecData[gAddrOffset].Data);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    UpdateProgerss(nProgress);
                }
                else //last package size will not equal PackSize
                {
                    int nLastMsgCount = m_RecData.Count;
                    for (int i = gAddrOffset; i < nLastMsgCount; i++)
                        DataBuffer = Combine(DataBuffer, m_RecData[i].Data);
                    nLastMsgByteCount = DataBuffer.Length;

                    UpdateProgerss(100);
                }
            }
            else if(PACK_SIZE == 0x38)
            {
                int nLastPackSize = 0;
                nLastPackSize = _0x36DataPack.Length % nLastMsgByteCount;

                if(nLastPackSize == 0)
                {
                    DataBuffer = new byte[nLastMsgByteCount];
                    m_nSourceIndex = nCurrPackPos * nLastMsgByteCount;
                    Array.Copy(_0x36DataPack, m_nSourceIndex, DataBuffer, 0, nLastMsgByteCount);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);

                    if (nCurrIndex < nMaxBlockSize - 1)
                        UpdateProgerss(nProgress);
                    else
                        UpdateProgerss(100);
                }
                else
                {
                    if (nCurrIndex < nMaxBlockSize - 1)
                    {
                        DataBuffer = new byte[nLastMsgByteCount];
                        m_nSourceIndex = nCurrPackPos * nLastMsgByteCount;
                        Array.Copy(_0x36DataPack, m_nSourceIndex, DataBuffer, 0, nLastMsgByteCount);

                        nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                        UpdateProgerss(nProgress);
                    }
                    else
                    {
                        DataBuffer = new byte[nLastPackSize];

                        Array.Copy(_0x36DataPack, _0x36DataPack.Length - nLastPackSize, DataBuffer, 0, nLastPackSize);
                        nLastMsgByteCount = nLastPackSize;
                        UpdateProgerss(100);
                    }
                }
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = Combine(_36Svr_Times, DataBuffer);

            if(nLastMsgByteCount>4)
                Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            else
                Write_CANMessage(DataBuffer, true, true, nLastMsgByteCount);

            //single block 0x36 data package sent
            m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return true;
        }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        private void N2S_CANWriteThreadFunc()
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] respMsg = new byte[8];

            try
            {
                SetDonwloadingStatus(true);//disable all of buttons which associate with diag message func when downnloading.
                gCurrPackPos = 0;   //reset 0x36 sent package counter 

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                nSendResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(3 * P2_ServerTime);

                if (m_RespMsg[1] == 0x50 && m_RespMsg[2] == 0x03)
                {
#if _SecurityAccess

                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(3* P2_ServerTime);

                    if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x01) //Not support 0x27 service now.
                    {
                        byte[] SeedArray = new byte[4];
                        byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                        for (int i = 0; i < SeedArray.Length; i++)
                            SeedArray[i] = m_RespMsg[i + 3];
                        fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                        m_ReqMsg = new byte[6];
                        m_ReqMsg[0] = 0x27;
                        m_ReqMsg[1] = 0x02;
                        m_ReqMsg[2] = KeyArray[0];
                        m_ReqMsg[3] = KeyArray[1];
                        m_ReqMsg[4] = KeyArray[2];
                        m_ReqMsg[5] = KeyArray[3];

                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                        Thread.Sleep(2* P2_ServerTime);
                        if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
#endif
                        {
                            m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                            if (bGetPositiveResp)
                            {
                                //Enable TestPresent 0x3E  & message view rolling
                                m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                Write_CANMessage(m_ReqMsg, true);
                                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 500, 0, 0x3E);
                                if (bGetPositiveResp)
                                {
                                    m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                    Write_CANMessage(m_ReqMsg, true);
                                    Thread.Sleep(2* P2_ServerTime);

                                    //lock (this)
                                    //{
                                    //    m_bEnable_0x3E = true;
                                    //}
                                }
                                else
                                {
                                    IncludeTextMessage("0x3E service not work normally.");
                                    return;
                                }

#if _SecurityAccess
                                m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                Thread.Sleep(3* P2_ServerTime);
                                if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x01) 
                                {
                                    SeedArray = new byte[4];
                                    KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                    for (int i = 0; i < SeedArray.Length; i++)
                                        SeedArray[i] = m_RespMsg[i + 3];
                                    fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                                    m_ReqMsg = new byte[6];
                                    m_ReqMsg[0] = 0x27;
                                    m_ReqMsg[1] = 0x02;
                                    m_ReqMsg[2] = KeyArray[0];
                                    m_ReqMsg[3] = KeyArray[1];
                                    m_ReqMsg[4] = KeyArray[2];
                                    m_ReqMsg[5] = KeyArray[3];

                                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                                    Thread.Sleep(2* P2_ServerTime);
                                    if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
#endif
                                    {
#if _SecurityAccess
                                        m_ReqMsg = new byte[] { 0x27, 0x09 }; //Security access,request seed
                                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                                        Thread.Sleep(3* P2_ServerTime);
                                        if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x09)
                                        {
                                            SeedArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                            KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                            for (int i = 0; i < SeedArray.Length; i++)
                                                SeedArray[i] = m_RespMsg[i + 3];
                                            fConvert.N2S_seedToKey(SeedArray, out KeyArray, 9);   //according response seed caculate security access key

                                            m_ReqMsg = new byte[6];
                                            m_ReqMsg[0] = 0x27;
                                            m_ReqMsg[1] = 0x0A;
                                            m_ReqMsg[2] = KeyArray[0];
                                            m_ReqMsg[3] = KeyArray[1];
                                            m_ReqMsg[4] = KeyArray[2];
                                            m_ReqMsg[5] = KeyArray[3];

                                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                                            Thread.Sleep(2* P2_ServerTime);
                                            if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x0A)
#endif
                                            {
                                                IncludeTextMessage("Security access pass.");

                                                //write DIDs value(F15A) in Programing session
                                                bool bDID_F15A_Right = false, bDID_008C_Right = false ;
                                                string strIniFile;
                                                byte[] writeDID_F15A = new byte[3] { 0x2E, 0xF1, 0x5A};
                                                byte[] writeDID_008C = new byte[3] { 0x2E, 0x00, 0x8C };

                                                strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";

                                                //write DIDs value(F15A) in Programing session
                                                bDID_F15A_Right = Excute_Write_DID(strIniFile, "F15A", writeDID_F15A, 9, 1);

                                                //write DIDs value(008C) in Programing session
                                                bDID_008C_Right = Excute_Write_DID(strIniFile, "008C", writeDID_008C, 14, 1);

                                                writeDID_F15A[0] = 0x2E;
                                                writeDID_008C[0] = 0x2E;

                                                if (!bDID_F15A_Right)
                                                {
                                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID_F15A)));
                                                    return;
                                                }
                                                else if(!bDID_008C_Right)
                                                {
                                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID_008C)));
                                                    return;
                                                }
                                                else
                                                {
                                                    SetWriteDID_ButtonColor("Write DID", Color.Transparent);

                                                    IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID_F15A)));
                                                    IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID_008C)));
                                                }

                                                //Earse command
                                                m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x00, 0x44 };
                                                //Memory address MEMORY_ADDR, MEMORY_SIZE
                                                string strDownloadADDR = Convert.ToString(CAN_ADDR, 16);
                                                byte[] DownloadADDR = HexStringToByteArray(strDownloadADDR);

                                                //Memory size
                                                string strDownloadLEN = Convert.ToString(CAN_SIZE, 16);
                                                byte[] DownloadLEN = HexStringToByteArray(strDownloadLEN);

                                                byte[] EraseMemory1 = Combine(m_ReqMsg, DownloadADDR);
                                                byte[] EraseMemory = Combine(EraseMemory1, DownloadLEN);

                                                //Earse whole command
                                                nSendResult = Write_CANMessage(EraseMemory);
                                                IncludeTextMessage("Now earsing flash,please wait for amoument...");

                                                int nWaitTime = 0;
                                                m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                                while (nWaitTime * REQ_3E_INTERVAL < RESP_0x31_WAITTING_TIME/10) //earsing need expenditure about 6000ms
                                                {
                                                    Write_CANMessage(m_ReqMsg, true);
                                                    Thread.Sleep(REQ_3E_INTERVAL * 10);
                                                    nWaitTime++;
                                                }

                                                int nBlocks = 0;
                                                bGetPositiveResp = false;
                                                bGetPositiveResp = N2S_canResp_TH(ref nBlocks, 100, 0, 0);
                                                if (bGetPositiveResp)
                                                {
                                                    IncludeTextMessage("Ecu's application be earsed.");
                                                    IncludeTextMessage("System will download application file.");

                                                    bMainFlashOK = N2S_canResp_Download_Finish();
                                                    if (bMainFlashOK)
                                                        IncludeTextMessage("Application file has been finished download.");
                                                    else
                                                        IncludeTextMessage("Dowload has be interupted.");
                                                }
                                                else
                                                {
                                                    NegativeMessage(0x31, m_RespMsg);
                                                    IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                                    return;
                                                }
                                            }
                                        }
                                        else
                                            NegativeMessage(0x27, m_RespMsg);

#if _SecurityAccess
                                    }
                                    else
                                        NegativeMessage(0x27, m_RespMsg);
#endif
                                }
                            }
                            else
                                NegativeMessage(0x10, m_RespMsg);

                            //Back flashing step
                            if (bMainFlashOK)
                            {
                                //caculate checksum by c dll func
                                int y = 0;
                                byte[] CheckSumR = new byte[4];
                                uint cCheckSum = fConvert.N2S_CheckSum(m_Total36Data, N2S_MASK);
                                byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);
                                //big ending convert
                                for (int z = bCheckSum.Length-1; z >= 0; z--)
                                    CheckSumR[y++] = bCheckSum[z];

                                m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                                Byte[] n2s_checksum = Combine(m_ReqMsg, CheckSumR);
                                Write_CANMessage(n2s_checksum);                                

                                int nBlockNum = 0;
                                bGetPositiveResp = false;
                                bGetPositiveResp = N2S_canResp_TH(ref nBlockNum, 200, 0, 0x31);
                                if (bGetPositiveResp)
                                {
                                    IncludeTextMessage("All of transfer hex data consistency check pass!");
                                    IncludeTextMessage("ECU will reboot,please wait for a moment.");

                                    m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset(SoftReset)
                                    Write_CANMessage(m_ReqMsg, true);

                                    bGetPositiveResp = N2S_canResp_TH(ref nBlockNum, 50, 0, 0x11);
                                    if (bGetPositiveResp)
                                    {
                                        SetDonwloadingStatus(false);
                                        IncludeTextMessage("ECU hard reset succeed.");
                                        IncludeTextMessage("Fireware download succeed.");
                                    }
                                    else
                                        NegativeMessage(0x11, m_RespMsg);

                                }
                                else
                                    NegativeMessage(0x31, m_RespMsg);
                            }
                            else
                                tmrDisplay.Enabled = false;
                        }
                    }
                }
            }
            catch (IOException ep)
            {
                MessageBox.Show(this, ep.Message, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        private bool N2S_canResp_Download_Finish()
        {
            IncludeTextMessage("Security access pass.");

            N2S_Send34Request(MEMORY_ADDR, MEMORY_SIZE, 0);
            Thread.Sleep(P2_ServerTime);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 200, 0, 0x34);
            if (bGetPositiveResp)
            {
                IncludeTextMessage("Data transfer start.");
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = m_Total36Data;

                    m_WriteThread = new System.Threading.Thread(N2S_canUpgrateFirmware_TH);
                    m_WriteThread.IsBackground = true;
                    m_WriteThread.Start(m_N2SDataPack);

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && m_WriteThread != null)
                    {
                        IfTimesEnd = m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver || !m_bTransferDataOK)
                        {
                            m_WriteThread.Interrupt();
                            m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                }
            }
            else
                NegativeMessage(0x34, m_RespMsg);

            gCurrPackPos++;
            

            if (m_bTransferDataOK)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(30);
                if (m_RespMsg[1] == 0x77)
                {
                    IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    lock (this)
                    { 
                        m_bEnable_0x3E = false;
                        m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs
                    }
                    
                    return true;
                }
                else
                    NegativeMessage(0x37, m_RespMsg);
            }

            return false;
        }

        /// <summary>
        /// UDS 34 service
        /// </summary>
        /// <param name="startAddr">UDS request start address</param>
        /// <param name="dataLen">UDS request data length</param>
        /// <param name="nRequestTime">send 0x34 request. 0: is first whole hex length request; 1: is following data transfer request</param>
        /// <returns></returns>
        private int N2S_Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }
        
        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        public bool N2S_canResp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, byte reqID = 0x00)
        {
            bool bResult = false;
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;
            while (true)
            {
                if (nLoop > nBlocks)
                {
                    bResult = false;
                    break;
                }
                if (PRODUCT_TYPE == PRJTYPE._N2S)
                    N2S_ProcessFollowCtrl(reqID);

                if (reqID == 0x31)
                {
                    Thread.Sleep(300);
                    ReadMessage(ref resp);
                }
                else if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    ReadMessage(ref resp);
                    m_RespMsg = resp;
                }
                else if(reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    ReadMessage(ref resp);
                    m_RespMsg = resp;
                }
                else
                    resp = m_RespMsg;

                //finish 0x31 routine control wait
                if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0xFF
                    && resp[4] == 0x44 && resp[5] == 0xFF)
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0xFF
                    && resp[4] == 0x00 && resp[5] == 0x00) //for N2S flash respose
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0x02 && resp[4] == 0x02) //for N2S CheckDependency
                {
                    if (resp[5] == 0x0)
                        bResult = true;
                    else
                        bResult = false;
                    break;
                }
                else if (resp[1] == 0x74 && resp[2] == 0x40)//for LIN bus //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = (resp[3] << 24) + (resp[4] << 16) + (resp[5] << 8) + resp[6];                    

                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x74 && resp[2] == 0x10) //for CAN bus
                {
                    nMaxNumOfBlockLen = /*(resp[1] & 0x0F) +*/ resp[3];
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x76 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x37 + 0x40)
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x41 && resp[2] == 0x01) //hard reset
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x41 && resp[2] == 0x02) //software reset
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x50 && resp[2] == 0x02) //service mode switch
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x50 && resp[2] == 0x03) //service mode switch
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x51 && resp[2] == 0x01) //soft reset
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 || resp[1] == 0x01) //security access request seed
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 || resp[1] == 0x02) //security access send key
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 || resp[1] == 0x0A) //security access send key
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x62) //response read DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0xC5 && (resp[2] == 0x01 || resp[2] == 0x02)) //DTC enable/disable
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x68 && (resp[2] == 0x00 || resp[2] == 0x01)) //communication ON/OFF
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x7E && resp[2] == 0x00)
                {
                    bResult = true;
                    break;
                }

                if (resp[1] == 0x7F)
                    nNegResp++;

                if (nNegResp > 5)
                    break;

                Thread.Sleep(10);
                nLoop++;
            }

            return bResult;
        }

        #endregion

        /// <summary>
        /// update flash progress bar value
        /// </summary>
        /// <param name="nStep">current progress position</param>
        public void UpdateProgerss(int nStep)
        {
            pBar.Value = nStep;
            if (pBar.Value >= pBar.Maximum)
                pBar.Value = pBar.Maximum;
            pBar.Refresh();
        }

        /// <summary>
        /// Split length greater than 5 bytes request data array into 3 byte array
        /// </summary>
        /// <param name="inByteArray"></param>
        /// <param name="nSegSize">each segment byte array size</param>
        /// <param name="req0">be splited byte array number 1</param>
        /// <param name="req1">be splited byte array number 2</param>
        /// <param name="req2">be splited byte array number 3</param>
        private void Split_ReqData(byte[] inByteArray,int nSegSize, ref byte[] req0, ref byte[] req1, ref byte[] req2)
        {
            int j = 0, k = 0, l = 0;
            int nSeg = inByteArray.Length / nSegSize;
            int nMod = inByteArray.Length % nSegSize;

            try
            {
                for (int i = 0; nMod > 0 ? i <= nSeg : i < nSeg; i++)
                {
                    if (i <= nSeg - 1)
                    {
                        if (i == 0)
                        {
                            req0 = new byte[nSegSize];
                            for (j = 0; j < req0.Length; j++)
                            { req0[j] = inByteArray[j]; }
                        }
                        else if (i == 1)
                        {
                            req1 = new byte[nSegSize];
                            for (k = 0; k < req1.Length; k++)
                            { req1[k] = inByteArray[j + k]; }
                        }
                        else if (i == 2)
                        {
                            req2 = new byte[nSegSize];
                            for (l = 0; l < req1.Length; l++)
                            { req2[l] = inByteArray[j + k + l]; }
                        }
                    }
                    else if (nMod > 0)
                    {
                        req2 = new byte[nMod];
                        for (l = 0; l < nMod; l++)
                            req2[l] = inByteArray[j + k + l];
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("worte byte out of range::"+ex.Message);
            }           
        }

        /// <summary>
        /// Use Linq and lammda expression,convert hex string to byte array
        /// </summary>
        /// <param name="HexString">hex string</param>
        /// <returns></returns>
        private byte[] ConvertHexStr2ByteArray(string HexString)
        {
            int byteLength = 0;
            string strTemp = string.Empty;

            byteLength = HexString.Length;
            if (byteLength < 8)
            {
                byteLength = 8 - byteLength;
                for (int i = 0; i < byteLength; i++)
                {
                    strTemp += "0";
                }
                HexString = strTemp + HexString;
            }
            else
            {
                if(byteLength%2 > 0)
                {
                    strTemp += "0";
                    HexString = HexString + strTemp;
                }
            }

            return Enumerable.Range(0, HexString.Length)
                            .Where(x => x % 2 == 0)
                            .Select(x => Convert.ToByte(HexString.Substring(x, 2), 16))
                            .ToArray();
        }

        /// <summary>
        /// Hex string convert to byte array
        /// </summary>
        /// <param name="hexString">hex string</param>
        /// <returns></returns>
        public byte[] HexStringToByteArray(string hexString, bool bReadDTC = false)
        {
            int byteLength = 0;
            int byteCount = 0;
            string strTemp = string.Empty;

            byte[] byteArray = new byte[] { };
            try
            {
                byteLength = hexString.Length;
                if(!bReadDTC)
                {
                    if(byteLength < 8)
                    {
                        byteLength = 8 - byteLength;
                        for(int i = 0; i< byteLength; i++)
                        {
                            strTemp += "0";
                        }
                        hexString = strTemp + hexString;
                    }
                }
                else
                {
                    //input dtc length must even
                    if ((byteLength % 2) > 0)
                    {
                        hexString += "0";
                    }
                }
             
                byteCount = hexString.Length / 2;
                byteArray = new byte[byteCount];

                for (int i = 0; i < byteCount; i++)
                {
                    string byteString = hexString.Substring(i * 2, 2);
                    byteArray[i] = Convert.ToByte(byteString, 16);
                }
            }
            catch(Exception ex)
            {
                IncludeTextMessage("An error occoured when string request convert to hex byte array:" + ex.Message);
            }            

            return byteArray;
        }

        ///<summary>
        ///combine 2 byte array
        /// </summary>
        public byte[] Combine(byte[] first, byte[] second)
        {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }

        ///<summary>
        ///earse byte array
        /// </summary>
        private void SweepByteArr(ref byte[] BR)
        {
            for (int i =0; i< BR.Length; i++)
                BR[i] = 0x0;
        }

        /// <summary>
        /// Get download file size
        /// </summary>
        /// <param name="strDownloadFile">down file name</param>
        /// <returns></returns>
        private byte[] GetMemorySize(string strDownloadFile)
        {
            int nFirmwareFileSize = (int)new FileInfo(strDownloadFile).Length;
            string strHex = Convert.ToString(nFirmwareFileSize, 16);
            byte[] MemorySize = Encoding.UTF8.GetBytes(strHex);
            return MemorySize;
        }

        ///<summary>
        ///Process negative response message infomation
        /// </summary>
        public void NegativeMessage(byte baseID, byte[] negMsg)
        {
            if (negMsg.Length == 0)
                return;
            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x12)
                    IncludeTextMessage("sub-functionNotSupported - This NRC shall be sent if the sub-function parameter is not supported.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x13)
                    IncludeTextMessage("incorrectMessageLengthOrInvalidFormat - This NRC shall be sent if the length of the message is wrong.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x22)
                    IncludeTextMessage("conditionsNotCorrect - Used when the server is in a critical normal mode activity and therefore cannot disable / enable the requested communication type.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x24)
                    IncludeTextMessage("requestSequenceError - Send if the ‘sendKey’ sub-function is received without first receiving a ‘requestSeed’ request messag");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x31)
                    IncludeTextMessage("requestOutOfRange - The server shall use this response code, if it detects an error in the communicationType or nodeIdentificationNumber parameter.");
                else if ((negMsg[1] == baseID + 0x40 && negMsg[2] == 0x33) || (negMsg[0] == 0x10 && negMsg[1] == 0x02))
                    IncludeTextMessage("securityAccessDenied - This NRC shall be returned if the server is secure (for server’s that support the SecurityAccess service) when a request for this service has been received.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x35)
                    IncludeTextMessage("invalidKey - Send if an expected 'sendKey' sub-function value is received and the value of the key does not match the server's internally stored/calculated key.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x36)
                    IncludeTextMessage("exceededNumberOfAttempts - Send if the delay timer is active due to exceeding the maximum number of allowed false access attempts.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x37)
                    IncludeTextMessage("requiredTimeDelayNotExpired - Send if the delay timer is active and a request is transmitted.");
                else if (negMsg[1] == baseID + 0x40 && negMsg[2] == 0x72)
                    IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                else if (negMsg[1] == baseID + 0x4E)
                    IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                else if (negMsg[1] == 0x67 && negMsg[2] != 0x02)
                    IncludeTextMessage("InvalidKey on security access service.");

                tmrDisplay.Enabled = false;
            }
            else if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                if (PRODUCT_TYPE == PRJTYPE._Chery_CBF)
                {
                    if (negMsg[1] == baseID && negMsg[2] == 0x12)
                        IncludeTextMessage("sub-functionNotSupported - This NRC shall be sent if the sub-function parameter is not supported.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x13)
                        IncludeTextMessage("incorrectMessageLengthOrInvalidFormat - This NRC shall be sent if the length of the message is wrong.");
                    else if (negMsg[1] == baseID  && negMsg[2] == 0x22)
                        IncludeTextMessage("conditionsNotCorrect - Used when the server is in a critical normal mode activity and therefore cannot disable / enable the requested communication type.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x24)
                        IncludeTextMessage("requestSequenceError - Send if the ‘sendKey’ sub-function is received without first receiving a ‘requestSeed’ request messag");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x31)
                        IncludeTextMessage("requestOutOfRange - The server shall use this response code, if it detects an error in the communicationType or nodeIdentificationNumber parameter.");
                    else if ((negMsg[1] == baseID  && negMsg[2] == 0x33) || (negMsg[1] == 0x10 && negMsg[2] == 0x02))
                        IncludeTextMessage("securityAccessDenied - This NRC shall be returned if the server is secure (for server’s that support the SecurityAccess service) when a request for this service has been received.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x35)
                        IncludeTextMessage("invalidKey - Send if an expected 'sendKey' sub-function value is received and the value of the key does not match the server's internally stored/calculated key.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x36)
                        IncludeTextMessage("exceededNumberOfAttempts - Send if the delay timer is active due to exceeding the maximum number of allowed false access attempts.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x37)
                        IncludeTextMessage("requiredTimeDelayNotExpired - Send if the delay timer is active and a request is transmitted.");
                    else if (negMsg[1] == baseID && negMsg[2] == 0x72)
                        IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                    else if (negMsg[1] == baseID + 0x4E)
                        IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                    else if (negMsg[1] == baseID  && negMsg[2] != 0x01)
                        IncludeTextMessage("InvalidKey on security access service.");
                }
                else
                {
                    if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x12)
                        IncludeTextMessage("sub-functionNotSupported - This NRC shall be sent if the sub-function parameter is not supported.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x13)
                        IncludeTextMessage("incorrectMessageLengthOrInvalidFormat - This NRC shall be sent if the length of the message is wrong.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x22)
                        IncludeTextMessage("conditionsNotCorrect - Used when the server is in a critical normal mode activity and therefore cannot disable / enable the requested communication type.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x24)
                        IncludeTextMessage("requestSequenceError - Send if the ‘sendKey’ sub-function is received without first receiving a ‘requestSeed’ request messag");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x31)
                        IncludeTextMessage("requestOutOfRange - The server shall use this response code, if it detects an error in the communicationType or nodeIdentificationNumber parameter.");
                    else if ((negMsg[0] == baseID + 0x40 && negMsg[1] == 0x33) || (negMsg[0] == 0x10 && negMsg[1] == 0x02))
                        IncludeTextMessage("securityAccessDenied - This NRC shall be returned if the server is secure (for server’s that support the SecurityAccess service) when a request for this service has been received.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x35)
                        IncludeTextMessage("invalidKey - Send if an expected 'sendKey' sub-function value is received and the value of the key does not match the server's internally stored/calculated key.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x36)
                        IncludeTextMessage("exceededNumberOfAttempts - Send if the delay timer is active due to exceeding the maximum number of allowed false access attempts.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x37)
                        IncludeTextMessage("requiredTimeDelayNotExpired - Send if the delay timer is active and a request is transmitted.");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] == 0x72)
                        IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                    else if (negMsg[0] == baseID + 0x4E)
                        IncludeTextMessage("generalProgrammingFailure - This NRC shall be returned if the server detects an error when finalizing the data transfer between the client and server(e.g., via an integrity check).");
                    else if (negMsg[0] == baseID + 0x40 && negMsg[1] != 0x01)
                        IncludeTextMessage("InvalidKey on security access service.");

                }
                
                tmrDisplay.Enabled = false;
            }
            //enable buttons when flash action failure.
            SetDonwloadingStatus(false);
        }

        /// <summary>
        /// Parse response message when receive follow crtl byte
        /// </summary>
        /// <param name="reqID">request ID</param>
        /// <returns>0:status ok, 1:wait for 10 millisecond, -1:over follow</returns>
        private int ProcessFollowCtrl(byte reqID)
        {
            int nResult = -1;
                   
            m_FollowControl = m_RespMsg[0];
            m_BlockSize = m_RespMsg[1];
            m_WaitTime = m_RespMsg[2];

            m_FC0 = Convert.ToByte((m_FollowControl & 0xF0));
            m_FC1 = Convert.ToByte((m_FollowControl & 0x0F));

            if (m_FC0 != 0x30) //not follow ctrl
            {
                nResult = 0;
                return nResult;                   
            }

            if (m_FC0 == 0x30 && m_BlockSize == 0x12 && m_WaitTime == 0x0A)//block size:0x12
            {
                byte[] resp = new byte[8];
                if (reqID == 0x31)
                    Thread.Sleep(600);//120ms change to 600ms,according hardware response time
                else
                    Thread.Sleep(120);
                if (0 == ReadMessage(ref resp))
                    nResult = 0;

                return nResult;
            }
            else if (m_FC0 == 0x30 && m_FC1 == 0x00) //continue send
            {
                nResult = 0;
                return nResult;
            }
            else if (m_FC0 == 0x30 && m_FC1 == 0x01)//wait
            {
                Thread.Sleep(m_WaitTime);
                nResult = 1;
            }
            else if (m_FC0 == 0x30 && m_FC1 == 0x02) //over flow
            {
                IncludeTextMessage("Current send data over flollow(Resp:0x30 0x02...)");
                return nResult;
            }
            
            return nResult;
        }

        /// <summary>
        /// Parse response message when receive follow crtl byte
        /// </summary>
        /// <param name="reqID">request ID</param>
        /// <returns>0:status ok, 1:wait for 10 millisecond, -1:over follow</returns>
        public int N2S_ProcessFollowCtrl(byte reqID)
        {
            int nResult = -1;
            int nWaitCount = 0;
            while (true)
            {
                m_FollowControl = m_RespMsg[0];
                m_BlockSize = m_RespMsg[1];
                m_WaitTime = m_RespMsg[2];

                m_FC0 = Convert.ToByte((m_FollowControl & 0xF0));
                m_FC1 = Convert.ToByte((m_FollowControl & 0x0F));

                if (nWaitCount > ST_MIN)//wait time greater than st_min(50ms)，return false;
                    break;

                if (m_FC0 == 0x30) //continue send
                {
                    nResult = 0;
                    break;
                }
                else if (m_FC0 == 0x31)//wait
                {
                    Thread.Sleep(m_WaitTime);
                    nResult = 1;
                }
                else if (m_FC0 == 0x32) //over flow
                {
                    IncludeTextMessage("Current send data over flollow(Resp:0x30 0x02...)");
                    nResult = 0;
                    break;
                }
                Thread.Sleep(1);
                nWaitCount++;
            }

            return nResult;
        }

        ///<summary>
        ///Write message to CAN bus
        ///<param name="msgs"/> diag request message</param>
        ///<param name="bSingleFrame">does frame data single or not?</param>
        ///<param name="b36Req">does request is 0x36</param>
        ///<param name="nLastMsgByteCount">use to make last block length while whole data length not be 0x80 divide</param>
        ///</summary>
        public int Write_CANMessage(byte[] msgs, bool bSingleFrame = false, bool b36Req = false, int nLastMsgByteCount = 0x80)
        {
            int nResult = -1;
            int nDatalen = 0;
            int loBitDataLen = 0;
            int hiBitDataLen = 0;
            int k = 0;  //Byte position in mutiple frame bytes array
            byte svrID;
            byte[] new_msg0 = new byte[2];
            byte[] new_msg1 = new byte[6];
            byte[] new_msg = new byte[8];
            bool bReConter36 = false;
            try
            {
                if (bSingleFrame)
                {
                    int nLen = msgs.Length;
                    byte[] byte0 = new byte[] { Convert.ToByte(nLen) };
                    new_msg = Combine(byte0, msgs);

                    // Send the message
                    Write_Message(new_msg);
                    Thread.Sleep(P2_ServerTime);
                }
                else
                {
                    //1st frame
                    {
                        nDatalen = msgs.Length;
                        svrID = msgs[0];

                        loBitDataLen = ((nDatalen >> 8) & 0x0F);

                        if (svrID != 0x36 || m_b1stFrm)
                        {
                           new_msg0[0] = Convert.ToByte(loBitDataLen + 0x10);
                           bReConter36 = true;
                        }
                        else
                        {
                            if (m_n36SvrPackNum == 0)
                            {
                                m_n36SvrPackNum = 0x10;
                                new_msg0[0] = Convert.ToByte(m_n36SvrPackNum++);                     
                                bReConter36 = true;
                            }
                            else
                                new_msg0[0] = Convert.ToByte(m_n36SvrPackNum++);
                        }

                        if (b36Req)
                        {
                            if (nLastMsgByteCount == 0x80)
                                hiBitDataLen = (PACK_SIZE + 2) & 0xFF;
                            else
                                hiBitDataLen = (nLastMsgByteCount + 2) & 0xFF;   
                        }
                        else
                            hiBitDataLen = (nDatalen & 0x00FF);

                         new_msg0[1] = Convert.ToByte(hiBitDataLen);

                        //copy 1st frame residue bytes(except 1st frame mark and frame length 2 byte)
                        for (k = 0; k < (new_msg.Length - new_msg0.Length); k++)
                            new_msg1[k] = msgs[k];
                        new_msg = Combine(new_msg0, new_msg1);

                        // Send the message
                        Write_Message(new_msg);

                        Thread.Sleep(P2_ServerTime);
                        
                    }
                    //backward frame
                    {
                        byte BlockSize = 7; //singel frame 1st byte is sequence number,so backfoward data len is 7
                        byte[] new_msgX = new byte[] { };

                        int n1stByteTimes = 0x21; //first frame sequence number
                        int nAfter1stFrameSent = 0;

                        int x = 0;
                        int y = 0;
#if _CheckSum
                        if (svrID != 0x36)
                        {
                            int nEndBytes = 0;
                            nAfter1stFrameSent = nDatalen - k;
                            if (nAfter1stFrameSent > BlockSize)
                                nEndBytes = nAfter1stFrameSent % BlockSize;
                            else
                                nEndBytes = 0;

                            if (bReConter36)
                            {
                                m_n36SvrPackNum = 0x21;
                                bReConter36 = false;
                            }

                            int nSingleFrmOfOneBlockSent = -1;
                            for (x = k; x < nDatalen; x += BlockSize)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                {
                                    if (m_n36SvrPackNum == 0)
                                        m_n36SvrPackNum = 0x21;
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);
                                }

                                for (y = 0; y < BlockSize; y++)
                                {
                                    if ((x + y) < msgs.Length)
                                        new_msgX[y + 1] = msgs[x + y];
                                }

                                if (nEndBytes == 1)
                                {
                                    byte byte_end = msgs[msgs.Length - 2];
                                    new_msgX[y] = byte_end;
                                }

                                // Send the message
                                nSingleFrmOfOneBlockSent = Write_Message(new_msgX);
                                nResult = nSingleFrmOfOneBlockSent;

                                Console.Write("0x36 svr block{0:d},result{1:d}", x, nSingleFrmOfOneBlockSent);
                                //waitting for incoming message
                                Thread.Sleep(10);
                                
                            }

                            //last bytes of message block
                            int t = 0;
                            byte[] EndBytes = new byte[] { };
                            if (nEndBytes == 1)
                            {
                                byte EndByte1 = msgs[msgs.Length - 1];
                                EndBytes = new byte[msgs.Length - x + 1];

                                if (svrID != 0x36)
                                    EndBytes[t++] = Convert.ToByte(n1stByteTimes);
                                else
                                    EndBytes[t++] = Convert.ToByte(m_n36SvrPackNum);

                                EndBytes[t] = EndByte1;

                                // Send the message
                                Write_Message(EndBytes);
                                nEndBytes = 0;
                                
                            }
                        }
                        else 
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = fConvert.CheckSum(msgs);  //msgs;  
                            nAfter1stFrameSent = checksum.Length - k;
                            if (nAfter1stFrameSent > BlockSize)
                                nEndBytes = nAfter1stFrameSent % BlockSize;
                            else
                                nEndBytes = nAfter1stFrameSent; //resedue bytes less than 7 bytes,so combine it with checksum bytes.

                            if (bReConter36)
                            {
                                m_n36SvrPackNum = 0x21;
                                bReConter36 = false;
                            }

                        #region  //combine residue bytes way2

                            int t = k;
                            int n7ByteGroups = nAfter1stFrameSent / BlockSize;
                            for(x = 0; x < n7ByteGroups; x++)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);

                                for (y = 0; y < BlockSize; y++)
                                {
                                    if (t < msgs.Length)
                                    {
                                        new_msgX[y + 1] = msgs[t++];                                        
                                    }
                                }
                                //If the data length is 128 bytes, the checksum bit be filled at the end of the data
                                if (x == n7ByteGroups - 1)
                                {
                                    byte checksum0 = checksum[checksum.Length - 2];
                                    byte checksum1 = checksum[checksum.Length - 1];
                                    new_msgX[y - 1] = checksum0;
                                    new_msgX[y] = checksum1;
                                }
                                // Send the message
                                nResult = WriteFrame(new_msgX);
                                Thread.Sleep(10);
                                
                            }
                            if(nEndBytes>0) //tail block(less than 7 bytes)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);

                                int p = 0;
                                k = t;
                                for (; p< nEndBytes; p++)
                                {
                                    if(k<msgs.Length)
                                        new_msgX[p + 1] = msgs[k++];
                                }
                                byte checksum0 = checksum[checksum.Length - 2];
                                byte checksum1 = checksum[checksum.Length - 1];
                                new_msgX[p++] = checksum0;
                                new_msgX[p] = checksum1;

                                // Send the message
                                nResult = WriteFrame(new_msgX);
                                Thread.Sleep(10);
                                
                            }
                        #endregion


                        #region //combine residue bytes way1(process last package not equal 0x80,will apear issue usually)
                            //int nRemainderBytes = 0;
                            //int nEndPos = 0;
                            //for (x = k; x < nAfter1stFrameSent; x += BlockSize)
                            //{
                            //    if (m_n36SvrPackNum > 0x2F)
                            //        m_n36SvrPackNum = 0x20;

                            //    new_msgX = new byte[BlockSize + 1];
                            //    for (int u = 0; u < new_msgX.Length; u++)
                            //        new_msgX[u] = 0xFF;

                            //    if (svrID != 0x36)
                            //        new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                            //    else
                            //        new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);

                            //    for (y = 0; y < BlockSize; y++)
                            //    {
                            //        if ((x + y) < msgs.Length)
                            //            new_msgX[y + 1] = msgs[x + y];
                            //    }

                            //    //If the data length is 128 bytes, the checksum bit is filled at the end of the data
                            //    if ((nAfter1stFrameSent - x < BlockSize) && (nAfter1stFrameSent == PACK_SIZE-2))
                            //    {                                    
                            //        byte checksum0 = checksum[checksum.Length - 2];
                            //        byte checksum1 = checksum[checksum.Length - 1];
                            //        new_msgX[y - 1] = checksum0;
                            //        new_msgX[y] = checksum1;
                            //    }//else remember current byte position, then process in following program(nRemainderBytes!=0)
                            //    else if ((nAfter1stFrameSent - x < BlockSize) && nAfter1stFrameSent != PACK_SIZE - 2)
                            //    {
                            //        if (nEndBytes != 0)
                            //        {
                            //            nRemainderBytes = nEndBytes - 2;//delete checksum bytes
                            //            nEndPos = x + y - 1;
                            //        }
                            //    }
                            //    // Send the message
                            //    nResult = WriteFrame(new_msgX);
                            //    Thread.Sleep(10);
                            //}

                            //if(nRemainderBytes!=0 && nLastMsgByteCount> 0x10)//find remainder bytes,so fixed and add checksum byte for request message
                            //{
                            //    int nDataPos = 1;
                            //    new_msgX = new byte[BlockSize + 1];
                            //    for (int u = 0; u < new_msgX.Length; u++)
                            //    {
                            //        if (u == 0)
                            //            new_msgX[u] = Convert.ToByte(m_n36SvrPackNum);
                            //        else if (u >= 1 && u < nRemainderBytes + 1)
                            //        {
                            //            new_msgX[u + 1] = msgs[nEndPos + nDataPos];
                            //            nDataPos++;
                            //        }
                            //        else if (u >= nRemainderBytes + 1 && u <= nRemainderBytes + 2)
                            //        {
                            //            if (u == nRemainderBytes + 1) // base on block number byte and data,add first byte of checksum bytes
                            //            {
                            //                new_msgX[u] = checksum[checksum.Length - 2];
                            //            }
                            //            else if (u == nRemainderBytes + 2) // base on block number byte and data,add second byte of checksum bytes
                            //            {
                            //                new_msgX[u] = checksum[checksum.Length - 1];
                            //            }
                            //        }
                            //        else if (u >= nRemainderBytes + 3)//3:means is byte0: 0x36 frame counter + checksum0 + checksum1
                            //        {
                            //            new_msgX[u] = 0xFF;
                            //        }
                            //    }
                            //    // Send the message
                            //    nResult = WriteFrame(new_msgX);
                            //    Thread.Sleep(10);
                            //}

                            //// Send the checksum 2 byte message,if 0x36 service data block length equal 0x4(equal 0x20,means has 1 line data)
                            //if (nLastMsgByteCount == 0x4)
                            //{
                            //    new_msgX = new byte[BlockSize + 1];
                            //    for (int u = 0; u < new_msgX.Length; u++)
                            //    {
                            //        if (u > 2)
                            //            new_msgX[u] = 0xFF;
                            //        else if (u == 0)
                            //            new_msgX[u] = Convert.ToByte(m_n36SvrPackNum);
                            //        else if (u == 1)
                            //            new_msgX[u] = checksum[checksum.Length - 2];
                            //        else if (u == 2)
                            //            new_msgX[u] = checksum[checksum.Length - 1];
                            //    }

                            //    nResult = WriteFrame(new_msgX);
                            //    Thread.Sleep(10);
                            //}

                        #endregion
                        }
#else
                        if (svrID != 0x36)
                        {
                            int nEndBytes = 0;
                            nAfter1stFrameSent = nDatalen - k;
                            if (nAfter1stFrameSent > BlockSize)
                                nEndBytes = nAfter1stFrameSent % BlockSize;
                            else
                                nEndBytes = 0;

                            if (bReConter36)
                            {
                                m_n36SvrPackNum = 0x21;
                                bReConter36 = false;
                            }

                            int nSingleFrmOfOneBlockSent = -1;
                            for (x = k; x < nDatalen; x += BlockSize)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                {
                                    if (m_n36SvrPackNum == 0)
                                        m_n36SvrPackNum = 0x21;
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);
                                }

                                for (y = 0; y < BlockSize; y++)
                                {
                                    if ((x + y) < msgs.Length)
                                        new_msgX[y + 1] = msgs[x + y];
                                }

                                if (nEndBytes == 1)
                                {
                                    byte byte_end = msgs[msgs.Length - 2];
                                    new_msgX[y] = byte_end;
                                }

                                // Send the message
                                nSingleFrmOfOneBlockSent = Write_Message(new_msgX);
                                nResult = nSingleFrmOfOneBlockSent;

                                Console.Write("0x36 svr block{0:d},result{1:d}", x, nSingleFrmOfOneBlockSent);
                                //waitting for incoming message
                                Thread.Sleep(P2_ServerTime);
                                
                            }

                            //last bytes of message block
                            int t = 0;
                            byte[] EndBytes = new byte[] { };
                            if (nEndBytes == 1)
                            {
                                byte EndByte1 = msgs[msgs.Length - 1];
                                EndBytes = new byte[msgs.Length - x + 1];

                                if (svrID != 0x36)
                                    EndBytes[t++] = Convert.ToByte(n1stByteTimes);
                                else
                                    EndBytes[t++] = Convert.ToByte(m_n36SvrPackNum);

                                EndBytes[t] = EndByte1;

                                // Send the message
                                Write_Message(EndBytes);
                                nEndBytes = 0;
                            }
                        }
                        else 
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = msgs;  //fConvert.CheckSum(msgs);
                            nAfter1stFrameSent = checksum.Length - k;
                            if (nAfter1stFrameSent > BlockSize)
                                nEndBytes = nAfter1stFrameSent % BlockSize;
                            else
                                nEndBytes = nAfter1stFrameSent; //resedue bytes less than 7 bytes,so combine it with checksum bytes.

                            if (bReConter36)
                            {
                                m_n36SvrPackNum = 0x21;
                                bReConter36 = false;
                            }

                        #region  //combine residue bytes way2

                            int t = k;
                            int n7ByteGroups = nAfter1stFrameSent / BlockSize;
                            for(x = 0; x < n7ByteGroups; x++)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);

                                for (y = 0; y < BlockSize; y++)
                                {
                                    if (t < msgs.Length)
                                    {
                                        new_msgX[y + 1] = msgs[t++];                                        
                                    }
                                }
                                // Send the message
                                nResult = WriteFrame(new_msgX);                             
                                Thread.Sleep(P2_ServerTime);
                            }
                            if(nEndBytes>0) //tail block(less than 7 bytes)
                            {
                                if (m_n36SvrPackNum > 0x2F)
                                    m_n36SvrPackNum = 0x20;

                                new_msgX = new byte[BlockSize + 1];
                                for (int u = 0; u < new_msgX.Length; u++)
                                    new_msgX[u] = 0xFF;

                                if (svrID != 0x36)
                                    new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                                else
                                    new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);

                                int p = 0;
                                k = t;
                                for (; p< nEndBytes; p++)
                                {
                                    if(k<msgs.Length)
                                        new_msgX[p + 1] = msgs[k++];
                                }

                                // Send the message
                                nResult = WriteFrame(new_msgX);
                                Thread.Sleep(P2_ServerTime);
                            }
                        #endregion
                        
                        }

                        #region  old way (process last package not equal 0x80,will apear issue usually)
                        //int nEndBytes = 0;
                        //nAfter1stFrameSent = nDatalen - k;
                        //if (nAfter1stFrameSent > BlockSize)
                        //    nEndBytes = nAfter1stFrameSent % BlockSize;
                        //else
                        //    nEndBytes = 0;

                        //if (bReConter36)
                        //{
                        //    m_n36SvrPackNum = 0x21;
                        //    bReConter36 = false;
                        //}

                        //int nSingleFrmOfOneBlockSent = -1;
                        //for (x = k; x < nDatalen; x += BlockSize)
                        //{
                        //    if (m_n36SvrPackNum > 0x2F)
                        //        m_n36SvrPackNum = 0x20;

                        //    new_msgX = new byte[BlockSize + 1];
                        //    for (int u = 0; u < new_msgX.Length; u++)
                        //        new_msgX[u] = 0xFF;

                        //    if (svrID != 0x36)
                        //        new_msgX[0] = Convert.ToByte(n1stByteTimes++);
                        //    else
                        //    {
                        //        if (m_n36SvrPackNum == 0)
                        //            m_n36SvrPackNum = 0x21;
                        //        new_msgX[0] = Convert.ToByte(m_n36SvrPackNum++);
                        //    }

                        //    for (y = 0; y < BlockSize; y++)
                        //    {
                        //        if ((x + y) < msgs.Length)
                        //            new_msgX[y + 1] = msgs[x + y];
                        //    }

                        //    if (nEndBytes == 1)
                        //    {
                        //        byte byte_end = msgs[msgs.Length - 2];
                        //        new_msgX[y] = byte_end;
                        //    }

                        //    // Send the message
                        //    nSingleFrmOfOneBlockSent = Write_Message(new_msgX);
                        //    nResult = nSingleFrmOfOneBlockSent;

                        //    Console.Write("0x36 svr block{0:d},result{1:d}", x, nSingleFrmOfOneBlockSent);
                        //    //waitting for incoming message
                        //    Thread.Sleep(10);
                        //}

                        ////last bytes of message block
                        //int t = 0;
                        //byte[] EndBytes = new byte[] { };
                        //if (nEndBytes ==1)
                        //{
                        //    byte EndByte1 = msgs[msgs.Length - 1];
                        //    EndBytes = new byte[msgs.Length - x + 1];

                        //    if (svrID != 0x36)
                        //        EndBytes[t++] = Convert.ToByte(n1stByteTimes);
                        //    else
                        //        EndBytes[t++] = Convert.ToByte(m_n36SvrPackNum);

                        //    EndBytes[t] = EndByte1;

                        //    // Send the message
                        //    Write_Message(EndBytes);
                        //    nEndBytes = 0;
                        //}

                        #endregion

#endif
                    }
                }
            }
            catch(Exception ex)
            {
                Invoke(new MethodInvoker(delegate() { IncludeTextMessage(string.Format("Issue occured when send message{0}", ex.Message)); }));
            }

            return nResult;
        }

        /// <summary>
        /// Write DID message to CAN bus
        /// </summary>
        /// <param name="msgs"></param>
        ///<param name="bSingleFrame">does frame data single or not?</param>
        ///<param name="bFollowCtrl">does send follow ctr frame</param>
        /// <returns></returns>
        public int Write_DID_CANMessage(byte[] msgs, bool bSingleFrame = false, bool bFollowCtrl = false)
        {
            int nResult = -1;
            int nDatalen = 0;
            int loBitDataLen = 0;
            int hiBitDataLen = 0;
            int k = 0;  //Byte position in mutiple frame bytes array
            byte svrID;
            byte[] new_msg0 = new byte[2];
            byte[] new_msg1 = new byte[7];
            byte[] new_msg = new byte[8];
            try
            {
                if (bSingleFrame)
                {
                    int nLen = msgs.Length;
                    byte[] byte0 = new byte[] { Convert.ToByte(nLen) };
                    if (!bFollowCtrl)
                        new_msg = Combine(byte0, msgs);
                    else
                        new_msg = msgs;

                    // Send the message
                    Write_Message(new_msg);
                    Thread.Sleep(P2_ServerTime);
                }
                else
                {
                    //1st frame
                    {
                        nDatalen = msgs.Length;
                        svrID = msgs[0];

                        loBitDataLen = ((nDatalen >> 8) & 0x0F);
                        new_msg0[0] = Convert.ToByte(loBitDataLen + 0x10);
                        
                        hiBitDataLen = (nDatalen & 0x00FF);
                        new_msg0[1] = Convert.ToByte(hiBitDataLen);
                        //copy 1st frame residue bytes(except 1st frame mark and frame length 2 byte)
                        for (k = 0; k < (new_msg.Length - new_msg0.Length); k++)
                            new_msg1[k] = msgs[k];
                        new_msg = Combine(new_msg0, new_msg1);

                        // Send the message
                        Write_Message(new_msg);
                        Thread.Sleep(P2_ServerTime * 3);
                    }

                    //backword frame
                    {
                        byte BlockSize = 7; //single frame 1st byte is sequence number,so backfoward data len is 7
                        byte[] new_msgX = new byte[] { };

                        int n1stByteTimes = 0x21; //first frame sequence number
                        int nAfter1stFrameSent = 0;
                        int y = 0;

                        int nEndBytes = 0;
                        nAfter1stFrameSent = msgs.Length - k;
                        if (nAfter1stFrameSent > BlockSize)
                            nEndBytes = nAfter1stFrameSent % BlockSize;
                        else
                            nEndBytes = nAfter1stFrameSent; //resedue bytes less than 7 bytes

                        int c = k;
                        int n7ByteGroups = nAfter1stFrameSent / BlockSize;
                        for (int t = 0; t < n7ByteGroups; t++)
                        {
                            new_msgX = new byte[BlockSize + 1];
                            for (int u = 0; u < new_msgX.Length; u++)
                                new_msgX[u] = 0xFF;
                          
                            new_msgX[0] = Convert.ToByte(n1stByteTimes++);

                            for (y = 0; y < BlockSize; y++)
                            {
                                if (c < msgs.Length)
                                {
                                    new_msgX[y + 1] = msgs[c++];
                                }
                            }

                            // Send the message
                            nResult = WriteFrame(new_msgX);
                            Thread.Sleep(P2_ServerTime);
                        }
                        if (nEndBytes > 0) //tail block(less than 7 bytes)
                        {
                            new_msgX = new byte[BlockSize + 1];
                            for (int u = 0; u < new_msgX.Length; u++)
                                new_msgX[u] = 0xFF;

                            new_msgX[0] = Convert.ToByte(n1stByteTimes++);

                            k = c;
                            for (int p = 0; p < nEndBytes; p++)
                            {
                                if (k < msgs.Length)
                                    new_msgX[p + 1] = msgs[k++];
                            }

                            // Send the message
                            nResult = WriteFrame(new_msgX);
                            Thread.Sleep(P2_ServerTime);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                IncludeTextMessage(string.Format("Issue occured when send message{0}", ex.Message));
            }

            return nResult;
        }

        ///<summary>
        ///Write message to CAN/LIN bus
        ///<param name="msgs"/> diag request message</param>
        ///</summary>
        public int Write_Message(byte[] msgs, uint ID=0x00)
        {
            int nResult;

            // Send the message
            //
            nResult =  WriteFrame(msgs);

            // The message was successfully sent
            //
            if (nResult == 0)
            {
                string strSendMsg = string.Empty;
                for (int i = 0; i < msgs.Length; i++)
                {
                    if ((msgs[i] & 0xF0) == 0x00)
                        strSendMsg += " 0x" + string.Format("0{0:X}", msgs[i]);
                    else
                        strSendMsg += " 0x" + string.Format("{0:X}", msgs[i]);
                }
                
                Invoke(new MethodInvoker(delegate () { IncludeTextMessage("SENT Message:" + strSendMsg); }));
                return nResult;
            }
            // An error occurred.  We show the error.
            //			
            else
                MessageBox.Show("Message send failured.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
            return nResult;
        }

        /// <summary>
        /// Help Function used to get an error as text
        /// </summary>
        /// <param name="error">Error code to be translated</param>
        /// <returns>A text with the translated error</returns>
        private string GetFormatedError(TPCANStatus error)
        {
            StringBuilder strTemp;

            // Creates a buffer big enough for a error-text
            //
            strTemp = new StringBuilder(256);
            // Gets the text using the GetErrorText API function
            // If the function success, the translated error is returned. If it fails,
            // a text describing the current error is returned.
            //
            if (PCANBasic.GetErrorText(error, 0x09, strTemp) != TPCANStatus.PCAN_ERROR_OK)
                return string.Format("An error occurred. Error-code's text ({0:X}) couldn't be retrieved", error);
            else
                return strTemp.ToString();
        }

        /// <summary>
        /// Get PCAN hardware infomation
        /// </summary>
        private void GetPCANVersion()
        {
            TPCANStatus stsResult;
            StringBuilder strTemp;
            string[] strArrayVersion;

            strTemp = new StringBuilder(256);

            // We get the vesion of the PCAN-Basic API
            //
            stsResult = PCANBasic.GetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_API_VERSION, strTemp, 256);
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
            {
                IncludeTextMessage("API Version: " + strTemp.ToString());

                // We get the version of the firmware on the device
                //
                stsResult = PCANBasic.GetValue(m_canBus.PCANHANDLE, TPCANParameter.PCAN_FIRMWARE_VERSION, strTemp, 256);
                if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                    IncludeTextMessage("Firmare Version: " + strTemp.ToString());

                // We get the driver version of the channel being used
                //
                stsResult = PCANBasic.GetValue(m_canBus.PCANHANDLE, TPCANParameter.PCAN_CHANNEL_VERSION, strTemp, 256);
                if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                {
                    // Because this information contains line control characters (several lines)
                    // we split this also in several entries in the Information List-Box
                    //
                    strArrayVersion = strTemp.ToString().Split(new char[] { '\n' });
                    IncludeTextMessage("Channel/Driver Version: ");
                    for (int i = 0; i < strArrayVersion.Length; i++)
                        IncludeTextMessage("     * " + strArrayVersion[i]);
                }
            }

            // If an error ccurred, a message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                MessageBox.Show(GetFormatedError(stsResult));
        }

        /// <summary>
        /// CAN message filter(Only receive custom range message)
        /// </summary>
        private void MessageFilter(bool bFillter)
        {
            ////message filter
            string strIfErr = string.Empty;
            uint uToID = Convert.ToUInt32(nudIdFrom.Value);
            uint uFromID = Convert.ToUInt32(nudIdTo.Value);
            bool bExtendedFrm = (nudIdTo.Value <= 0x7FF && nudIdFrom.Value <= 0x7FF) ? false : true;

            if (m_bus == null)
                return;

            TPCANStatus stsResult = ((CAN_Bus)m_bus).MessageFillter(uToID, uFromID,  out strIfErr, bExtendedFrm,  bFillter);
            if (stsResult == TPCANStatus.PCAN_ERROR_OK && bFillter)
            {
                IncludeTextMessage(string.Format("The filter was customized. IDs from {0:X} to {1:X}", nudIdTo.Text, nudIdFrom.Text));
            }
            else
            {
                if (strIfErr != string.Empty)
                    IncludeTextMessage(strIfErr);
            }
        }

        /// <summary>
        /// Init message class
        /// </summary>
        private bool Init_Msg()
        {
            m_bus = new Bus();
            string strmsg = string.Empty;

            ushort BaudRate = 1;
            switch (cbbBaudrates.SelectedIndex)
            {
                case 0:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_1M;
                    break;
                case 1:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_500K;
                    break;
                case 2:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_250K;
                    break;
                case 3:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_125K;
                    break;
                case 4:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_100K;
                    break;
                case 5:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_20K;
                    break;
                case 6:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_10K;
                    break;
                case 7:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_5K;
                    break;

                default:
                    BaudRate = (ushort)TPCANBaudrate.PCAN_BAUD_500K;
                    break;
            }

            m_bus = Bus.Initialize(ref strmsg, BaudRate);
            if (m_bus == null)
                return false;

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                TPCANStatus stsResult = TPCANStatus.PCAN_ERROR_ANYBUSERR;
                m_lstCANMsg = new List<CANMsgs>(); // move to under,when P-CAN adapter connected then init CANMsgs list     

                m_canBus = (CAN_Bus)m_bus;
                if (m_canBus == null)
                {
                    IncludeTextMessage("Please connect can bus adapter first!");
                    return false;
                }
                else
                {
                    if (strmsg != "PCAN_ERROR_OK")
                        MessageBox.Show(GetFormatedError(stsResult));
                    else
                    // Prepares the PCAN-Basic's PCAN-Trace file
                    //
                    {
                        //CAN download need P-CAN set AutoResetEvent for read incoming message from ECU
                        RecieveMsg_TH();//start receive thread on CAN bus
                        ConfigureTraceFile();
                        return true;
                    }
                }
            }

            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                m_linBus = (LIN_Bus)m_bus;
                if(m_linBus == null)
                {
                    IncludeTextMessage("Please connect lin bus adapter first!");
                    return false;
                }
                else
                {
                    m_lstLINMsg = new List<LINMsg>();
                    return true;
                }                    
            }
            return false;
        }

        ///<summary>
        ///Write CAN/LIN standard frame to BUS
        /// </summary>
        public int WriteFrame(byte[] msgs, uint ID = 0x00)
        {
            int iLength;
            int nTxIDLen;
            int nRet = -1;
            iLength = msgs.Length;

            if(m_bus.BusType == Bus.Type.LIN_BUS)
            {
                // We create a LINMsg message structure 
                //
                LINMsg linMsg = new LINMsg();
                // We get so much data as the Len of the message
                //
                 iLength = GetLengthFromDLC(msgs.Length, false);

                if (nudIdTo.Text.Length>2 || nudIdFrom.Text.Length>2)
                {
                    MessageBox.Show("LIN message id not correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }

                // We configurate the Message.  The ID,
                // Length of the Data, and the data
                //
                linMsg.data = new byte[iLength]; //msgs.Length < 8 ? new byte[8] : new byte[iLength];
                linMsg.lin_uds_addr.ReqID = Convert.ToByte(nudIdTo.Text, 16);
                linMsg.lin_uds_addr.ResID = Convert.ToByte(nudIdFrom.Text, 16);
                linMsg.DLC = iLength; // Convert.ToByte(iLength); //msgs.Length < 8 ? Convert.ToByte(8) : Convert.ToByte(iLength);
                linMsg.ID = linMsg.lin_uds_addr.ReqID;
                linMsg.Dir = "Tx";

                // The message is sent to the configured hardware
                //
                linMsg.WaitTime = 100;
                linMsg.lin_uds_addr.NAD = Convert.ToByte(numUpDownNAD.Text, 16);   //0x77; //0x65; //0x42;
                linMsg.lin_uds_addr.CheckType = 0; //0:standard verify  1:enhance verify
                if(PRODUCT_TYPE == PRJTYPE._7Kw)
                    linMsg.lin_uds_addr.STmin = 5;
                else if(PRODUCT_TYPE == PRJTYPE._LINHex)
                    linMsg.lin_uds_addr.STmin = 10;

                for (int i = 0; i < iLength; i++)
                {
                    linMsg.data[i] = msgs[i]; //i < msgs.Length ? msgs[i] : Convert.ToByte(0xFF);
                }

                object LINMSG = (object)linMsg;
                nRet = m_linBus.SendMessage(LINMSG);
                if (nRet == LIN_UDS.LIN_UDS_OK)
                {
                    //add request message into message display list(m_LastsMsgList)
                    //
                    GetMsgTimeStamp(ref T_timestamp);
                    this.Invoke(new MethodInvoker(delegate (){ProcessMessage(linMsg, T_timestamp);}));
                }
            }

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                // We create a LINMsg message structure 
                //
                CANMsgs canMsg = new CANMsgs();
                // We get so much data as the Len of the message
                // //request message length define to 8, even if real length less than 8(ex. 0x10 02), will fill residue bytes with 0x00.
                iLength = 8; // GetLengthFromDLC(msgs.Length, true);
                // We configurate the Message.  The ID,
                // Length of the Data, Message Type, and the Data
                //
                nTxIDLen = nudIdTo.Text.Length;
                canMsg.Dir = "Tx";
                canMsg.CANMsg.DATA = new byte[8];
                if (ID == 0x00)
                {
                    canMsg.ID = Convert.ToUInt32(nudIdTo.Text, 16);
                    canMsg.CANMsg.ID = Convert.ToUInt32(nudIdTo.Text, 16);
                }
                else
                {
                    canMsg.ID = ID;
                    canMsg.CANMsg.ID = ID;
                }
                canMsg.CANMsg.LEN = Convert.ToByte(iLength);
                canMsg.CANMsg.MSGTYPE = (nTxIDLen<=3) ? TPCANMessageType.PCAN_MESSAGE_STANDARD : TPCANMessageType.PCAN_MESSAGE_EXTENDED;

                for (int i = 0; i < iLength; i++)
                {
                    canMsg.CANMsg.DATA[i] = i < msgs.Length ? msgs[i] : Convert.ToByte(0x00);
                }

                object CANMSG = (object)canMsg;
                nRet = m_canBus.SendMessage(CANMSG);

                if(nRet == (int)TPCANStatus.PCAN_ERROR_OK)
                {
                    //add request message into message display list(m_LastsMsgList)
                    //
                    GetMsgTimeStamp(ref T_timestamp);

                    if(PRODUCT_TYPE == PRJTYPE._CANUDS40 || PRODUCT_TYPE == PRJTYPE._CANUDS01)
                        ProcessMessage(canMsg, T_timestamp);
                    else
                        this.Invoke(new MethodInvoker(delegate () { ProcessMessage(canMsg, T_timestamp); }));
                }
            }
            return nRet;
        }

        /// <summary>
        /// record message time stamp
        /// </summary>
        /// <param name="strTimeStamp">message time stamp string</param>
        public void GetMsgTimeStamp(ref String strTimeStamp)
        {
            long ticks_ms;
            if (m_bRelativeTime)
            {
                if (FristEnterRT_ticks > 0)
                    ticks_ms = (DateTime.Now.Ticks - FristEnterRT_ticks) / 10000;
                else
                {
                    FristEnterRT_ticks = DateTime.Now.Ticks;
                    ticks_ms = 0;
                }
                strTimeStamp = Time_Frame_Conversion(ticks_ms);
            }
            else
            {
                strTimeStamp = Time_Frame_Conversing();
            }
        }

        /// <summary>
        /// Inserts a new entry for a new message in the Message-ListView
        /// </summary>
        /// <param name="newMsg">The messasge to be inserted</param>
        /// <param name="timeStamp">The Timesamp of the new message</param>
        private void InsertMsgEntry(object Msg, String timeStamp)
        {
            MessageStatus msgStsCurrentMsg;
            lock (m_LastMsgsList.SyncRoot)
            {
                if (m_bus.BusType == Bus.Type.LIN_BUS)
                {
                    LINMsg newMsg = (LINMsg)Msg;
                    // We add this status in the last message list
                    //
                    msgStsCurrentMsg = new MessageStatus(newMsg, timeStamp, m_LastMsgsList.Count+1);
                    msgStsCurrentMsg.ShowingPeriod = chbShowPeriod.Checked;
                    m_LastMsgsList.Add(msgStsCurrentMsg);
                    m_MsgCount++;
                    //delete 0 position message when size of message list greater than 12000
                    if (!m_WholeTrace)
                    {
                        if (m_lstLINMsg.Count >= RT_MaxNumber - 1)
                        {
                            m_lstLINMsg.RemoveAt(0);
                            m_LastMsgsList.RemoveAt(0);
                        }
                    }

                    m_lstLINMsg.Add(new LINMsg(msgStsCurrentMsg.IdString, newMsg.Dir, GetLengthFromDLC(newMsg.DLC, false).ToString(), m_MsgCount.ToString()/*m_LastMsgsList.Count.ToString()*/, msgStsCurrentMsg.TimeString, msgStsCurrentMsg.DataString));

                }

                if (m_bus.BusType == Bus.Type.CAN_BUS)
                {
                    CANMsgs canMsg = (CANMsgs)Msg;
                    // We add this status in the last message list
                    //
                    msgStsCurrentMsg = new MessageStatus(canMsg, timeStamp, m_LastMsgsList.Count+1);
                    msgStsCurrentMsg.ShowingPeriod = chbShowPeriod.Checked;
                    m_LastMsgsList.Add(msgStsCurrentMsg);
                    m_MsgCount++;
                    //delete 0 position message when size of message list greater than RT_MaxNumber
                    if (!m_WholeTrace)
                    {
                        if (m_lstCANMsg.Count >= RT_MaxNumber-1)
                        {
                            m_lstCANMsg.RemoveAt(0);
                            m_LastMsgsList.RemoveAt(0);
                        }
                    }

                    m_lstCANMsg.Add(new CANMsgs(msgStsCurrentMsg.IdString, canMsg.Dir, GetLengthFromDLC(canMsg.CANMsg.LEN, false).ToString(), m_MsgCount.ToString(), msgStsCurrentMsg.TimeString, msgStsCurrentMsg.DataString)); ;
                }
            }
        }

        /// <summary>
        /// Processes a received message, in order to show it in the Message-ListView
        /// </summary>
        /// <param name="theMsg">The received PCAN-Basic message</param>
        /// <returns>True if the message must be created, false if it must be modified</returns>
        private void ProcessMessage(object Msg, String itsTimeStamp)
        {
            // We search if a message (Same ID and Type) is 
            // already received or if this is a new message
            //
            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                InsertMsgEntry(Msg, itsTimeStamp);
            }

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                // Message not found. It will created
                //
                InsertMsgEntry(Msg, itsTimeStamp);
            }           
        }

        /// <summary>
        /// relative time
        /// </summary>
        /// <param name="ms">Startup time difference string</param>
        /// <returns></returns>
        private String Time_Frame_Conversion(long ms)
        {
            string conversion_value;
            conversion_value = (ms / 1000).ToString().PadLeft(4, '0') + ":" + (ms % 1000).ToString().PadLeft(3, '0');

            return conversion_value;
        }

        /// <summary>
        /// absolute time
        /// </summary>
        /// <returns></returns>
        private String Time_Frame_Conversing()
        {
            string strFullTime = DateTime.Now.TimeOfDay.ToString();
            string[] strArr = strFullTime.Split('.');
            string strTime = strArr[0];
            string strTick = strArr[1];
            strFullTime = strTime + ":" + strTick.Substring(0, 3);

            return strFullTime;
        }

        /// <summary>
        /// Make CAN message receive thread
        /// </summary>
        private void RecieveMsg_TH()
        {
            // Create and start the tread to read CAN Message using SetRcvEvent()
            //
            if (m_DisplayAppMsg)
            {                
                m_ReadThread = new Thread(() => CANReadThreadFunc());
                m_ReadThread.IsBackground = true;
                m_ReadThread.Start();
            }
            else
            {
                ThreadStart threadDelegate = new ThreadStart(CANReadThreadFunc);
                m_ReadThread = new Thread(threadDelegate);
                m_ReadThread.IsBackground = true;
                m_ReadThread.Start();
            }
        }

        /// <summary>
        /// Thread-Function used for reading PCAN-Basic messages
        /// </summary>
        private void CANReadThreadFunc()
        {
            int nWaitTime = 0;
            byte[] resp = new byte[8];
            // While flash action on
            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                UInt32 iBuffer;
                TPCANStatus stsResult;

                iBuffer = Convert.ToUInt32(m_ReceiveEvent.SafeWaitHandle.DangerousGetHandle().ToInt32());
                // Sets the handle of the Receive-Event.
                //
                stsResult = PCANBasic.SetValue(m_canBus.PCANHANDLE, TPCANParameter.PCAN_RECEIVE_EVENT, ref iBuffer, sizeof(UInt32));

                if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                {
                    MessageBox.Show(GetFormatedError(stsResult), "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                // Waiting for Receive-Event
                //                
                while (true)
                {
                    if (m_bAppAddr_Enable)
                        nWaitTime = 50;
                    else
                        nWaitTime = 10;
                    if (m_ReceiveEvent.WaitOne(nWaitTime) && !m_bReadWriteDID)
                    {
                        // Process Receive-Event using .NET Invoke function
                        // in order to interact with Winforms UI (calling the 
                        // function ReadMessages)
                        // 
                        //if (m_DisplayAppMsg)
                            ReadMessages(ref resp);
                        //else
                        //    this.Invoke(m_ReadDelegate);
                    }
                }
            }
        }

        /// <summary>
        /// Function for reading CAN/LIN messages on bus adapter device(for thread create way1)
        /// </summary>
        /// <returns>A TPCANStatus error code</returns>
        public void ReadMessage()
        {
            Int32 nResult = -1;
            int iLength = 8;
            uint uID;
            byte[] respMsg = new byte[8];
            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                // We create a LINMsg message structure 
                //
                LINMsg linMsg = new LINMsg();

                // We execute the "Read" function of the USBXXX
                //
                linMsg.data = new byte[iLength];
                linMsg.lin_uds_addr.ReqID = Convert.ToByte(nudIdTo.Text, 16);
                linMsg.lin_uds_addr.ResID = Convert.ToByte(nudIdFrom.Text, 16);
                
                linMsg.ID = linMsg.lin_uds_addr.ResID;
                linMsg.Dir = "Rx";

                object LINMSG = (object)linMsg;
                nResult = m_linBus.ReceiveMessage(out LINMSG);
                
                if (nResult > 0 && LINMSG!=null)
                {
                    respMsg = ((LINMsg)LINMSG).lin_ex_msg.Data; 
                    linMsg.data = ((LINMsg)LINMSG).lin_ex_msg.Data;
                    if (linMsg.data!=null)
                        linMsg.DLC = Convert.ToByte(linMsg.data.Length);
                    else
                        linMsg.DLC = Convert.ToByte(iLength);
                    uID = ((LINMsg)LINMSG).lin_uds_addr.ResID;

                    if (m_DisplayAppMsg)
                    { 
                        GetMsgTimeStamp(ref T_timestamp);
                        this.Invoke(new MethodInvoker(delegate (){ ProcessMessage(linMsg, T_timestamp); }));
                        m_lin_msg.data = respMsg;
                        m_RespMsg = respMsg;

                        //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$####**********// !!!
                        //tell read dtc thread DTC has be found.
                        //if (uID == (uint)nudIdFrom.Value)
                        //    m_ReadDTCEvent.Set();
                    }
                    else
                    {
                        //collectting setting Rsps(h) IDs' message only
                        if (uID == Convert.ToUInt32(nudIdFrom.Value))
                        {
                            GetMsgTimeStamp(ref T_timestamp);
                            this.Invoke(new MethodInvoker(delegate () { ProcessMessage(linMsg, T_timestamp); }));
                            m_lin_msg.data = respMsg;
                            m_RespMsg = respMsg;

                            //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$###**********// !!!
                            //tell read dtc thread DTC has be found.
                            //m_ReadDTCEvent.Set();
                        }
                    }
                }
            }

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                CANMsgs canMsg = new CANMsgs();
                canMsg.Dir = "Rx";

                object CANMSG;
                nResult = m_canBus.ReceiveMessage(out CANMSG);
                
                if ((nResult != (int)TPCANStatus.PCAN_ERROR_QRCVEMPTY) || nResult == (int)TPCANStatus.PCAN_ERROR_OK)
                {
                    respMsg = ((CANMsgs)CANMSG).CANMsg.DATA;
                    canMsg.ID = ((CANMsgs)CANMSG).CANMsg.ID;
                    canMsg.CANMsg = ((CANMsgs)CANMSG).CANMsg;
                    m_can_msg.CANMsg = ((CANMsgs)CANMSG).CANMsg;

                    if (m_DisplayAppMsg)
                    {
                        m_RespMsg = respMsg;

                        //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$####**********// !!!
                        //tell read dtc thread DTC has be found.
                        if (canMsg.ID == Convert.ToUInt32(nudIdFrom.Value))
                            m_ReadDTCEvent.Set();

                        GetMsgTimeStamp(ref T_timestamp);
                        this.Invoke(new MethodInvoker(delegate () { ProcessMessage(canMsg, T_timestamp); }));
                    }
                    else
                    {
                        //collectting setting Rsps(h) IDs' message only
                        if (canMsg.ID == Convert.ToUInt32(nudIdFrom.Value))
                        {
                            uID = canMsg.ID;
                            respMsg = ((CANMsgs)CANMSG).CANMsg.DATA;
                            m_RespMsg = respMsg;

                            GetMsgTimeStamp(ref T_timestamp);

                            if (PRODUCT_TYPE == PRJTYPE._CANUDS40 || PRODUCT_TYPE == PRJTYPE._CANUDS01)
                                ProcessMessage(canMsg, T_timestamp);
                            else
                                this.Invoke(new MethodInvoker(delegate () { ProcessMessage(canMsg, T_timestamp); }));

                            //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$####**********// !!!
                            //tell read dtc thread DTC has be found.And this can used on use ManualResetEvent to process response messages' flash project(CAN UDS)
                            m_ReadDTCEvent.Set();
                        }
                    }
                }
                else
                {
                    respMsg = new byte[8]; //flash buffer wether not receive message.
                    m_RespMsg = respMsg;
                }
            }
        }

        /// <summary>
        /// overload Function for reading CAN/LIN messages on bus adapter device(for thread create way2)
        /// </summary>
        /// <returns>A TPCANStatus error code</returns>
        public uint ReadMessage(ref byte[] respMsg)
        {
            Int32 nResult = -1;
            int iLength = 8;
            uint uID;

            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                // We create a LINMsg message structure 
                //
                LINMsg linMsg = new LINMsg();

                // We execute the "Read" function of the USBXXX
                //
                linMsg.data = new byte[iLength];
                linMsg.lin_uds_addr.ReqID = Convert.ToByte(nudIdTo.Text, 16);
                linMsg.lin_uds_addr.ResID = Convert.ToByte(nudIdFrom.Text, 16);

                linMsg.ID = linMsg.lin_uds_addr.ResID;
                linMsg.Dir = "Rx";

                object LINMSG = (object)linMsg;
                nResult = m_linBus.ReceiveMessage(out LINMSG);

                if (nResult > 0 && LINMSG != null)
                {
                    respMsg = ((LINMsg)LINMSG).lin_ex_msg.Data;
                    linMsg.data = ((LINMsg)LINMSG).lin_ex_msg.Data;
                    if (linMsg.data != null)
                        linMsg.DLC = Convert.ToByte(linMsg.data.Length);
                    else
                        linMsg.DLC = Convert.ToByte(iLength);
                    uID = ((LINMsg)LINMSG).lin_uds_addr.ResID;

                    if (m_DisplayAppMsg)
                    {
                        GetMsgTimeStamp(ref T_timestamp);
                        this.Invoke(new MethodInvoker(delegate () { ProcessMessage(linMsg, T_timestamp); }));
                        m_lin_msg.data = respMsg;
                        m_RespMsg = respMsg;
                    }
                    else
                    {
                        //collectting setting Rsps(h) IDs' message only
                        if (uID == (uint)nudIdFrom.Value)
                        {
                            GetMsgTimeStamp(ref T_timestamp);
                            this.Invoke(new MethodInvoker(delegate () { ProcessMessage(linMsg, T_timestamp); }));
                            m_lin_msg.data = respMsg;
                            m_RespMsg = respMsg;
                        }
                    }

                    return (uint)nResult;
                }

                #region LIN_UDS_GetMsgFromUDSBuffer
                // We process the received message
                //
                //string strmsg = string.Empty;
                //IntPtr pt = IntPtr.Zero;
                //try
                //{
                //    pt = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(USB2LIN_EX.LIN_EX_MSG)) * 50);
                //    nResult = LIN_UDS.LIN_UDS_GetMsgFromUDSBuffer(m_DevHandle, m_LINIndex, pt, m_lin_msg.WaitTime);
                //    if (nResult > 0)
                //    {
                //        USB2LIN_EX.LIN_EX_MSG[] lin_msg = new USB2LIN_EX.LIN_EX_MSG[nResult];
                //        for (int i = 0; i < nResult; i++)
                //        {
                //            lin_msg[i] = (USB2LIN_EX.LIN_EX_MSG)Marshal.PtrToStructure((IntPtr)((UInt32)pt + i * Marshal.SizeOf(typeof(USB2LIN_EX.LIN_EX_MSG))), typeof(USB2LIN_EX.LIN_EX_MSG));
                //            respMsg = lin_msg[i].Data;
                //            linMsg.data = lin_msg[i].Data;
                //            //lstMessages rolling newest message position
                //            //
                //            GetMsgTimeStamp(ref T_timestamp);
                //            ProcessMessage(linMsg, T_timestamp);
                //            MessageView_RollingNewestPos(lstMessages.Items.Count);
                //            IncludeTextMessage(String.Format("[%d]%s SYNC[%02X] PID[%02X] ", i, m_MSGTypeStr[lin_msg[i].MsgType], lin_msg[i].Sync, lin_msg[i].PID));
                //            for (int j = 0; j < lin_msg[i].DataLen; j++)
                //            {
                //                strmsg += String.Format("%02X ", lin_msg[i].Data[j]) + " ";
                //                m_lin_msg.data[j] = lin_msg[i].Data[j];
                //            }
                //            IncludeTextMessage("RespMsg: " + strmsg);
                //            IncludeTextMessage((String.Format("[%s][%02X] [%02d:%02d:%02d.%03d]\n", m_CKTypeStr[lin_msg[i].CheckType], lin_msg[i].Check, (lin_msg[i].Timestamp / 3600000) % 60, (lin_msg[i].Timestamp / 60000) % 60, (lin_msg[i].Timestamp / 1000) % 60, (lin_msg[i].Timestamp) % 1000)));
                //        }
                //    }
                //}
                //finally
                //{
                //    Marshal.FreeHGlobal(pt);
                //}

                #endregion
            }

            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                CANMsgs canMsg = new CANMsgs();
                canMsg.Dir = "Rx";

                object CANMSG;
                nResult = m_canBus.ReceiveMessage(out CANMSG);

                if ((nResult != (int)TPCANStatus.PCAN_ERROR_QRCVEMPTY) || nResult == (int)TPCANStatus.PCAN_ERROR_OK)
                {
                    respMsg = ((CANMsgs)CANMSG).CANMsg.DATA;
                    canMsg.ID = ((CANMsgs)CANMSG).CANMsg.ID;
                    canMsg.CANMsg = ((CANMsgs)CANMSG).CANMsg;
                    m_can_msg.CANMsg = ((CANMsgs)CANMSG).CANMsg;

                    if (m_DisplayAppMsg)
                    {
                        m_RespMsg = respMsg;
                        //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$####**********// !!!
                        //tell read dtc thread DTC has be found.
                        if (canMsg.ID == Convert.ToUInt32(nudIdFrom.Value))
                        {
                            m_ReadDTCEvent.Set();
                        }

                        GetMsgTimeStamp(ref T_timestamp);
                        this.Invoke(new MethodInvoker(delegate () { ProcessMessage(canMsg, T_timestamp); }));
                    }
                    else
                    {
                        //collectting setting Rsps(h) IDs' message only
                        if (canMsg.ID == Convert.ToUInt32(nudIdFrom.Value))
                        {
                            uID = canMsg.ID;
                            respMsg = ((CANMsgs)CANMSG).CANMsg.DATA;
                            m_RespMsg = respMsg;

                            GetMsgTimeStamp(ref T_timestamp);

                            if (PRODUCT_TYPE == PRJTYPE._CANUDS40 || PRODUCT_TYPE == PRJTYPE._CANUDS01)
                                ProcessMessage(canMsg, T_timestamp);
                            else
                                this.Invoke(new MethodInvoker(delegate () { ProcessMessage(canMsg, T_timestamp); }));

                            //**********####$$$$$IMPORTANT(INDISPENSABLE)$$$$$####**********// !!!
                            //tell read dtc thread DTC has be found.And this can used on use ManualResetEvent to process response messages' flash project(CAN UDS)
                            m_ReadDTCEvent.Set();
                        }
                        else
                        {
                            //flash buffer wether not receive message.
                            Buffer.BlockCopy(new byte[8], 0, m_RespMsg, 0, 8 * sizeof(byte));
                        }
                    }
                }
                else
                {
                    //flash buffer wether not receive message.
                    Buffer.BlockCopy(new byte[8], 0, m_RespMsg, 0, 8 * sizeof(byte));
                }
            }

            return  (uint)nResult;
        }

        /// <summary>
        /// byte[]转换为 struct
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public object BytesToStruct(byte[] bytes, Type type)
        {
            int size = Marshal.SizeOf(type);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return Marshal.PtrToStructure(buffer, type);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// struct转换为byte[]
        /// </summary>
        /// <param name="structObj"></param>
        /// <returns></returns>
        public byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structObj, buffer, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        /// <summary>
        /// Function for reading PCAN-Basic messages
        /// </summary>
        public void ReadMessages()
        {
            // We read at least one time the queue looking for messages.
            // If a message is found, we look again trying to find more.
            // If the queue is empty or an error occurr, we get out from
            // the dowhile statement.
            //	
            _rw.EnterReadLock();
            ReadMessage();
            _rw.ExitReadLock();
        }

        public uint ReadMessages(ref byte[] respMsg)
        {
            uint uID = 0x0F;
            // We read at least one time the queue looking for messages.
            // If a message is found, we look again trying to find more.
            // If the queue is empty or an error occurr, we get out from
            // the dowhile statement.
            //	
            _rw.EnterReadLock();
            uID = ReadMessage(ref respMsg);
            _rw.ExitReadLock();

            return uID;
        }

        private void cbbChannel_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chbCanFD_CheckedChanged(object sender, EventArgs e)
        {
            m_IsFD = chbCanFD.Checked;

            cbbBaudrates.Visible = !m_IsFD;
            cbbHwType.Visible = !m_IsFD;
            cbbInterrupt.Visible = !m_IsFD;
            cbbIO.Visible = !m_IsFD;
            laBaudrate.Visible = !m_IsFD;
            laHwType.Visible = !m_IsFD;
            laIOPort.Visible = !m_IsFD;
            laInterrupt.Visible = !m_IsFD;

            txtBitrate.Visible = m_IsFD;
            laBitrate.Visible = m_IsFD;
            //chbFD.Visible = m_IsFD;
            //chbBRS.Visible = m_IsFD;

            //if ((nudLength.Maximum > 8) && !m_IsFD)
            //    chbFD.Checked = false;
        }

        private void Init_LIN()
        {
            // Sets the connection status of the main-form
            //
            bool bResult = false;
            String strmsg;
            int nInit = 0;

            try
            {
                m_LINDevNum = USB_DEVICE.USB_ScanDevice(m_DevHandles); //扫描查找设备
                if (m_LINDevNum <= 0)
                {
                    strmsg = "No device connected!";
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                    return;
                }
                else
                {
                    strmsg = String.Format("Have {0} device connected!", m_LINDevNum);
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                }
                m_DevHandle = m_DevHandles[0];

                m_state = USB_DEVICE.USB_OpenDevice(m_DevHandle);  //打开设备
                if (!m_state)
                {
                    strmsg = "Open device error!";
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                    return;
                }
                else
                {
                    bResult = true;
                    strmsg = "Open device success!";
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                }
                nInit = USB2LIN_EX.LIN_EX_Init(m_DevHandle, m_LINIndex, 19200, 1); //初始化设备
                if (nInit != 0)
                {
                    strmsg = "Init device error!";
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                    return;
                }
                else
                {
                    bResult = true;
                    strmsg = "Init device success!";
                    Console.WriteLine(strmsg);
                    IncludeTextMessage(strmsg);
                }

                SetConnectionStatus(bResult);
            }
            catch (IOException ex)
            {
                MessageBox.Show("Device not connect::%s" + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnInit_Click(object sender, EventArgs e)
        {
            if (Init_Msg())
                btnHwRefresh_Click(sender, e);
            else
            {
                IncludeTextMessage("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$Attention!$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                IncludeTextMessage("This hardware maybe in use by another software or not plug in! The bit rate can not be changed.");
                IncludeTextMessage("$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$");
                return;
            }

            lock (m_LastMsgsList.SyncRoot)
            {
                m_LastMsgsList.Clear();
            }

            if (dgView.Rows.Count > 0)
            {
                dgView.Rows.Clear();
                if (m_bus.BusType == Bus.Type.CAN_BUS)
                {
                    m_lstCANMsg.Clear();
                    m_lstCANMsg.TrimExcess();
                }
                if (m_bus.BusType == Bus.Type.LIN_BUS)
                {
                    m_lstLINMsg.Clear();
                    m_lstLINMsg.TrimExcess();
                }
            }
        }

        private void btnRelease_Click(object sender, EventArgs e)
        {
            // Releases a current connected PCAN-Basic or Tomoss channel
            //
            if (m_ReadDTCEvent != null)
                m_ReadDTCEvent.Reset();

            if (m_ReadThread != null)
            {
                m_ReadThread.Abort();
                m_ReadThread.Join();
                m_ReadThread = null;
            }
            if (m_WriteThread != null)
            {
                m_bBreakInDownloading = true;
                m_WriteThread.Abort();
                m_WriteThread.Join();
                m_WriteThread = null;
            }
            if (UDS_Test_TH != null)
            {
                UDS_Test_TH.Interrupt();
                UDS_Test_TH.Abort();
                UDS_Test_TH = null;
            }

            if (m_bus.BusType == Bus.Type.LIN_BUS)
                m_linBus.Close();
            else if (m_bus.BusType == Bus.Type.CAN_BUS)
                m_canBus.Close();

            // Sets the connection status of the main-form
            //
            SetConnectionStatus(false);
            EN_DIS_WriteDID_Button(false);
            UpdateProgerss(0);

            m_strHexFileName = string.Empty;
            this.lbFilePath.Text = m_strHexFileName;
            tbDownload.Enabled = false;

            tmrDisplay.Enabled = false;
            tmrMsg.Enabled = false;

            m_bEnable_0x3E = false;
            m_bEnable_Trace = false;
        }

        /// <summary>
        /// read bin file info,get 2 block start address and length
        /// </summary>
        private void ReadBinFileInfo()
        {
            int nPackSize = 16;
            int k = 0;
            uint nToltalLen;
            byte bCurrByte;
            _Bin_Addr_Len addInfo;
            BinaryReader BR = null;
            byte[] DataBuffer = new byte[nPackSize];
            try
            {
                nToltalLen = (uint)new FileInfo(m_strHexFileName).Length;
                BR = new BinaryReader(new FileStream(m_strHexFileName, FileMode.Open));

                for (uint i = 0; i < nToltalLen; i++)
                {
                    if (k > nPackSize - 1)
                        break;

                    bCurrByte = BR.ReadByte();
                    if (i > BIN_START_POS && i <= BIN_START_POS + nPackSize)
                    {
                        DataBuffer[k++] = bCurrByte;
                    }
                }

                m_lstBinInfo = new List<_Bin_Addr_Len>();
                //block1 start address raw data
                byte b0 = DataBuffer[0];
                byte b1 = DataBuffer[1];
                byte b2 = DataBuffer[2];
                byte b3 = DataBuffer[3];
                byte[] startAddr1 = new byte[4] { b3, b2, b1, b0 };
                MEMORY_ADDR = BitConverter.ToUInt32(startAddr1, 0);

                //block1 length raw data
                byte b4 = DataBuffer[4];
                byte b5 = DataBuffer[5];
                byte b6 = DataBuffer[6];
                byte b7 = DataBuffer[7];
                byte[] Len1 = new byte[4] { b7, b6, b5, b4 };
                MEMORY_SIZE = BitConverter.ToUInt32(Len1, 0);

                addInfo = new _Bin_Addr_Len();
                addInfo.StartAddress = MEMORY_ADDR;
                addInfo.BlockLen = MEMORY_SIZE;
                m_lstBinInfo.Add(addInfo);

                //block2 start address raw data
                byte b10 = DataBuffer[8];
                byte b11 = DataBuffer[9];
                byte b12 = DataBuffer[10];
                byte b13 = DataBuffer[11];
                byte[] startAddr2 = new byte[4] { b13, b12, b11, b10 };
                MEMORY_ADDR2 = BitConverter.ToUInt32(startAddr2, 0);

                //block2 length raw data
                byte b14 = DataBuffer[12];
                byte b15 = DataBuffer[13];
                byte b16 = DataBuffer[14];
                byte b17 = DataBuffer[15];
                byte[] Len2 = new byte[4] { b17, b16, b15, b14 };
                MEMORY_SIZE2 = BitConverter.ToUInt32(Len2, 0);

                addInfo = new _Bin_Addr_Len();
                addInfo.StartAddress = MEMORY_ADDR2;
                addInfo.BlockLen = MEMORY_SIZE2;
                m_lstBinInfo.Add(addInfo);
            }
            catch (IOException iex)
            {
                IncludeTextMessage(iex.Message);
            }
            finally
            {
                BR.Close();
            }
        }

        /// <summary>
        /// read .hex file all of data that will use on CAN_UDS data checksum verify
        /// </summary>
        private void ReadHexFileData(byte[] TotalHexDataRec)
        {
            uint uToltalLen = 0;
            byte[] DataBuffer;
            int k = 0;
            int nPackSize = 16;
            byte bCurrByte;
            _Bin_Addr_Len addInfo;

            try
            {
                DataBuffer = new byte[nPackSize];
                uToltalLen = (uint)TotalHexDataRec.Length;
                for (uint i = 0; i < uToltalLen; i++)
                {
                    if (k > nPackSize - 1)
                        break;

                    bCurrByte = TotalHexDataRec[i];
                    if (i > BIN_START_POS && i <= BIN_START_POS + nPackSize)
                    {
                        DataBuffer[k++] = bCurrByte;
                    }
                }

                m_lstBinInfo = new List<_Bin_Addr_Len>();
                //block1 start address raw data
                byte b0 = DataBuffer[0];
                byte b1 = DataBuffer[1];
                byte b2 = DataBuffer[2];
                byte b3 = DataBuffer[3];
                byte[] startAddr1 = new byte[4] { b3, b2, b1, b0 };
                MEMORY_ADDR = BitConverter.ToUInt32(startAddr1, 0);

                //block1 length raw data
                byte b4 = DataBuffer[4];
                byte b5 = DataBuffer[5];
                byte b6 = DataBuffer[6];
                byte b7 = DataBuffer[7];
                byte[] Len1 = new byte[4] { b7, b6, b5, b4 };
                MEMORY_SIZE = BitConverter.ToUInt32(Len1, 0);

                addInfo = new _Bin_Addr_Len();
                addInfo.StartAddress = MEMORY_ADDR;
                addInfo.BlockLen = MEMORY_SIZE;
                m_lstBinInfo.Add(addInfo);

                //block2 start address raw data
                byte b10 = DataBuffer[8];
                byte b11 = DataBuffer[9];
                byte b12 = DataBuffer[10];
                byte b13 = DataBuffer[11];
                byte[] startAddr2 = new byte[4] { b13, b12, b11, b10 };
                MEMORY_ADDR2 = BitConverter.ToUInt32(startAddr2, 0);

                //block2 length raw data
                byte b14 = DataBuffer[12];
                byte b15 = DataBuffer[13];
                byte b16 = DataBuffer[14];
                byte b17 = DataBuffer[15];
                byte[] Len2 = new byte[4] { b17, b16, b15, b14 };
                MEMORY_SIZE2 = BitConverter.ToUInt32(Len2, 0);

                addInfo = new _Bin_Addr_Len();
                addInfo.StartAddress = MEMORY_ADDR2;
                addInfo.BlockLen = MEMORY_SIZE2;
                m_lstBinInfo.Add(addInfo);
            }
            catch (IOException ex)
            {
                IncludeTextMessage(string.Format("Some issue occoured when read HEX file content::{0}", ex.Message));
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog oFD = new OpenFileDialog();
            oFD.InitialDirectory = Environment.CurrentDirectory;
            oFD.Title = "Open flash files";            //"Open config file";
            oFD.RestoreDirectory = true;
            oFD.Filter = "CBF File(*.cbf)|*.cbf;| HEX File(*.hex)|*.hex; |BIN File(*.bin)|*.bin; |H86 File(*.h86)|*.H86;";
            oFD.Multiselect = true;

            if (oFD.ShowDialog() == DialogResult.OK)
            {
                uint uBaseAddr = 0;
                int nHexTotalLen = 0;
                int nLastDataLen = 0;
                string[] strSelectedFiles;
                string strFileName, strFileName1;
                bool bCheckFileExt1 = false;
                bool bCheckFileExt2 = false;

                strSelectedFiles = oFD.FileNames;
                m_strHexFileName = oFD.FileName;
                FileInfo fi = new FileInfo(m_strHexFileName);
                m_strHexBinExtension = fi.Extension;
                strFileName = fi.Name;
                this.lbFilePath.Text = m_strHexFileName;

                //If imported file attributes is ReadOnly,then set attributes to Normal.Otherwise can not read it content,.netframework issue message will apear.
                foreach (string strFlashFile in strSelectedFiles) 
                {
                    FileAttributes fa = File.GetAttributes(strFlashFile);
                    if (fa == (FileAttributes.ReadOnly | FileAttributes.Archive) || fa == FileAttributes.ReadOnly)
                    {
                        fa &= ~FileAttributes.ReadOnly;
                        fa |= FileAttributes.Normal;
                        File.SetAttributes(strFlashFile, fa);
                        fa = File.GetAttributes(strFlashFile);
                        IncludeTextMessage(string.Format("Flash file attributes has been modified to '" + "{0}" + "' for read.", fa.ToString()));
                    }
                }

                if (m_strHexBinExtension == ".hex")
                {
                    HexParser HP = new HexParser(m_strHexFileName, (int)PRODUCT_TYPE);
                    m_RecInfo = HP.ReadHex();
                    m_RecData = HP.lstRecData;
                    tbDownload.Enabled = true;
                }
                else if (m_strHexBinExtension == ".cbf")  //Chery CBF file
                {
                    if (PRODUCT_TYPE == PRJTYPE._Chery_CBF)
                    {
                        m_CBFParser = new CBFParser();
                        //.cbf validity and sequence check
                        if (strSelectedFiles.Length != 2)
                        {
                            IncludeTextMessage("Selected flash files not accord with one FlashDriver and one App file rule ,Please confirm then try again.");
                            return;
                        }                    
                        foreach (string strFlashFile in strSelectedFiles)
                        {
                            FileInfo fileinfo = new FileInfo(strFlashFile);
                            strFileName = fileinfo.Name;
                            if (!strFileName.Contains("FLD") && !bCheckFileExt1)
                                bCheckFileExt1 = false;
                            else
                                bCheckFileExt1 = true;

                            if (!strFileName.Contains("ASW") && !bCheckFileExt2)
                                bCheckFileExt2 = false;
                            else
                            {
                                bCheckFileExt2 = true;
                                continue;
                            }
                        }
                        if(!(bCheckFileExt1 && bCheckFileExt2))
                        {
                            IncludeTextMessage("Selected Chery .CBF file wrong, please confirm select 2 files' name one is flashdriver(FLD) and another is app(ASW) for Chery Project.");
                            return;
                        }

                        strFileName = strSelectedFiles[0];
                        strFileName1 = strSelectedFiles[1];
                        //read flashdriver & app info and data
                        if (strFileName.Contains("FLD"))
                        {
                            if (m_CBFParser.ReadCBFFile1(strFileName, 0))
                            {
                                IncludeTextMessage(string.Format("FlashDriver file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[0].StartAddr_Block, m_CBFParser.m_FlashDataLst[0].Length_Block));
                            }

                            if (strFileName1.Contains("ASW"))
                            {
                                if (m_CBFParser.ReadCBFFile1(strFileName1, 1))
                                {
                                    IncludeTextMessage(string.Format("App file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[1].StartAddr_Block, m_CBFParser.m_FlashDataLst[1].Length_Block));
                                }
                            }
                        }
                        else if (strFileName1.Contains("FLD"))
                        {
                            if (m_CBFParser.ReadCBFFile1(strFileName1, 0))
                            {
                                IncludeTextMessage(string.Format("FlashDriver file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[0].StartAddr_Block, m_CBFParser.m_FlashDataLst[0].Length_Block));
                            }

                            if (strFileName.Contains("ASW"))
                            {
                                if (m_CBFParser.ReadCBFFile1(strFileName, 1))
                                {
                                    IncludeTextMessage(string.Format("App file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[1].StartAddr_Block, m_CBFParser.m_FlashDataLst[1].Length_Block));
                                }
                            }
                        }

                        tbDownload.Enabled = true;
                        return;


                        #region FlashDrv & App in one .cbf file
                        /*
                        m_CBFParser = new CBFParser();
                        if(m_CBFParser.ReadCBFFile1(m_strHexFileName, 0))
                        {
                            IncludeTextMessage(string.Format("FlashDriver file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[0].StartAddr_Block, m_CBFParser.m_FlashDataLst[0].Length_Block));

                            //add 2nd app.hex file(get app hex data from ASW CBF file
                            oFD.InitialDirectory = Environment.CurrentDirectory;
                            oFD.Title = "Open ASW flash data file";
                            oFD.RestoreDirectory = true;
                            oFD.Filter = "CBF File(*.cbf)|*.cbf;";
                            if (oFD.ShowDialog() == DialogResult.OK) 
                            {
                                m_strHexFileName = oFD.FileName;
                                this.lbFilePath.Text = m_strHexFileName;
                                if (m_CBFParser.ReadCBFFile1(m_strHexFileName, 1)) 
                                {
                                    IncludeTextMessage(string.Format("App file info::StartAddress:0x{0,8:X8},Length:0x{1,8:X8}", m_CBFParser.m_FlashDataLst[1].StartAddr_Block, m_CBFParser.m_FlashDataLst[1].Length_Block));

                                    nHexTotalLen = (int)m_CBFParser.m_DataBlock.Length_Block;
                                    MEMORY_ADDR = m_CBFParser.m_DataBlock.StartAddr_Block;
                                    m_Total36Data = new byte[nHexTotalLen];
                                    m_Total36Data = m_CBFParser.m_DataBlock.Data;
                                    MEMORY_SIZE = (uint)nHexTotalLen;

                                    #region //write data to .bin temp file (following code for test only, .hex file data in CBF file which format is .bin already)
                                    //int nLastPackage = 0, k = 0;
                                    //byte[] line_data = new byte[0x20];
                                    //string strTempHexFilePath = fi.DirectoryName + @"\tmp.bin";                            

                                    //BinaryWriter BW = new BinaryWriter(new FileStream(strTempHexFilePath, FileMode.Create));
                                    //nLastPackage = nHexTotalLen % 0x20;

                                    //for (; k< nHexTotalLen- nLastPackage; k+=0x20)
                                    //{
                                    //    Array.Copy(m_Total36Data, k,  line_data, 0, 0x20);

                                    //    BW.Seek(k, SeekOrigin.Begin);
                                    //    BW.Write(line_data);
                                    //}
                                    //line_data = new byte[0x20]; //clear byte buffer here,cause last line data less than 0x20 nomally.
                                    //Array.Copy(m_Total36Data, k, line_data, 0, nLastPackage);
                                    //BW.Seek(k, SeekOrigin.Begin);
                                    //BW.Write(line_data);
                                    //BW.Close();

                                    //FileInfo fInfo = new FileInfo(strTempHexFilePath);
                                    //fInfo.Delete();
                                    #endregion

                                    tbDownload.Enabled = true;

                                }
                            }
                            return;
                        }
                        else
                        {
                            IncludeTextMessage("Read CBF file failure.");
                            return;
                        }
                         
                         */
                        #endregion
                    }
                }

                if (m_RecInfo != null)
                {
                    if (m_RecInfo.Count > 0)
                    {
                        if (PRODUCT_TYPE == PRJTYPE._N2S ||
                           PRODUCT_TYPE == PRJTYPE._CANUDS40 ||
                           PRODUCT_TYPE == PRJTYPE._CANUDS01 ||
                           PRODUCT_TYPE == PRJTYPE._LINHex)
                        {
                            #region Copy all of .hex file data into global byte array(at end of flash follow,use to check dependency of tansfer data)

                            //cause hex file length must size of 4096s' multiple, so we fix then...
                            uBaseAddr = m_RecInfo[0].uBaseAddress;
                            nHexTotalLen = m_RecInfo[m_RecInfo.Count - 1].nTotalLen;
                            nLastDataLen = m_RecData[m_RecData.Count - 1].uRecordLength;
                            MEMORY_ADDR = uBaseAddr;

                            //load flash(app, cal) enable status and address
                            FlashAdressSet fs = new FlashAdressSet();
                            m_bAppAddr_Enable = fs.Enable_AppAddr == true ? true : false;
                            m_bCalAddr_Enable = fs.Enable_CalAddr == true ? true : false;
                            m_uAppEndAddr = fs.AppEndAddress;
                            m_uCalStartAddr = fs.CalStartAddress;
                            //_

                            int nRealDataLen = 0;
                            byte[] tmpBuffer;
                            nRealDataLen = (m_RecData.Count - 1) * m_RecData[0].uRecordLength;
                            m_Total36Data = new byte[nRealDataLen];
                            tmpBuffer = new byte[m_RecData[0].uRecordLength];

                            for (int i = 0; i < m_RecData.Count - 1; i++)
                            {
                                tmpBuffer = m_RecData[i].Data;
                                Array.Copy(tmpBuffer, 0, m_Total36Data, i * m_RecData[i].uRecordLength, m_RecData[i].uRecordLength);
                            }
                            m_Total36Data = Combine(m_Total36Data, m_RecData[m_RecData.Count - 1].Data);

                            #endregion
                        }
                        // split app&cal flash are supported on these projects type
                        else if (PRODUCT_TYPE == PRJTYPE._SplitFlash_CAN)
                        {
                            //cause hex file length must size of 4096s' multiple, so we fix then...
                            uBaseAddr = m_RecInfo[0].uBaseAddress;
                            nHexTotalLen = m_RecInfo[m_RecInfo.Count - 1].nTotalLen;
                            nLastDataLen = m_RecData[m_RecData.Count - 1].uRecordLength;
                            MEMORY_ADDR = uBaseAddr;

                            //load flash(app, cal) enable status and address
                            FlashAdressSet fs = new FlashAdressSet();
                            m_bAppAddr_Enable = fs.Enable_AppAddr == true ? true : false;
                            m_bCalAddr_Enable = fs.Enable_CalAddr == true ? true : false;
                            m_uAppEndAddr = fs.AppEndAddress;
                            m_uCalStartAddr = fs.CalStartAddress;
                            //_

                            int nRealDataLen = 0;
                            byte[] tmpBuffer;

                            #region Select APP end address & CAL start address from UI, these can flash together or single excute.

                            //collect hex file all of data
                            if (m_bAppAddr_Enable && m_bCalAddr_Enable)
                            {
                                nRealDataLen = (m_RecData.Count - 1) * m_RecData[0].uRecordLength;
                                m_Total36Data = new byte[nRealDataLen];
                                tmpBuffer = new byte[m_RecData[0].uRecordLength];

                                for (int i = 0; i < m_RecData.Count - 1; i++)
                                {
                                    tmpBuffer = m_RecData[i].Data;
                                    Array.Copy(tmpBuffer, 0, m_Total36Data, i * m_RecData[i].uRecordLength, m_RecData[i].uRecordLength);
                                }
                                m_Total36Data = Combine(m_Total36Data, m_RecData[m_RecData.Count - 1].Data);
                                MEMORY_SIZE = (uint)nHexTotalLen;

                                //caculate how about memory size need to earse
                                uint all_residue_bytes = (uint)(CHIP_PAGESIZE - (nRealDataLen % CHIP_PAGESIZE));
                                CAN_SIZE = (uint)(nRealDataLen + all_residue_bytes);
                            }
                            //flash APP data only
                            else if (m_bAppAddr_Enable && !m_bCalAddr_Enable)
                            {
                                for (int i = 0; i < m_RecData.Count; i++)
                                {
                                    if (m_RecData[i].uPerRecordAddress == m_uAppEndAddr)
                                    {
                                        break;
                                    }
                                    m_nAppBlockNum++;
                                }

                                nRealDataLen = m_nAppBlockNum * m_RecData[0].uRecordLength;
                                m_Total36Data = new byte[nRealDataLen];
                                tmpBuffer = new byte[m_RecData[0].uRecordLength];

                                for (int j = 0; j < m_nAppBlockNum; j++)
                                {
                                    tmpBuffer = m_RecData[j].Data;
                                    Array.Copy(tmpBuffer, 0, m_Total36Data, j * m_RecData[j].uRecordLength, m_RecData[j].uRecordLength);
                                }
                                m_Total36Data = Combine(m_Total36Data, m_RecData[m_nAppBlockNum].Data);
                                MEMORY_SIZE = (uint)nRealDataLen;     // + m_RecData[nAppBlockNum].uRecordLength) ;

                                //caculate how about memory size need to earse
                                uint app_residue_bytes = CHIP_PAGESIZE - (m_uAppEndAddr % CHIP_PAGESIZE);
                                CAN_SIZE = m_uAppEndAddr + app_residue_bytes;
                            }
                            //flash CAL data only
                            else if (!m_bAppAddr_Enable && m_bCalAddr_Enable)
                            {
                                for (int i = 0; i < m_RecData.Count - 1; i++)
                                {
                                    if (m_RecData[i].uPerRecordAddress == m_uCalStartAddr)
                                    {
                                        MEMORY_ADDR = m_uCalStartAddr;
                                        break;
                                    }
                                    m_nCalBlockNum++;
                                }

                                nRealDataLen = (m_RecData.Count - (m_nCalBlockNum + 1)) * m_RecData[0].uRecordLength;
                                m_Total36Data = new byte[nRealDataLen];
                                tmpBuffer = new byte[m_RecData[0].uRecordLength];

                                int nCalIdx = 0, nCalBytes = 0;
                                for (int k = m_nCalBlockNum; k < m_RecData.Count - 1; k++)
                                {
                                    tmpBuffer = m_RecData[k].Data;
                                    Array.Copy(tmpBuffer, 0, m_Total36Data, nCalIdx * m_RecData[k].uRecordLength, m_RecData[k].uRecordLength);
                                    nCalIdx++;
                                    nCalBytes += m_RecData[0].uRecordLength;
                                }

                                m_Total36Data = Combine(m_Total36Data, m_RecData[m_RecData.Count - 1].Data);
                                MEMORY_SIZE = (uint)(nRealDataLen + m_RecData[m_RecData.Count - 1].uRecordLength);

                                //caculate how about memory size need to earse
                                uint cal_residue_bytes = (uint)(CHIP_PAGESIZE - ((nHexTotalLen - m_uCalStartAddr)));
                                CAN_SIZE = (uint)(nHexTotalLen + cal_residue_bytes);

                            }


                            #endregion

                        }
                        //else if (PRODUCT_TYPE == PRJTYPE._LINHex)//7Kw hex file read
                        //{
                        //    ReadHexFileData(m_Total36Data);
                        //}
                        //return;
                    }
                }

                if (m_bus.BusType == Bus.Type.CAN_BUS)
                {
                    MEMORY_SIZE = (uint)nHexTotalLen;
                }
                else if (m_bus.BusType == Bus.Type.LIN_BUS) //make data length integer for LIN
                {
                    if (m_strHexBinExtension == ".bin")
                    {
                        ReadBinFileInfo();
                        return;
                    }
                    else if (m_strHexBinExtension == ".hex" && PRODUCT_TYPE != PRJTYPE._xc2234)
                    {
                        MEMORY_SIZE = (uint)nHexTotalLen;
                        return;
                    }
                    else if (PRODUCT_TYPE == PRJTYPE._xc2234)
                    {
                        #region XC2234(fixed .hex with 0xff, if size of tail data block less than 0x80/0x100)

                        int n0 = 0;
                        int n1 = 0;
                        int npos = 0;
                        int nSignalBlockDataLen = 0;
                        n1 = nHexTotalLen % FIXED_DATA_SIZE;
                        if (n1 > 0)
                        {
                            n0 = FIXED_DATA_SIZE - n1; //need fix 0xFF data length0

                            nHexTotalLen += n0; //fixed new data length that is 4096 integer mutiple
                            nSignalBlockDataLen = m_RecData[0].uRecordLength;

                            //fix last data record data fullfill size 32
                            byte[] lastData = new byte[32];
                            HexParser.RecordAddrInfo RecDataInfo;

                            for (int x = 0; x < nLastDataLen; x++)
                                lastData[x] = m_RecData[m_RecData.Count - 1].Data[x];

                            //then remove last not whole line data
                            m_RecData.RemoveAt(m_RecData.Count - 1);

                            for (int z = nLastDataLen; z < nSignalBlockDataLen; z++)
                                lastData[z] = 0xFF;

                            //make new fixed data into valid data of list end 
                            RecDataInfo = new HexParser.RecordAddrInfo();
                            RecDataInfo.uRecordLength = nSignalBlockDataLen;
                            RecDataInfo.Data = lastData;
                            m_RecData.Add(RecDataInfo);

                            //fix fullfill 4096 integer mutiple data                   
                            byte[] newData = new byte[32];
                            npos = n0 / nSignalBlockDataLen; //how much block which new data needed
                            for (int i = 0; i < npos; i++)
                            {
                                for (int j = 0; j < nSignalBlockDataLen; j++)
                                {
                                    newData[j] = 0xFF;
                                }
                                RecDataInfo = new HexParser.RecordAddrInfo();
                                RecDataInfo.uRecordLength = nSignalBlockDataLen;
                                RecDataInfo.Data = newData;

                                m_RecData.Add(RecDataInfo);
                            }
                            MEMORY_SIZE = (uint)nHexTotalLen;
                        }
                        else
                        {
                            MEMORY_SIZE = (uint)nHexTotalLen;
                        }

                        #endregion
                    }
                }
            }
        }

        private void btnInfoClear_Click(object sender, EventArgs e)
        {
            lbxInfo.Items.Clear();
        }

        private void chbShowPeriod_CheckedChanged(object sender, EventArgs e)
        {
            if (chbShowPeriod.Checked)
            {
                m_bRelativeTime = true;
            }
            else
            {
                m_bRelativeTime = false;
            }
        }

        private void Diag_PCAN_Load(object sender, EventArgs e)
        {
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            // The information contained in the messages List-View
            // is cleared
            //
            lock (m_LastMsgsList.SyncRoot)
            {
                m_LastMsgsList.Clear();
            }

            dgView.Rows.Clear();
            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                m_lstCANMsg.Clear();
                m_lstCANMsg.TrimExcess();
            }
            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                m_lstLINMsg.Clear();
                m_lstLINMsg.TrimExcess();
            }
        }

        private void Diag_PCAN_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Releases the used PCAN-Basic channel
            //
            if (btnRelease.Enabled)
                btnRelease_Click(this, new EventArgs());
        }

        private void btnReadDTC_Click(object sender, EventArgs e)
        {
            bool IfTimesEnd = false;
            bool IfRunOver = false;

            #region use thread send read dtc request
            try
            {
                m_ReadDTCEvent.Reset();
                m_ReadDTCThread = new Thread(new ThreadStart(WriteDignosticReq_TH));
                m_ReadDTCThread.Priority = ThreadPriority.Lowest;
                m_ReadDTCThread.IsBackground = true;
                m_ReadDTCThread.Start();

                while (!IfRunOver && m_ReadDTCThread != null)
                {
                    IfTimesEnd = m_ReadDTCThread.IsAlive;
                    Application.DoEvents();
                    if (!IfTimesEnd || IfRunOver)
                    {
                        m_ReadDTCThread.Interrupt();
                        m_ReadDTCThread.Abort();
                        IfTimesEnd = false;
                        break;
                    }
                }
            }
            catch (IOException ex)
            {
                IncludeTextMessage(ex.Message);
            }

            #endregion

        }
        /// <summary>
        /// read DTC thread
        /// </summary>
        /// <returns></returns>
        private void WriteDignosticReq_TH()
        {
            string strReqMsg = "1902FF";
            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                byte[] RespMsg = new byte[100];

                //strReqMsg = tbData.Text.Replace(" ", "");
                m_ReqMsg = HexStringToByteArray(strReqMsg, true);

                Write_Message(m_ReqMsg);
                Thread.Sleep(30);
                ReadMessage(ref RespMsg);

                ShowDTC(m_ReqMsg, RespMsg);
            }
            else if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                byte[] RespMsg = new byte[] { };
                
                //strReqMsg = tbData.Text.Replace(" ", "");
                if (strReqMsg.Length >= 6)
                {
                    m_ReqMsg = HexStringToByteArray(strReqMsg, true);
                    ReadDTC(m_ReqMsg, ref RespMsg);

                    ShowDTC(m_ReqMsg, RespMsg);

                }
                else
                    MessageBox.Show("Input request message length must greater or equal 3 bytes!", "Error", MessageBoxButtons.OK);
            }

            Invoke(new MethodInvoker(delegate() { RefreshDBGridView(); }));
        }

        /// <summary>
        /// Read response message when send 0x19 request
        /// </summary>
        /// <param name="ReadDTC">Read DTC request(include 3 subFunction process)</param>
        /// <param name="respMsg">resoonse message</param>
        /// <returns>return 0 if succeed</returns>
        private int ReadDTC(byte[] ReadDTC, ref byte[] respMsg)
        {
            int nResult = -1;
            int n = 0;
            int nRespMsgLen = 0;
            int nResidueBytes = 0;
            int nMaxNumOfBytes = 0;
            //uint uID = 0x00;
            bool bGetPositiveResp = false;
            byte StatusMask;
            byte MulFrameByte1;
            byte[] respFrm;
            byte[] tmpRespMsg;

            byte[] ReadBuf = new byte[8];
            byte[] FollowCtrl = new byte[1] { 0x30 };
            int nEndBytes = 0;
            int BlockSize = 7;

            try
            {
                Write_CANMessage(ReadDTC, true);
                if (ReadDTC.Length >= 3)
                {
                    StatusMask = ReadDTC[2];
                }
                else
                {
                    Invoke(new MethodInvoker(delegate () { IncludeTextMessage("Dignostic request message length less than 3 bytes."); }));
                    return nResult;
                }

                MulFrameByte1 = 0x10;
                respFrm = new byte[8];

                m_ReadDTCEvent.WaitOne(P2_ServerTime * 5);
                bGetPositiveResp = dtcResp_TH(ref nMaxNumOfBytes, ref respFrm, P2_ServerTime * 10, 0x19, MulFrameByte1);
                if (bGetPositiveResp)
                {
                    //////////////////////////copy first response frame data////////////////////////////////////////////
                    nRespMsgLen = nMaxNumOfBytes;
                    tmpRespMsg = new byte[nMaxNumOfBytes];
                    Array.Copy(respFrm, 2, tmpRespMsg, 0, 6);
                    n += 6;
                    //_

                    ////follow ctrl frame request for get residue response bytes
                    //send follow control frame
                    m_ReadDTCEvent.WaitOne(P2_ServerTime * 2);
                    Write_Message(FollowCtrl);

                    MulFrameByte1 = 0x21; //mutiple response message byte one
                    bGetPositiveResp = dtcResp_TH(ref nMaxNumOfBytes, ref respFrm, P2_ServerTime * 7, 0x19, MulFrameByte1);
                    if (bGetPositiveResp)
                    {
                        nResidueBytes = nRespMsgLen - 6;
                        if (nResidueBytes > BlockSize)
                        {
                            nEndBytes = nResidueBytes % BlockSize;
                        }
                        else
                        {
                            nEndBytes = nResidueBytes; //resedue bytes less than 7 bytes
                        }
                        
                        int n7ByteGroups = (nResidueBytes / BlockSize);
                        for (int i = 0; i < n7ByteGroups; i++)
                        {
                            m_ReadDTCEvent.WaitOne(P2_ServerTime * 5);
                            lock (m_obj)
                            {
                                Array.Copy(m_RespMsg, 1, tmpRespMsg, n, BlockSize);
                                n += 7;
                            }

                            m_ReadDTCEvent.Reset();
                        }
                        if (nEndBytes > 0)
                        {
                            lock (m_obj)
                            {
                                Array.Copy(respFrm, 1, tmpRespMsg, n, nEndBytes);
                            }
                        }

                        respMsg = new byte[nRespMsgLen];
                        Array.Copy(tmpRespMsg, respMsg, nRespMsgLen);

                    }
                }
                nResult = 0;
            }
            catch (IOException ex)
            {
                Invoke(new MethodInvoker(delegate () { IncludeTextMessage(ex.Message); }));
            }

            return nResult;
        }

        /// <summary>
        /// display got dtc on UI
        /// </summary>
        /// <param name="dtc">DTC byte array</param>
        private void ShowDTC(byte[] reqMsg, byte[] dtc)
        {
            int k = 0;
            bool bDTC = false;
            string strDTC = string.Empty;
            byte[] DTC = new byte[3];
            byte reqStatusMask = 0x00;
            byte respStatusMask = 0x00;

            if (reqMsg.Length >= 3 && dtc.Length > 0)
            {
                reqStatusMask = reqMsg[2];
                respStatusMask = dtc[2];
            }
            //request StatusMask code & response StatusMask,if result not 0 then DTC need find out.
            if ((reqStatusMask & respStatusMask) == 0)
                return;

            //according fault code mask find dtc in response message byte array
            for (int i = 0; i < dtc.Length; i++)
            {
                if (bDTC)
                {
                    if (k < 3)
                        DTC[k++] = dtc[i];
                    else
                    {
                        strDTC = "";
                        for (int _ = 0; _ < DTC.Length; _++)
                        {
                            strDTC += string.Format("{0:X}", DTC[_]);
                            DTC[_] = 0x0;
                        }
                        strDTC = "DTC::0x" + strDTC;
                        this.Invoke(new MethodInvoker(delegate () { IncludeTextMessage(strDTC); }));
                        bDTC = false;
                        k = 0;
                    }
                }

                if (dtc[i] == respStatusMask/*reqStatusMask*/)
                    bDTC = true;
            }

        }

        ///<summary>
        ///wait dtc response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of wait times for other service</param>
        /// <param name="reqID">request message ID</param>
        /// <param name="MulFrameByte1">mutiple frame byte0</param>
        private bool dtcResp_TH(ref int nMaxNumOfBlockLen, ref byte[] respMsg, int nBlocks, byte reqID = 0x00, byte MulFrameByte1 = 0x20)
        {
            bool bResult = false;
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;

            while (true)
            {
                if (nLoop > nBlocks)
                {
                    bResult = false;
                    break;
                }
                ProcessFollowCtrl(reqID);
                resp = m_RespMsg;

                //waitting for response message coming              
                if (resp[0] == MulFrameByte1 && resp[2] == 0x40 + reqID) //0x19 service
                {
                    respMsg = resp;
                    nMaxNumOfBlockLen = resp[1];
                    bResult = true;
                    break;
                }
                else if (resp[0] == MulFrameByte1) //mutiple frmae response
                {
                    respMsg = resp;
                    bResult = true;
                    break;
                }
                else if (resp[0] == 0x01 && resp[2] == 0x54) //0x14 service
                {
                    respMsg = resp;
                    bResult = true;
                    break;
                }

                if (resp[1] == 0x7F)
                    nNegResp++;

                if (nNegResp > 5)
                    break;

                Thread.Sleep(5);
                nLoop++;
            }

            return bResult;
        }

        private void btnClearDTC_Click(object sender, EventArgs e)
        {
            m_ReqMsg = new byte[]  { 0x14, 0xFF, 0xFF, 0xFF};//  { 0x10, 0x03};

            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {   
                Write_Message(m_ReqMsg);
                Thread.Sleep(30);
               ReadMessage(ref m_RespMsg);
            }
            else if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                Write_CANMessage(m_ReqMsg, true);
            }
            RefreshDBGridView();
        }

        private void nudIdTo_ValueChanged(object sender, EventArgs e)
        {
            //Init_Msg();
        }

        private void nudIdFrom_ValueChanged(object sender, EventArgs e)
        {
            //Init_Msg();
        }
        
        private void btnTest_Click(object sender, EventArgs e)
        {
            string strUDSFilePath;
            //Init_Msg();

            strUDSFilePath = Directory.GetCurrentDirectory() + "\\template\\";
            strUDSFilePath += "UDS_Service.xlsx";
            FileInfo fi = new FileInfo(strUDSFilePath);
            if (fi.Exists)
            {
                bool IfTimesEnd = false;
                bool IfRunOver = false;

                m_strHexFile = strUDSFilePath;
                DoChkUDSSvrDelegate doCheckUDSSvr = new DoChkUDSSvrDelegate(Excute_DiagSvrCheck);
                UDS_Test_TH = new Thread(() => RunUDSServiceCheck_TH(this, doCheckUDSSvr));
                UDS_Test_TH.IsBackground = true;
                UDS_Test_TH.Start();

                while (!IfRunOver)
                {
                    IfTimesEnd = UDS_Test_TH.IsAlive;
                    Application.DoEvents();
                    if (!IfTimesEnd || IfRunOver)
                    {
                        UDS_Test_TH.Interrupt();
                        UDS_Test_TH.Abort();
                        IfTimesEnd = false;
                        break;
                    }
                }
            }
            else
                MessageBox.Show("IN template folder UDS_Service.xlsx file can not find.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void RunUDSServiceCheck_TH(object obj, Delegate DelegateExcute_DiagSvrCheck)
        {
            bool bResult = false;
            Diag_LIN thisPt = (Diag_LIN)obj;

            DataTable data = new DataTable();
            thisPt.m_ExcleObj = new ExcelHelper(thisPt.m_strHexFile);
            bResult = thisPt.m_ExcleObj.ExcelToDataTable(ref data, "Sheet1", true);

            if (bResult)
            {
                //read and wrtie test content from 2 row,1 column
                for (int i = 0; i < thisPt.m_ExcleObj.Row; i++)
                {
                    thisPt.Invoke(DelegateExcute_DiagSvrCheck, new object[] { data, thisPt.m_strHexFile, i, 0 });
                    Thread.Sleep(1000);
                }
                if (thisPt.m_ExcleObj.DataTableToExcel(data, "Sheet1", true) > 0)
                    thisPt.m_ExcleObj.Dispose();
                else
                    MessageBox.Show("Write test content into UDS_Service.xlsx failured.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                MessageBox.Show("UDS_Service.xlsx file can not open,maybe in use.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// accoroding readed UDS id from excel file to check relevant diagnostic id service.
        /// </summary>
        /// <param name="strUDSFilePath">defined UDS id,sub-fucntion,positive/negetive infos excel format file name</param>
        /// <param name="nRow">excel file rows num</param>
        /// <param name="nColumn">excel file column num</param>
        private bool Excute_DiagSvrCheck(DataTable data, string strUDSFilePath, int nRow, int nColumn)
        {
            string strCellValue, strUDS_ID, strSubFunc;
            string strRespMsg;
            string[] strCellArr;
            int nCorrectID = -1;
            byte[] req_msg= { 0 };
            byte[] resp_msg = { 0 };

            //Diagnostic servilce ID()
            strCellValue = data.Rows[nRow][nColumn].ToString(); 
            strCellArr = strCellValue.Split(' ');
            strUDS_ID = strCellArr[0];
           
            //sub-function
            strCellValue = data.Rows[nRow][nColumn+1].ToString();
            strCellArr = strCellValue.Split(':');

            strSubFunc = strCellArr[0];
            if (strSubFunc.Length == 4)
            {
                req_msg = new byte[2];
                req_msg[0] = Convert.ToByte(strUDS_ID, 16);
                req_msg[1] = Convert.ToByte(strSubFunc, 16);
            }
            else 
            {
                string strSubFunc01, strSubFunc02;
                strSubFunc01 = string.Empty;
                strSubFunc02 = string.Empty;

                req_msg = new byte[3];
                req_msg[0] = Convert.ToByte(strUDS_ID, 16);
                
                strSubFunc01 = strSubFunc.Substring(2, 2);
                if (strSubFunc.Length == 5)
                    strSubFunc02 = strSubFunc.Substring(4, 1);
                if (strSubFunc.Length == 6)
                    strSubFunc02 = strSubFunc.Substring(4, 2);
                req_msg[1] = Convert.ToByte(strSubFunc01, 16);
                req_msg[2] = Convert.ToByte(strSubFunc02, 16);
            }
            int k = 0, nResult = 0;
            resp_msg = new byte[8];
            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                while (++k < 3)
                {
                    nCorrectID = Write_Message(req_msg);
                    if (nCorrectID != 0)
                        return false;

                    Thread.Sleep(200);
                    nResult = (int)ReadMessage(ref resp_msg);
                    if (nResult > 0)
                        break;
                }
            }
            else if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                nResult = Write_CANMessage(req_msg, true);
            }

            strRespMsg = string.Empty;
            if (nResult < 0)
            {
                strRespMsg = "No Response";
            }
            else
            {              
                for (int i = 0; i < m_RespMsg.Length; i++)
                    strRespMsg += "0x" + string.Format("{0:X}", m_RespMsg[i]) + " ";
            }
     
            data.Rows[nRow][nColumn + 4] = strRespMsg;

            return true;
        }

        private void btnReadHexFile_Click(object sender, EventArgs e)
        {
            #region test AES-128-CMAC

            //byte[] seed = new byte[] { 0xF0, 0xB6, 0xE5, 0x3E, 0xA9, 0x01, 0xFD, 0x31, 0xE6, 0x64, 0x81, 0xD4, 0x23, 0x98, 0x7C, 0xC7 };         
            //byte[] token = new byte[16];

            //string strSeed = BitConverter.ToString(seed).Replace('-', ' ');
            //IncludeTextMessage(string.Format("AES-128-CMAC test Seed::{0}", strSeed));

            //token = fConvert.AES_128_CMAC(seed, 0x11);
            
            //string strKey = BitConverter.ToString(token).Replace('-', ' ');            
            //IncludeTextMessage(string.Format("AES-128-CMAC test Token::{0}", strKey));


            #endregion

            OpenFileDialog oFD = new OpenFileDialog();
            oFD.InitialDirectory = Environment.CurrentDirectory;
            oFD.Title = "Open hex file";
            oFD.RestoreDirectory = true;
            oFD.Filter = "HEX File(*.hex)|*.hex; |BIN File(*.bin)|*.bin; |H86 File(*.h86)|*.H86; ";

            if (oFD.ShowDialog() == DialogResult.OK)
            {
                string strHexFile = string.Empty;
                string strCRC = string.Empty;
                strHexFile = oFD.FileName;
                FileInfo fi = new FileInfo(strHexFile);
                if (fi.Extension == ".bin")
                {
                    uint CRC = fConvert.BINCRC(strHexFile);
                    strCRC = string.Format("BIN file CRC::0x{0:X}", CRC);
                    IncludeTextMessage(strCRC);
                }
                else if (fi.Extension == ".hex")
                {
                    HexParser HP = new HexParser(strHexFile);
                    List<HexParser.RecordAddrInfo> RecInfo = HP.ReadHex();

                    string strOutput = string.Empty;
                    foreach (HexParser.RecordAddrInfo rdi in RecInfo)
                    {
                        strOutput = string.Format("Block:{0}", Convert.ToString(rdi.nBlockNum, 10));
                        IncludeTextMessage(strOutput);

                        strOutput = string.Format("StartAddress:0x{0,6:x}", Convert.ToString(rdi.uBaseAddress, 16));
                        IncludeTextMessage(strOutput);
                        strOutput = string.Format("EndAddress:0x{0,6:X}", Convert.ToString(rdi.uEndAddress, 16));
                        IncludeTextMessage(strOutput);
                        strOutput = string.Format("RecordLength:0x{0,6:X}", Convert.ToString(rdi.uRecordLength, 16));
                        IncludeTextMessage(strOutput);

                        if (rdi.nTotalLen > 0)
                        {
                            strOutput = string.Format("Hex total length:0x{0,6:X}", Convert.ToString(rdi.nTotalLen, 16));
                            IncludeTextMessage(strOutput);
                        }
                    }
                }
            }

         }

        private void cbProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnWriteDID.Visible = false;
            btnResetDID.Visible = false;
            cbEnAPPMsg.Visible = false;
            btnFlashAddr.Visible = false;
            btnResetECU.Visible = false;

            if (cbProject.SelectedIndex == 0) //320v Compresor
            {
                MEMORY_ADDR = 0x08009000;
                MEMORY_SIZE = 0xF0000;

                CAN_ADDR = 0x08009000;
                CAN_SIZE = 0x00017000;

                PRODUCT_TYPE = PRJTYPE._320vCompresor;

                nudIdTo.Value = 0x7E0;
                nudIdFrom.Value = 0x7E8;
                cbbBaudrates.SelectedIndex = 1;
                //cbEnAPPMsg.Visible = true;
            }
            else if (cbProject.SelectedIndex == 1)//400v Compresor
            {
                MEMORY_ADDR = 0x00010000;
                MEMORY_SIZE = 0xF0000;

                CAN_ADDR = 0x00010000;
                CAN_SIZE = 0xF0000;

                PRODUCT_TYPE = PRJTYPE._400vCompresor;

                nudIdTo.Value = 0x7E0;
                nudIdFrom.Value = 0x7E8;
                cbbBaudrates.SelectedIndex = 1;
                //cbEnAPPMsg.Visible = true;
            }
            else if (cbProject.SelectedIndex == 2) //xc2234(.hex)
            {
                MEMORY_ADDR = 0x08009000;
                MEMORY_SIZE = 0x1000;

                PRODUCT_TYPE = PRJTYPE._xc2234;

                nudIdTo.Value = 0x3C;
                nudIdFrom.Value = 0x3D;
                numUpDownNAD.Value = 0x77;
                cbbBaudrates.SelectedIndex = 5;
                btnWriteDID.Visible = true;
                btnResetDID.Visible = true;
            }
            else if (cbProject.SelectedIndex == 3) //(7Kw .bin)
            {
                MEMORY_ADDR = 0x00C40000;
                MEMORY_SIZE = 0x1000;

                PRODUCT_TYPE = PRJTYPE._7Kw;

                nudIdTo.Value = 0x3C;
                nudIdFrom.Value = 0x3D;
                numUpDownNAD.Value = 0x77;
                cbbBaudrates.SelectedIndex = 5;
                btnWriteDID.Visible = true;
                btnResetDID.Visible = true;
            }
            else if (cbProject.SelectedIndex == 4) //N2S
            {
                MEMORY_ADDR = 0x00C00000;
                MEMORY_SIZE = 0x1000;

                CAN_ADDR = 0x00005000;
                CAN_SIZE = 0x12000;

                PRODUCT_TYPE = PRJTYPE._N2S;

                nudIdTo.Value = 0x729;
                nudIdFrom.Value = 0x7A9;
                cbbBaudrates.SelectedIndex = 1;
                //btnWriteDID.Visible = true;
                //btnResetDID.Visible = true;
                cbEnAPPMsg.Visible = true;
                btnResetECU.Visible = true;
                //btnFlashAddr.Visible = true;
            }
            else if (cbProject.SelectedIndex == 5) //CAN UDS(ac7840)
            {
                MEMORY_ADDR = 0x00010000;
                MEMORY_SIZE = 0x000F0000;

                //AC7840
                CAN_ADDR = 0x00010000;
                CAN_SIZE = 0x000F0000;

                PRODUCT_TYPE = PRJTYPE._CANUDS40;

                nudIdTo.Value = 0x7E0;
                nudIdFrom.Value = 0x7E8;
                cbbBaudrates.SelectedIndex = 1;
                btnWriteDID.Visible = true;
                btnResetDID.Visible = true;
                cbEnAPPMsg.Visible = true;
            }
            else if (cbProject.SelectedIndex == 6) //CAN UDS(ac7801)
            {
                MEMORY_ADDR = 0x08009000;
                MEMORY_SIZE = 0x000F0000;

                //AC7801
                CAN_ADDR = 0x08009000;
                CAN_SIZE = 0x00017000;

                PRODUCT_TYPE = PRJTYPE._CANUDS01;

                nudIdTo.Value = 0x7E0;
                nudIdFrom.Value = 0x7E8;
                cbbBaudrates.SelectedIndex = 1;
                //btnWriteDID.Visible = true;
                //btnResetDID.Visible = true;
                cbEnAPPMsg.Visible = true;
            }
            else if (cbProject.SelectedIndex == 7) //LIN(.hex)
            {
                MEMORY_ADDR = 0x08009000;
                MEMORY_SIZE = 0x1000;

                //borrow from CAN variables(ac7801)
                CAN_ADDR = 0x08009000;
                CAN_SIZE = 0x00017000;

                //borrow from CAN variables(ac7840)
                //CAN_ADDR = 0x00010000;
                //CAN_SIZE = 0x000F0000;

                PRODUCT_TYPE = PRJTYPE._LINHex;

                nudIdTo.Value = 0x3C;
                nudIdFrom.Value = 0x3D;
                numUpDownNAD.Value = 0x42;
                cbbBaudrates.SelectedIndex = 5;
                //btnWriteDID.Visible = true;
                //btnResetDID.Visible = true;
                //btnFlashAddr.Visible = true;
            }
            else if (cbProject.SelectedIndex == 8) //Split flash on CAN
            {
                MEMORY_ADDR = 0x08009000;
                MEMORY_SIZE = 0x000F0000;

                //borrow from CAN variables
                CAN_ADDR = 0x08009000;
                CAN_SIZE = 0x00017000;

                PRODUCT_TYPE = PRJTYPE._SplitFlash_CAN;

                nudIdTo.Value = 0x7E0;
                nudIdFrom.Value = 0x7E8;
                cbbBaudrates.SelectedIndex = 1;
                //btnWriteDID.Visible = true;
                //btnResetDID.Visible = true;
                //cbEnAPPMsg.Visible = true;
                btnFlashAddr.Visible = true;
            }
            else if (cbProject.SelectedIndex == 9) //Chery CBF
            {
                MEMORY_ADDR = 0x00C40000;
                MEMORY_SIZE = 0x1000;

                //borrow from CAN variables
                CAN_ADDR = 0x00005000;
                CAN_SIZE = 0x12000;

                PRODUCT_TYPE = PRJTYPE._Chery_CBF;

                nudIdTo.Value = 0x3C;
                nudIdFrom.Value = 0x3D;
                numUpDownNAD.Value = 0x77;
                cbbBaudrates.SelectedIndex = 5;
                //btnWriteDID.Visible = true;
                //btnResetDID.Visible = true;
                //cbEnAPPMsg.Visible = true;

            }
        }

        private void dgView_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            try
            {
                if (m_bus != null)
                {
                    if (m_bus.BusType == Bus.Type.LIN_BUS)
                    {
                        if (m_lstLINMsg != null)
                        {
                            switch (e.ColumnIndex)
                            {
                                case 0:
                                    e.Value = m_lstLINMsg[e.RowIndex].msgID;
                                    break;
                                case 1:
                                    e.Value = m_lstLINMsg[e.RowIndex].Dir;
                                    break;
                                case 2:
                                    e.Value = m_lstLINMsg[e.RowIndex].MsgLen;
                                    break;
                                case 3:
                                    e.Value = m_lstLINMsg[e.RowIndex].Count;
                                    break;
                                case 4:
                                    e.Value = m_lstLINMsg[e.RowIndex].TimeString;
                                    break;
                                case 5:
                                    e.Value = m_lstLINMsg[e.RowIndex].DataString;
                                    break;
                            }
                        }

                    }

                    if (m_bus.BusType == Bus.Type.CAN_BUS)
                    {

                        if (m_lstCANMsg != null)
                        {
                            switch (e.ColumnIndex)
                            {
                                case 0:
                                    e.Value = m_lstCANMsg[e.RowIndex].msgID;
                                    break;
                                case 1:
                                    e.Value = m_lstCANMsg[e.RowIndex].Dir;
                                    break;
                                case 2:
                                    e.Value = m_lstCANMsg[e.RowIndex].MsgLen;
                                    break;
                                case 3:
                                    e.Value = m_lstCANMsg[e.RowIndex].Count;
                                    break;
                                case 4:
                                    e.Value = m_lstCANMsg[e.RowIndex].TimeString;
                                    break;
                                case 5:
                                    e.Value = m_lstCANMsg[e.RowIndex].DataString;
                                    break;
                            }

                        }

                    }
                }                 
            
            }
            catch
            {

            }
        }

        private void btnExportTrace_Click(object sender, EventArgs e)
        {
            if (m_LastMsgsList.Count == 0)
                return;

            SaveFileDialog oFD = new SaveFileDialog();
            oFD.InitialDirectory = Environment.CurrentDirectory;
            oFD.Title = "Save trace file";
            oFD.RestoreDirectory = true;
            oFD.Filter = "Excel File(*.xls)|*.xls";

            if (oFD.ShowDialog() == DialogResult.OK)
            {
                int nPos = 0;
                DataRow dataRow;
                DataTable data = new DataTable();

                DataColumn columnID = new DataColumn("ID");
                data.Columns.Add(columnID);
                DataColumn columnDir = new DataColumn("Dir");
                data.Columns.Add(columnDir);
                DataColumn columnLen = new DataColumn("Length");
                data.Columns.Add(columnLen);
                DataColumn columnCount = new DataColumn("Count");
                data.Columns.Add(columnCount);
                DataColumn columnTime = new DataColumn("Time");
                data.Columns.Add(columnTime);
                DataColumn columnData = new DataColumn("Data");
                data.Columns.Add(columnData);

                lock (m_LastMsgsList.SyncRoot)
                {
                    foreach (MessageStatus msgStatus in m_LastMsgsList)
                    {
                        dataRow = data.NewRow();
                        if (m_bus.BusType == Bus.Type.CAN_BUS)
                        {
                            dataRow[nPos++] = msgStatus.IdString;
                            dataRow[nPos++] = msgStatus.CANMsg.Dir;
                            dataRow[nPos++] = msgStatus.CANMsg.CANMsg.LEN;
                            dataRow[nPos++] = msgStatus.Position;
                            dataRow[nPos++] = msgStatus.TimeString;
                            dataRow[nPos] = msgStatus.DataString;

                        }
                        else if (m_bus.BusType == Bus.Type.LIN_BUS)
                        {
                            dataRow[nPos++] = msgStatus.IdString;
                            dataRow[nPos++] = msgStatus.LINMsg.Dir;
                            dataRow[nPos++] = msgStatus.LINMsg.DLC;
                            dataRow[nPos++] = msgStatus.Position;
                            dataRow[nPos++] = msgStatus.TimeString;
                            dataRow[nPos] = msgStatus.DataString;
                        }

                        data.Rows.Add(dataRow);
                        nPos = 0;
                    }
                }
                string strResult = string.Empty;
                ExcelHelper ExcleObj = new ExcelHelper(oFD.FileName);
                strResult = ExcleObj.WriteTace(data);
                if (strResult == "OK")
                    MessageBox.Show("Success export trace to excel file.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                else
                    MessageBox.Show(string.Format("Some issue occur:\n{0}\n when save trace into excel file.", strResult), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

                ExcleObj.Dispose();
            }
        }
        /// <summary>
        /// refresh dbGridView
        /// </summary>
        public void RefreshDBGridView()
        {
            if (m_bus != null)
            {
                if (m_bus.BusType == Bus.Type.CAN_BUS)
                {
                    //rolling message view to newest position
                    if (m_lstCANMsg.Count > 0)
                    {
                        dgView.RowCount = m_lstCANMsg.Count;
                        if (dgView.Rows.Count > 0)
                        {
                            dgView.FirstDisplayedScrollingRowIndex = dgView.Rows[dgView.Rows.Count - 1].Index;
                        }
                    }
                }

                if (m_bus.BusType == Bus.Type.LIN_BUS )
                {
                    if (m_lstLINMsg.Count > 0)
                    {
                        dgView.RowCount = m_lstLINMsg.Count;
                        if (dgView.Rows.Count > 0)
                        {
                            dgView.FirstDisplayedScrollingRowIndex = dgView.Rows[dgView.Rows.Count - 1].Index;
                        }
                    }
                }               
            }
        }

        private void dgView_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.F5)
            {
                if (m_bus != null)
                {
                    if (m_bus.BusType == Bus.Type.CAN_BUS)
                    {
                        //rolling message view to newest position
                        if (m_lstCANMsg.Count > 0)
                        {
                            dgView.RowCount = m_lstCANMsg.Count;
                            if (dgView.Rows.Count > 0)
                            {
                                dgView.FirstDisplayedScrollingRowIndex = dgView.Rows[dgView.Rows.Count - 1].Index;
                            }
                        }
                    }

                    if (m_bus.BusType == Bus.Type.LIN_BUS)
                    {
                        if (m_lstLINMsg.Count > 0)
                        {
                            dgView.RowCount = m_lstLINMsg.Count;
                            if (dgView.Rows.Count > 0)
                            {
                                dgView.FirstDisplayedScrollingRowIndex = dgView.Rows[dgView.Rows.Count - 1].Index;
                            }
                        }
                    }
                }
               
            }
        }

        /// <summary>
        /// read & write DID value in INI file
        /// </summary>
        /// <param name="strIniFile">ini file name</param>
        /// <param name="strIniKey">ini file key name</param>        
        /// <param name="nValueLen">key value length,use to create assioate byte array</param>
        /// <param name="nBusType">bus type 0:LIN; 1:CAN</param>
        /// <returns>readed bytes from ini file</returns>
        private byte[] ReadWriteDID_OnIni(string strIniFile, string strIniKey, int nValueLen, int nBusType = 0)
        {
            int k = 0;
            string strIniKeyValueT = string.Empty;
            string strIniKeyValue = string.Empty;
            byte[] kValue = new byte[nValueLen];

            if(nBusType == 0)
            {
                if (strIniKey != "F199" && strIniKey != "F18B" && 
                    strIniKey != "00B3" && strIniKey != "008C" && strIniKey != "F184")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_LIN", strIniKey, "0", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    kValue[kValue.Length - 1] += 1;
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if(strIniKey == "00B3") //backdoor code for bootload program
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_LIN", strIniKey, "6D596C5F00000000000030303030303030303030", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if (strIniKey == "008C")
                {
                    string strBaseRegVal, strDate;
                    //Part number type encoding + Vendor code type encoding + Vendor code + Supplier produces batch type code
                    //strBaseRegVal = "31305F33303230303333383241415F31315F5A4A305F31325F";
                    strBaseRegVal = "10_302003382AA_11_ZJ0_12_";
                    //@1
                    byte[] BaseRegVal = Encoding.ASCII.GetBytes(strBaseRegVal);

                    //Year/month/day ASCII code
                    DateTime dt = DateTime.Now;
                    strDate = dt.Date.ToString("yy/MM/dd");
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;
                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 3 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }

                    //@2
                    byte[] pDataTime = Encoding.ASCII.GetBytes(strDate);                 

                    //SerialNumberDataIdentifier
                    strIniKeyValue = fConvert.ReadIniKeys("DID_LIN", strIniKey, "10000", strIniFile);
                    int nFlushTimes = int.Parse(strIniKeyValue) + 1;
                    strIniKeyValue = nFlushTimes.ToString();

                    //@3
                    byte[] FlushTimes = Encoding.ASCII.GetBytes(strIniKeyValue);
                    byte[] part = Combine(BaseRegVal, pDataTime);
                    kValue = Combine(part, FlushTimes);

                    //临时代码 
                    //string hexString = "31305F33303230303333383241415F31315F5A4A305F31325F3234313032303030303032";
                    //int byteCount = hexString.Length / 2;
                    //byte[] byteArray = new byte[byteCount];
                    //for (int i = 0; i < byteCount; i++)
                    //{
                    //    string byteString = hexString.Substring(i * 2, 2);
                    //    byteArray[i] = Convert.ToByte(byteString, 16);
                    //}
                    //_

                    //strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if (strIniKey == "F184")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_LIN", strIniKey, "4F544100000120202020202020202020", strIniFile);    //OTA001
                    kValue = HexStringToByteArray(strIniKeyValue);

                    for (k = 0; k < kValue.Length; k++)
                    {
                        if (kValue[k] == 0xFF)
                            break;
                    }
                    kValue[k - 1] += 1;
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                    fConvert.WriteIniKeys("DID_LIN", strIniKey, strIniKeyValue, strIniFile);

                    DateTime dt = DateTime.Now;
                    string strDate;
                    strDate = dt.Date.ToString("yy/MM/dd");
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;

                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 3 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }

                    byte[] DateValue = String2BCD(strDate);
                    kValue = Combine(DateValue, kValue);

                   
                    return kValue;
                }
                else if (strIniKey == "F18B" || strIniKey == "F199") //ECU Manufacturing Date  //Programming Date
                {
                    DateTime dt = DateTime.Now;
                    string strDate = dt.Date.ToString("yyyy/MM/dd");
                    string strHour = dt.Hour.ToString();
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;
                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }
                    strIniKeyValue = strDate + strHour;
                    kValue = String2BCD(strIniKeyValue);
                }

                fConvert.WriteIniKeys("DID_LIN", strIniKey, strIniKeyValue, strIniFile);
            }
           
            if(nBusType == 1)
            {
                if (strIniKey != "F199" && strIniKey != "F18B" && strIniKey != "F15A" && 
                    strIniKey != "008C" && strIniKey != "F190" && strIniKey != "F184")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_CAN", strIniKey, "0", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    for (k = 0; k < kValue.Length; k++)
                    {
                        if (kValue[k] == 0xFF)
                            break;
                    }
                    kValue[k - 1] += 1;
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if(strIniKey == "008C")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_CAN", strIniKey, "1000000000000000000000000000", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    for (k = 0; k < kValue.Length; k++)
                    {
                        if (kValue[k] == 0xFF)
                            break;
                    }
                    kValue[k - 1] += 1;
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if (strIniKey == "F190")
                {                    
                    strIniKeyValue = fConvert.ReadIniKeys("DID_CAN", strIniKey, "1000000000000000000000000000000000", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    for (k = 0; k < kValue.Length; k++)
                    {
                        if (kValue[k] == 0xFF)
                            break;
                    }
                    kValue[k - 1] += 1;
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                }
                else if(strIniKey == "F15A")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_CAN", strIniKey, "FFCB00EA3001", strIniFile);
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                    if(strIniKeyValue.Length > 0xc)
                        strIniKeyValue = strIniKeyValue.Substring(strIniKeyValue.Length/2, strIniKeyValue.Length/2);

                    DateTime dt = DateTime.Now;
                    string strDate; 
                    strDate = dt.Date.ToString("yy/MM/dd");                   
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;
                    
                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 3 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }

                    byte[] DateValue = String2BCD(strDate);
                    kValue = Combine(DateValue, kValue);

                    foreach (byte t in kValue)
                    {
                        strIniKeyValueT += /*"0" +*/ t.ToString();
                    }
                    fConvert.WriteIniKeys("DID_CAN", strIniKey, strIniKeyValueT, strIniFile);
                    return kValue;
                }
                else if (strIniKey == "F184")
                {
                    strIniKeyValue = fConvert.ReadIniKeys("DID_CAN", strIniKey, "4F544100000120202020202020202020", strIniFile);    //OTA001
                    kValue = ConvertHexStr2ByteArray(strIniKeyValue);
                    strIniKeyValue = BitConverter.ToString(kValue).Replace("-", "");
                    //if (strIniKeyValue.Length > 0xc)
                    //    strIniKeyValue = strIniKeyValue.Substring(strIniKeyValue.Length / 2, strIniKeyValue.Length / 2);

                    DateTime dt = DateTime.Now;
                    string strDate;
                    strDate = dt.Date.ToString("yy/MM/dd");
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;

                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 3 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }

                    byte[] DateValue = String2BCD(strDate);
                    kValue = Combine(DateValue, kValue);

                    foreach (byte t in kValue)
                    {
                        strIniKeyValueT += /*"0" +*/ t.ToString();
                    }
                    fConvert.WriteIniKeys("DID_CAN", strIniKey, strIniKeyValueT, strIniFile);
                    return kValue;
                }
                else if (strIniKey == "F18B" || strIniKey == "F199") //ECU Manufacturing Date  //Programming Date
                {
                    DateTime dt = DateTime.Now;
                    string strDate = dt.Date.ToString("yyyy/MM/dd");
                    string strHour = dt.Hour.ToString();
                    string[] strDates = strDate.Split('/');
                    strDate = string.Empty;
                    foreach (string s in strDates)
                    {
                        if (s.Length == 4 || s.Length == 2)
                            strDate += s;
                        else
                            strDate += "0" + s;
                    }
                    strIniKeyValue = strDate + strHour;
                    kValue = String2BCD(strIniKeyValue);

                }
                fConvert.WriteIniKeys("DID_CAN", strIniKey, strIniKeyValue, strIniFile);
            }
            return kValue;
        }

        /// <summary>
        /// manaually read DID response message 
        /// </summary>
        /// <param name="ReadDID"></param>
        /// <param name="respMsg"></param>
        /// <param name="nRespMsgLen"></param>
        /// <returns></returns>
        public int ManauallyReadMessage(byte[] ReadDID, ref byte[] respMsg, int nRespMsgLen)
        {
            int nResult = -1;
            int n = 0;
            int nResidueBytes = 0;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] tmpRespMsg = new byte[nRespMsgLen];

            try
            {
                m_bReadWriteDID = true; //read response message manually
                Write_DID_CANMessage(ReadDID, true);

                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, P2_ServerTime, 0, 0x22);
                if (bGetPositiveResp)
                {
                    if (tmpRespMsg.Length > 3) {
                        Array.Copy(m_RespMsg, 5, tmpRespMsg, 0, 3);
                        n += 3;
                    }
                    else
                    {
                        Array.Copy(m_RespMsg, 5, tmpRespMsg, 0, tmpRespMsg.Length);
                        n += tmpRespMsg.Length;
                        respMsg = tmpRespMsg;
                        nResult = 0;
                        m_bReadWriteDID = false; //read response message manually

                        return nResult;
                    }

                    ////follow ctrl frame request for get residue response bytes
                    byte[] ReadBuf = new byte[8];
                    byte[] FollowCtrl = new byte[1] { 0x30 };
                    Write_DID_CANMessage(FollowCtrl, true, true);
                    Thread.Sleep(P2_ServerTime * 3);

                    int nEndBytes = 0, BlockSize = 7;
                    nResidueBytes = nRespMsgLen - 3;

                    ReadMessage(ref ReadBuf);
                    if (nResidueBytes > BlockSize)
                        nEndBytes = nResidueBytes % BlockSize;
                    else
                        nEndBytes = 0; //resedue bytes less than 7 bytes

                    int n7ByteGroups = nResidueBytes / BlockSize;
                    for (int i = 0; i < n7ByteGroups; i++)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, BlockSize);
                        Thread.Sleep(P2_ServerTime * 3);
                        ReadMessage(ref ReadBuf);
                        n += 7;
                    }
                    if (nEndBytes > 0)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, nEndBytes);
                    }

                    respMsg = tmpRespMsg;
                }
                nResult = 0;
                m_bReadWriteDID = false; //read response message manually
            }
            catch { }
 
            return nResult;
        }

        /// <summary>
        /// manaually read DID response message 
        /// </summary>
        /// <param name="ReadDID"></param>
        /// <param name="respMsg"></param>
        /// <param name="nRespMsgLen"></param>
        /// <returns></returns>
        private int N2S_ManauallyReadMessage(byte[] ReadDID, ref byte[] respMsg, int nRespMsgLen)
        {
            int nResult = -1;
            int n = 0;
            int nResidueBytes = 0;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] tmpRespMsg = new byte[nRespMsgLen];

            try
            {
                m_bReadWriteDID = true; //read response message manually

                Write_DID_CANMessage(ReadDID, true);
                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 20, 0, 0x22);
                if (bGetPositiveResp)
                {
                    if (tmpRespMsg.Length > 3)
                    {
                        Array.Copy(m_RespMsg, 5, tmpRespMsg, 0, 3);
                        n += 3;
                    }
                    else
                    {
                        Array.Copy(m_RespMsg, 5, tmpRespMsg, 0, tmpRespMsg.Length);
                        n += tmpRespMsg.Length;
                        respMsg = tmpRespMsg;
                        nResult = 0;
                        m_bReadWriteDID = false; //read response message manually

                        return nResult;
                    }

                    ////follow ctrl frame request for get residue response bytes
                    byte[] ReadBuf = new byte[8];
                    byte[] FollowCtrl = new byte[1] { 0x30 };
                    ReadBuf = m_RespMsg;
                    
                    Thread.Sleep(P2_ServerTime);
                    Write_DID_CANMessage(FollowCtrl, true, true);
                    
                    int nEndBytes = 0, BlockSize = 7;
                    nResidueBytes = nRespMsgLen - 3;
                    if (nResidueBytes > BlockSize)
                        nEndBytes = nResidueBytes % BlockSize;
                    else
                        nEndBytes = 0; //resedue bytes less than 7 bytes

                    int n7ByteGroups = nResidueBytes / BlockSize;
                    for (int i = 0; i < n7ByteGroups; i++)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, BlockSize);
                        //Thread.Sleep(P2_ServerTime / 2);

                        ReadMessage(ref ReadBuf);
                        n += 7;
                    }
                    if (nEndBytes > 0)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, nEndBytes);
                    }

                    respMsg = tmpRespMsg;
                }
                nResult = 0;

               m_bReadWriteDID = false; //read response message manually
            }
            catch { }

            return nResult;
        }

        /// <summary>
        /// read & write DID through send bus message
        /// </summary>
        /// <param name="writeDID">byte array of 0x2E message</param>
        /// <param name="ReadDID">byte array of 0x22 message</param>
        /// <param name="respMsg">response message from ECU</param>
        /// <param name="nRespMsgLen">response message length</param>
        /// <returns></returns>
        private int Write_Read_DID(byte[] writeDID, byte[] ReadDID, ref byte[] respMsg, int nBusType = 0, int nRespMsgLen = 0)
        {
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            int k = 0, nResult = -1;
            byte[] tmpRespMsg = new byte[nRespMsgLen];
            byte[] linFC = new byte[8];
            for (int i = 0; i < respMsg.Length; i++)
                respMsg[i] = 0;

            try
            { 
                if (nBusType == 0)//LIN
                {
                    while (++k < 3)
                    {
                        if (writeDID.Length > 0)
                        {
                            nResult = Write_Message(writeDID);
                            if (nResult != 0)
                                return nResult;

                            if(PRODUCT_TYPE == PRJTYPE._xc2234)       
                                Thread.Sleep(30);
                            else if (PRODUCT_TYPE == PRJTYPE._7Kw || PRODUCT_TYPE == PRJTYPE._Chery_CBF) 
                                Thread.Sleep(10);

                            nResult = (int)ReadMessage(ref respMsg);

                            if (PRODUCT_TYPE == PRJTYPE._xc2234) 
                                Thread.Sleep(30);
                            else if (PRODUCT_TYPE == PRJTYPE._7Kw) 
                                Thread.Sleep(10);

                            if (PRODUCT_TYPE == PRJTYPE._Chery_CBF && respMsg[2] == 0x78)
                            { 
                                Thread.Sleep(500);
                                nResult = (int)ReadMessage(ref respMsg);
                            }
                        }

                        nResult = Write_Message(ReadDID);
                        if (nResult != 0)
                            return nResult;

                        if (PRODUCT_TYPE == PRJTYPE._xc2234)
                            Thread.Sleep(30);
                        else if (PRODUCT_TYPE == PRJTYPE._7Kw || PRODUCT_TYPE == PRJTYPE._Chery_CBF) 
                            Thread.Sleep(10);

                        nResult = (int)ReadMessage(ref respMsg);

                        if (PRODUCT_TYPE == PRJTYPE._7Kw && respMsg[0] == 0x78)
                        {
                            Thread.Sleep(500);
                            nResult = (int)ReadMessage(ref respMsg);
                        }
                        else if (PRODUCT_TYPE == PRJTYPE._Chery_CBF && respMsg[2] == 0x78)
                        {
                            Thread.Sleep(500);
                            nResult = (int)ReadMessage(ref respMsg);
                        }

                        if (nResult > 0 && respMsg[0] != 0x7F)
                            break; 
                    }
                }

                if (nBusType == 1)//CAN
                {                  
                    if (writeDID.Length > 0)
                    {
                        if (writeDID.Length < 8)
                        {
                            Write_DID_CANMessage(writeDID, true);
                        }
                        else
                        {
                            m_bReadWriteDID = true; //read response message manually
                            Write_DID_CANMessage(writeDID, false);

                            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, P2_ServerTime, 0, 0x2E);
                            if (bGetPositiveResp)
                            {
                                if ((writeDID[1] == 0xF1 && writeDID[2] == 0x5A) ||  //write 0xF15A only when flashing
                                   ( writeDID[1] == 0xF1 && writeDID[2] == 0x90) ||   //write 0xF190 on WriteDID button pressed
                                   ( writeDID[1] == 0x00 && writeDID[2] == 0x8C) ||  //write 0x008C on WriteDID button pressed
                                   ( writeDID[1] == 0xF1 && writeDID[2] == 0x84 ))   //write 0xF184 on WriteDID button pressed  
                                {
                                    nResult = 0;
                                    m_bReadWriteDID = false; //read response message manually
                                    return nResult;
                                }

                                nResult = ManauallyReadMessage(ReadDID, ref respMsg, nRespMsgLen);

                            }                           
                        }                    
                    }
                    else
                    {
                        Write_DID_CANMessage(ReadDID, true);
                        Thread.Sleep(P2_ServerTime);

                        nResult = 0;
                    }
                }
            }
            catch(Exception ex)
            {
                IncludeTextMessage(string.Format("Some issue occoured when write/read DID. \t\n {0}", ex.Message));
            }

            return nResult;
        }

        /// <summary>
        /// string convert to BCD code
        /// </summary>
        /// <param name="strTemp"></param>
        /// <returns></returns>
        private Byte[] String2BCD(string strTemp)
        {
            try
            {
                if (Convert.ToBoolean(strTemp.Length & 1))//If the length of the string is odd, it becomes even
                {
                    strTemp = strTemp + "0";//backword an odd number with a 0
                }
                Byte[] aryTemp = new Byte[strTemp.Length / 2];
                for (int i = 0; i < (strTemp.Length / 2); i++)
                {
                    //The number of two bytes makes up a byte of BCD code 0-9
                    aryTemp[i] = (Byte)(((strTemp[i * 2] - '0') << 4) | (strTemp[i * 2 + 1] - '0'));
                }
                return aryTemp;//Big endding  
            }
            catch
            {
                IncludeTextMessage("Convert DateTime to BCD code failed!");
                return null;
            }
        }

        /// <summary>
        /// excute write/read DID 
        /// </summary>
        /// <param name="strIniFile">stored last write DID info file</param>
        /// <param name="strIniKey">DID name</param>
        /// <param name="writDID">DID content</param>
        /// <param name="nDidLEN">DID length</param>
        /// <param name="nBusType">0: LIN bus; 1:CAN bus</param>
        /// <param name="bClearDTC">false: write dtc; true:clea dtc</param>
        /// <returns></returns>
        public bool Excute_Write_DID(string strIniFile, string strIniKey, byte[] writeDID, int nDidLEN, int nBusType = 0, bool bClearDTC = false)
        {
            bool bResult = false;
            byte[] ReadDID = new byte[3];
            byte[] respDIDData = new byte[nDidLEN+3];
            byte[] reqDIDData = new byte[nDidLEN];

            try
            {
                if (strIniKey != "F199" &&  strIniKey != "F184")
                {
                    byte[] bF0B4Resp = ReadWriteDID_OnIni(strIniFile, strIniKey, nDidLEN, nBusType);
                    if (!bClearDTC) 
                    {                     
                        if (bF0B4Resp.Length <= nDidLEN)
                        {
                            for (int i = 0; i < nDidLEN; i++)
                            {
                                if (i < bF0B4Resp.Length)
                                    reqDIDData[i] = bF0B4Resp[i];
                            }
                        }
                        else
                        {
                            for (int i = 0; i < nDidLEN; i++)
                            {
                                reqDIDData[i] = bF0B4Resp[nDidLEN - 1 - i];
                            }
                        }
                    }
                    else  //clearing dtc value
                    {
                        for (int i = 0; i < nDidLEN; i++)
                        {
                            if (i < bF0B4Resp.Length)
                                reqDIDData[i] = 0x00;
                        }
                    }
                }
                else
                {
                    reqDIDData = ReadWriteDID_OnIni(strIniFile, strIniKey, nDidLEN, nBusType);
                }
                //0xF18C write DID need use 0x008C
                if(writeDID[2] == 0x8C)
                    writeDID[1] = 0x00;
                
                byte[] WriteDID = Combine(writeDID, reqDIDData);

                if (writeDID[2] == 0xB3)
                {
                    ReadDID = writeDID;
                    ReadDID[0] = 0x22;
                    ReadDID[1] = 0xF0;
                }
                else
                {
                    ReadDID = writeDID;
                    ReadDID[0] = 0x22;
                    ReadDID[1] = 0xF1;
                }

                int nWrite = Write_Read_DID(WriteDID, ReadDID, ref respDIDData, nBusType, nDidLEN);
                if(nWrite > 0)
                    bResult = true;

                //if ((strIniKey == "F15A" && nWrite == 0) ||
                //    strIniKey == "F184" ||
                //    strIniKey == "00B3" ||
                //    strIniKey == "008C" ||
                //    strIniKey == "F190" ||
                //    strIniKey == "0002")
                //{
                //    bResult = true;
                //    return bResult;
                //};

                //string strDIDReqData;
                //string strDIDRespData;
                //strDIDReqData = BitConverter.ToString(reqDIDData).Replace("-", "");   
                //strDIDRespData = BitConverter.ToString(respDIDData).Replace("-", "");
                //if (strIniKey == "F0B4")
                //    strDIDRespData = strDIDRespData.Substring(6, strDIDRespData.Length - 6);
                //else if (strIniKey == "F190")
                //    strDIDRespData = strDIDRespData.Substring(6, strDIDRespData.Length - 2 * 6);
                //else if (strIniKey != "F18C")
                //    strDIDRespData = strDIDRespData.Substring(6, 2 * nDidLEN);

                //if (strDIDReqData != strDIDRespData)
                //{
                //    IncludeTextMessage(string.Format("DID:: {0} write failured, write opereation exit.", strIniKey));
                //}
                //else
                //    bResult = true;
            }
            catch(Exception ex)
            {
                IncludeTextMessage(ex.Message);
            }
            
            return bResult;
        }

        /// <summary>
        /// Only read DIDs for display ECU info
        /// </summary>
        /// <param name="strIniFile">stored last read DID info file</param>
        /// <param name="strIniKey">DID name</param>
        /// <param name="readDID">DID content</param>
        /// <param name="nDidLEN">DID length</param>
        /// <param name="nBusType">Bus type. 0: LIN bus; 1:CAN bus</param>
        /// <returns></returns>
        private bool Only_Read_DID(string strIniFile, string strIniKey, byte[] readDID, int nDidLEN, int nBusType = 0)
        {
            bool bResult = false;
            int nRet = -1;

            string strDIDRespData;
            byte[] writeDID = new byte[3];
            byte[] respDIDData = new byte[nDidLEN + 3];
            byte[] reqDIDData = new byte[nDidLEN];

            if(nBusType == 0)
            {
                nRet = Write_Read_DID(writeDID, readDID, ref respDIDData, nBusType, nDidLEN);

                strDIDRespData = BitConverter.ToString(respDIDData).Replace("-", "");
                if (strIniKey == "F0B3" || strIniKey == "F0B4")
                    strDIDRespData = strDIDRespData.Substring(6, strDIDRespData.Length - 6);
                else if (strIniKey == "F190" || strIniKey == "F1C1" || strIniKey == "F1C2" || strIniKey == "F1C3")
                    strDIDRespData = strDIDRespData.Substring(6, strDIDRespData.Length - 2 * 6);
                else
                    strDIDRespData = strDIDRespData.Substring(6, 2 * nDidLEN);

                    fConvert.WriteIniKeys("DID_LIN", strIniKey, strDIDRespData, strIniFile);
                nRet = 0;
            }

            if(nBusType == 1)
            {
              if(0 == ManauallyReadMessage(readDID, ref respDIDData, nDidLEN))
                {
                    strDIDRespData = BitConverter.ToString(respDIDData).Replace("-", "");
                    fConvert.WriteIniKeys("DID_CAN", strIniKey, strDIDRespData, strIniFile);
                    nRet = 0;
                }

                bResult = nRet == 0 ? true : false;
            } 

            return bResult;
        }

        /// <summary>
        /// write DID info on LIN 
        /// </summary>
        /// <param name="bClear">does clear write info or not</param>
        private void WriteDID(bool bClear)
        {
            FileStream fs = null;
            FileInfo fi = null;
            string strIniFile = string.Empty;
            string strIniSection = string.Empty;
            string strIniKey = string.Empty;
            bool bDID_Right = false;
            byte[] readDID = new byte[3];
            byte[] writeDID = new byte[3];
            byte[] respMsg = new byte[8];

            //Create DID config file DIDInfo.ini if it not exists
            strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
            fi = new FileInfo(strIniFile);
            if (!fi.Exists)
            {
                fs = File.Create(strIniFile, 50, FileOptions.None);
                fs.Close();

                strIniSection = "DID_LIN";
                //strIniKey = "F0B3";
                strIniKey = "00B3";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "6D596C5F00000000000030303030303030303030", strIniFile);

                strIniKey = "F0B4";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "0000000000000000000000000000000000000000", strIniFile);

                strIniKey = "0002";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "00", strIniFile);
            }

            try
            {
                tmrDisplay.Enabled = true;
                EN_DIS_WriteDID_Button(false);

                #region //*****************************************Read DIDs(these DID support read only)***************************************//
                //readDID[0] = 0x22;
                //readDID[1] = 0xF0;
                //readDID[2] = 0x83;
                //bDID_Right = Only_Read_DID(strIniFile, "F083", readDID, 20);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF0;
                //readDID[2] = 0x85;
                //bDID_Right = Only_Read_DID(strIniFile, "F085", readDID, 1);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x86;
                //bDID_Right = Only_Read_DID(strIniFile, "F186", readDID, 1);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x87;
                //bDID_Right = Only_Read_DID(strIniFile, "F187", readDID, 9);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x89;
                //bDID_Right = Only_Read_DID(strIniFile, "F189", readDID, 15);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0xC1;
                //bDID_Right = Only_Read_DID(strIniFile, "F1C1", readDID, 17);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0xC2;
                //bDID_Right = Only_Read_DID(strIniFile, "F1C2", readDID, 17);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0xC3;
                //bDID_Right = Only_Read_DID(strIniFile, "F1C3", readDID, 17);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x8C;
                //bDID_Right = Only_Read_DID(strIniFile, "F18C", readDID, 16);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x97;
                //bDID_Right = Only_Read_DID(strIniFile, "F197", readDID, 8);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x9E;
                //bDID_Right = Only_Read_DID(strIniFile, "F19E", readDID, 16);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0xA2;
                //bDID_Right = Only_Read_DID(strIniFile, "F1A2", readDID, 16);

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0xB0;
                //bDID_Right = Only_Read_DID(strIniFile, "F1B0", readDID, 1);

                #endregion//********************************************************************************************************************//

                m_ReqMsg = new byte[] { 0x10, 0x03 }; 
                if (Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                {
                    MessageBox.Show("Message ID not correct.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (m_RespMsg[0] == 0x50 && m_RespMsg[1] == 0x03) //Enter extension session
                {
                    //*************************************(Sessiong switch to programe in BOOT mode)*****************************//
                    //if (m_nWriteDID_Times == 0)
                    {
                        //change session to 0x27 05
                        m_ReqMsg = new byte[] { 0x27, 0x01 }; //SubFunc05、06
                        byte[] resp0x27 = new byte[18];
                        Send5TimeReqMsg(m_ReqMsg, ref resp0x27);

                        if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x01) //SubFunc05、06
                        {
                            byte[] reqKEY = new byte[18];
                            byte[] SeedArray = new byte[16];
                            byte[] KeyArray = new byte[16];
                            for (int i = 0; i < 16; i++)
                                SeedArray[i] = resp0x27[i + 2];
                            fConvert.seedToKey3(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                            reqKEY[0] = 0x27;
                            reqKEY[1] = 0x02; //                  SubFunc05、06
                            reqKEY[2] = KeyArray[0];
                            reqKEY[3] = KeyArray[1];
                            reqKEY[4] = KeyArray[2];
                            reqKEY[5] = KeyArray[3];

                            reqKEY[6] = 0x31;
                            reqKEY[7] = 0x01;
                            reqKEY[8] = 0xFF;
                            reqKEY[9] = 0x00;

                            reqKEY[10] = 0x08;
                            reqKEY[11] = 0x00;
                            reqKEY[12] = 0x90;
                            reqKEY[13] = 0x00;

                            reqKEY[14] = 0x00;
                            reqKEY[15] = 0x17;
                            reqKEY[16] = 0x00;
                            reqKEY[17] = 0xFF;

                            Send5TimeReqMsg(reqKEY, ref respMsg);
                            if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x02)
                            {
                                IncludeTextMessage("Security access pass.");

                                if (PRODUCT_TYPE == PRJTYPE._7Kw) 
                                {
                                    //writeDID = new byte[3];

                                    writeDID[0] = 0x2E;
                                    writeDID[1] = 0x00;
                                    writeDID[2] = 0xB3;

                                    if(!bClear)
                                    { 
                                        bDID_Right = Excute_Write_DID(strIniFile, "00B3", writeDID, 20);//
                                    }
                                    else //clear dtc if func selected
                                    {
                                        bDID_Right = Excute_Write_DID(strIniFile, "00B3", writeDID, 20, 0, true);
                                        if (bDID_Right)
                                        {
                                            IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                        }
                                    }
                                    if (!bDID_Right)
                                    {
                                        IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                        return;
                                    }

                                    writeDID[0] = 0x2E;
                                    writeDID[1] = 0x00;
                                    writeDID[2] = 0x8C;

                                    if (!bClear) 
                                    { 
                                        bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 16);//
                                    }
                                    else
                                    {
                                        bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 16, 0, true);//
                                        if(bDID_Right)
                                        {
                                            IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                            return;
                                        }
                                    }
                                    if (!bDID_Right)
                                    {
                                        IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                        return;
                                    }
                                }                               

                                //write DID succeed, so we brush 'WriteDID' button to LimeGreen
                                IncludeTextMessage("Please wait for 5s for diagnostic security session switch finished.");
                                IncludeTextMessage("PTC will jump from boot to app mode, system restart. please wait for a moument.");
                                Thread.Sleep(5 * 1000);

                                SetWriteDID_ButtonColor("Write DID", Color.LimeGreen);
                                IncludeTextMessage(@"*****Now please press Download button to flash App file into PTC on next hardware.*****" );
                                //IncludeTextMessage(@"***Now please press Download button to flash App file into PTC,***" + "\t\n" + @"***After flash App file success,then press Write DID1 button again to write residue DIDs.***");

                                m_nWriteDID_Times++;
                                EN_DIS_WriteDID_Button(true);                      

                                /*
                                 * No need send 0x11 01 to reset ecu after flash finished
                                 * 
                                 *  //soft restart request(jump to App mode for write residue DIDs)
                                m_ReqMsg = new byte[] { 0x11, 0x01 };
                                Write_Message(m_ReqMsg);
                                Thread.Sleep(P2_ServerTime);
                                byte[] resp = new byte[8];
                                ReadMessage(ref resp);
                                Thread.Sleep(P2_ServerTime);
                                if (resp[0] == 0x51 && resp[1] == 0x01)
                                {
                                    SetWriteDID_ButtonColor("Write DID1", Color.LimeGreen);

                                    IncludeTextMessage("PTC will jump from boot to app mode, system restart. please wait for a moument.");
                                    IncludeTextMessage(@"***Now please press Download button to flash App file into PTC,***" + "\t\n" + @"***After flash App file success,then press Write DID1 button again to write residue DIDs.***");

                                    Thread.Sleep(1000);
                                    m_nWriteDID_Times++;
                                }
                                     
                                 */

                            }
                            else
                                SetWriteDID_ButtonColor("Write DID1", Color.Red);
                        }
                        else
                            SetWriteDID_ButtonColor("Write DID1", Color.Red);
                    }

                    #region //**************************************(write DID in APP mode)***********************************************//

                    /*                   
                     if (m_nWriteDID_Times == 2)
                    {
                        //change session to 0x27 01
                        m_ReqMsg = new byte[] { 0x27, 0x01 }; //SubFunc01、02
                        byte[] resp0x2701 = new byte[18];
                        Send5TimeReqMsg(m_ReqMsg, ref resp0x2701);

                        if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x01) //SubFunc01、02
                        {
                            byte[] reqKEY = new byte[18];
                            byte[] SeedArray = new byte[16];
                            byte[] KeyArray = new byte[16];
                            for (int i = 0; i < 16; i++)
                                SeedArray[i] = resp0x2701[i + 2];
                            fConvert.seedToKey3(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                            reqKEY[0] = 0x27;
                            reqKEY[1] = 0x02; //                  SubFunc01、02
                            reqKEY[2] = KeyArray[0];
                            reqKEY[3] = KeyArray[1];
                            reqKEY[4] = KeyArray[2];
                            reqKEY[5] = KeyArray[3];

                            reqKEY[6] = 0x31;
                            reqKEY[7] = 0x01;
                            reqKEY[8] = 0xFF;
                            reqKEY[9] = 0x00;

                            reqKEY[10] = 0x08;
                            reqKEY[11] = 0x00;
                            reqKEY[12] = 0x90;
                            reqKEY[13] = 0x00;

                            reqKEY[14] = 0x00;
                            reqKEY[15] = 0x17;
                            reqKEY[16] = 0x00;
                            reqKEY[17] = 0xFF;

                            Send5TimeReqMsg(reqKEY, ref respMsg);
                            if (m_RespMsg[0] == 0x67 && m_RespMsg[1] == 0x02)
                            {
                                //write DIDs value(F0B4, F18B, F190, F198) in extension session
                                writeDID = new byte[3] { 0x2E, 0xF0, 0xB4 };

                                bDID_Right = Excute_Write_DID(strIniFile, "F0B4", writeDID, 20);
                                if (!bDID_Right)
                                    return;

                                writeDID[0] = 0x2E;
                                writeDID[1] = 0xF1;
                                writeDID[2] = 0x8B;
                                bDID_Right = Excute_Write_DID(strIniFile, "F18B", writeDID, 5);
                                if (!bDID_Right)
                                {
                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                    return;
                                }

                                writeDID[0] = 0x2E;
                                writeDID[1] = 0xF1;
                                writeDID[2] = 0x90;
                                bDID_Right = Excute_Write_DID(strIniFile, "F190", writeDID, 17);
                                if (!bDID_Right)
                                {
                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                    return;
                                }

                                writeDID[0] = 0x2E;
                                writeDID[1] = 0xF1;
                                writeDID[2] = 0x98;
                                bDID_Right = Excute_Write_DID(strIniFile, "F198", writeDID, 10);
                                if (!bDID_Right)
                                {
                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                    return;
                                }
                                //_End write

                                SetWriteDID_ButtonColor("Write DID2", Color.LimeGreen);
                                m_nWriteDID_Times = 0;

                            }
                            else
                                SetWriteDID_ButtonColor("Write DID2", Color.Red);
                        }
                        else
                            SetWriteDID_ButtonColor("Write DID2", Color.Red);
                    }
                     */
                    #endregion
                }
                else
                    SetWriteDID_ButtonColor("Write DID", Color.Red);

                tmrDisplay.Enabled = false;
                RefreshDBGridView();
            }
            catch (Exception ex)
            {
                IncludeTextMessage(string.Format("Error occured when write DID info:{0}", ex.Message));
            }
        }

        /// <summary>
        /// write DID info on CAN
        /// </summary>
        /// <param name="bClear">does clear write info or not</param>
        public void CAN_WriteDID(bool bClear)
        {
            FileStream fs = null;
            FileInfo fi = null;
            string strIniFile = string.Empty;
            string strIniSection = string.Empty;
            string strIniKey = string.Empty;
            bool bDID_Right = false;
            bool bGetPositiveResp = false;
            int nMaxNumOfBlock = 0;
            int nSendResult = 0;
            byte[] readDID = new byte[3];
            byte[] writeDID = new byte[3];
            byte[] respMsg = new byte[8];

            //Create DID config file DIDInfo.ini if it not exists
            strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
            fi = new FileInfo(strIniFile);
            if (!fi.Exists)
            {
                fs = File.Create(strIniFile, 50, FileOptions.None);
                fs.Close();

                strIniSection = "DID_CAN";

                strIniKey = "008C"; 
                fConvert.WriteIniKeys(strIniSection, strIniKey, "1000000000000000000000000000", strIniFile);

                strIniKey = "F15A";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "FFCB00EA3001", strIniFile);

                strIniKey = "F184";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "4F544100000120202020202020202020", strIniFile);

                strIniKey = "F190";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "1000000000000000000000000000000000", strIniFile);

                strIniKey = "0002";
                fConvert.WriteIniKeys(strIniSection, strIniKey, "00", strIniFile);   
            }

            try
            {
                tmrDisplay.Enabled = true;

                #region //*****************************************Read DIDs(these DID support read only)***************************************//
                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x87;
                //bDID_Right = Only_Read_DID(strIniFile, "F187", readDID, 12, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x8A;
                //bDID_Right = Only_Read_DID(strIniFile, "F18A", readDID, 10, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x93;
                //bDID_Right = Only_Read_DID(strIniFile, "F193", readDID, 2, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x95;
                //bDID_Right = Only_Read_DID(strIniFile, "F195", readDID, 2, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x97;
                //bDID_Right = Only_Read_DID(strIniFile, "F197", readDID, 10, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x8C;
                //bDID_Right = Only_Read_DID(strIniFile, "F18C", readDID, 14, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x90;
                //bDID_Right = Only_Read_DID(strIniFile, "F190", readDID, 17, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}

                //readDID[0] = 0x22;
                //readDID[1] = 0xF1;
                //readDID[2] = 0x5B;
                //bDID_Right = Only_Read_DID(strIniFile, "F15B", readDID, 4, 1);
                //if (!bDID_Right)
                //{
                //    IncludeTextMessage(string.Format("Read DID::{0} failured.", BitConverter.ToString(readDID)));
                //    return;
                //}


                #endregion//********************************************************************************************************************//

                if (PRODUCT_TYPE == PRJTYPE._7Kw)
                {
                    m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                    Write_DID_CANMessage(m_ReqMsg, true);

                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                    if (bGetPositiveResp)//Enter extension session
                    {
                        m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programe session
                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                        bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                        if (bGetPositiveResp)//Enter extension session
                        {
                            //*************************************(Session switch to programe in BOOT mode)*****************************//
                            //if (m_nWriteDID_Times == 0)
                            {
                                //change session to 0x27 05
                                m_ReqMsg = new byte[] { 0x27, 0x01 }; //SubFunc01、02
                                Write_DID_CANMessage(m_ReqMsg, true);
                                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 30, 0, 0x27);
                                if (bGetPositiveResp)//SubFunc01、02
                                {
                                    byte[] SeedArray = new byte[4];
                                    byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                    for (int i = 0; i < SeedArray.Length; i++)
                                        SeedArray[i] = m_RespMsg[i + 3];

                                    fConvert.seedToKey2(SeedArray, out KeyArray, MASK);

                                    m_ReqMsg = new byte[6];
                                    m_ReqMsg[0] = 0x27;
                                    m_ReqMsg[1] = 0x02;
                                    m_ReqMsg[2] = KeyArray[0];
                                    m_ReqMsg[3] = KeyArray[1];
                                    m_ReqMsg[4] = KeyArray[2];
                                    m_ReqMsg[5] = KeyArray[3];

                                    nSendResult = Write_DID_CANMessage(m_ReqMsg, true);
                                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 30, 0, 0x27);
                                    if (bGetPositiveResp)
                                    {
                                        IncludeTextMessage("Security access pass.");

                                        writeDID[0] = 0x2E;
                                        writeDID[1] = 0x00;
                                        writeDID[2] = 0x8C;

                                        if (!bClear)
                                        {
                                            bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1);
                                        }
                                        else  //clearing dtc if user select
                                        {
                                            bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1, true);
                                            if (bDID_Right)
                                            {
                                                IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                                return;
                                            }
                                        }

                                        if (!bDID_Right)
                                        {
                                            IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                            return;
                                        }

                                        ////①write DID succeed, so we brush 'WriteDID' button to LimeGreen
                                        IncludeTextMessage("Please wait for 5s for diagnostic security session switch finished.");
                                        IncludeTextMessage("PTC will jump from boot to app mode, system restart. please wait for a moument.");
                                        Thread.Sleep(5 * 1000);
                                        m_nWriteDID_Times++;
                                        SetWriteDID_ButtonColor("Write DID", Color.LimeGreen);
                                        IncludeTextMessage(@"*****Now please press Download button to flash App file into PTC on next hardware.*****");

                                    }
                                    else
                                        SetWriteDID_ButtonColor("Write DID1", Color.Red);
                                }
                                else
                                    SetWriteDID_ButtonColor("Write DID1", Color.Red);
                            }

                            #region //**************************************(write DID in APP mode)***********************************************//
                            /*
                            if (m_nWriteDID_Times == 2)
                            {
                                m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programing session
                                Write_DID_CANMessage(m_ReqMsg, true);

                                if (m_RespMsg[1] == 0x50 && m_RespMsg[2] == 0x02)
                                {
                                    //change session to 0x27 01
                                    m_ReqMsg = new byte[] { 0x27, 0x09 }; //SubFunc09、0A
                                    Write_DID_CANMessage(m_ReqMsg, true);

                                    if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x09) //SubFunc09、0A
                                    {
                                        byte[] SeedArray = new byte[4];
                                        byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                        for (int i = 0; i < SeedArray.Length; i++)
                                            SeedArray[i] = m_RespMsg[i + 3];
                                        fConvert.N2S_seedToKey(SeedArray, out KeyArray, 9);   //according response seed caculate security access key

                                        m_ReqMsg = new byte[6];
                                        m_ReqMsg[0] = 0x27;
                                        m_ReqMsg[1] = 0x0A;
                                        m_ReqMsg[2] = KeyArray[0];
                                        m_ReqMsg[3] = KeyArray[1];
                                        m_ReqMsg[4] = KeyArray[2];
                                        m_ReqMsg[5] = KeyArray[3];

                                        nSendResult = Write_DID_CANMessage(m_ReqMsg, true);
                                        Thread.Sleep(100);
                                        if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
                                        {
                                            //write DIDs value(F15A) in Programing session
                                            writeDID[0] = 0x2E;
                                            writeDID[1] = 0xF1;
                                            writeDID[2] = 0x5A;
                                            bDID_Right = Excute_Write_DID(strIniFile, "F15A", writeDID, 9, 1);
                                            if (!bDID_Right)
                                            {
                                                IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                                return;
                                            }                                        

                                            //_End write

                                            SetWriteDID_ButtonColor("Write DID2", Color.LimeGreen);
                                            m_nWriteDID_Times = 0;

                                        }
                                        else
                                            SetWriteDID_ButtonColor("Write DID2", Color.Red);
                                    }
                                    else
                                        SetWriteDID_ButtonColor("Write DID2", Color.Red);
                                }
                            }                    
                             */
                            #endregion
                        }
                    }
                    else
                        SetWriteDID_ButtonColor("Write DID", Color.Red);
                }
                else if (PRODUCT_TYPE == PRJTYPE._N2S)
                {
                    m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 15, 0, 0x10);
                    if (bGetPositiveResp)
                    {
#if _SecurityAccess

                        m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                        if (bGetPositiveResp)
                        {
                            byte[] SeedArray = new byte[4];
                            byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                            for (int i = 0; i < SeedArray.Length; i++)
                                SeedArray[i] = m_RespMsg[i + 3];
                            fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                            m_ReqMsg = new byte[6];
                            m_ReqMsg[0] = 0x27;
                            m_ReqMsg[1] = 0x02;
                            m_ReqMsg[2] = KeyArray[0];
                            m_ReqMsg[3] = KeyArray[1];
                            m_ReqMsg[4] = KeyArray[2];
                            m_ReqMsg[5] = KeyArray[3];

                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                            if (bGetPositiveResp)
#endif
                            {
                                m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                //Thread.Sleep(2 * P2_ServerTime);
                                //if (m_RespMsg[1] == 0x50 && m_RespMsg[2] == 0x02)
                                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                                if (bGetPositiveResp)
                                {
                                    //Enable TestPresent 0x3E  & message view rolling
                                    m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                    Write_CANMessage(m_ReqMsg, true);
                                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x3E);
                                    if (bGetPositiveResp)
                                    {
                                        m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                        Write_CANMessage(m_ReqMsg, true);
                                        Thread.Sleep(2 * P2_ServerTime);
                                    }
                                    else
                                    {
                                        IncludeTextMessage("0x3E service not work normally.");
                                        return;
                                    }

#if _SecurityAccess
                                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                                    //Thread.Sleep(3 * P2_ServerTime);
                                    //if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x01)
                                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                                    if (bGetPositiveResp)
                                    {
                                        SeedArray = new byte[4];
                                        KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                        for (int i = 0; i < SeedArray.Length; i++)
                                            SeedArray[i] = m_RespMsg[i + 3];
                                        fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                                        m_ReqMsg = new byte[6];
                                        m_ReqMsg[0] = 0x27;
                                        m_ReqMsg[1] = 0x02;
                                        m_ReqMsg[2] = KeyArray[0];
                                        m_ReqMsg[3] = KeyArray[1];
                                        m_ReqMsg[4] = KeyArray[2];
                                        m_ReqMsg[5] = KeyArray[3];

                                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                                        //Thread.Sleep(3 * P2_ServerTime);
                                        //if (m_RespMsg[1] == 0x67 && m_RespMsg[2] == 0x02)
                                        bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                                        if (bGetPositiveResp)
#endif
                                        {
#if _SecurityAccess
                                            m_ReqMsg = new byte[] { 0x27, 0x09 }; //Security access,request seed
                                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                                            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                                            if (bGetPositiveResp)
                                            {
                                                SeedArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                                KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                                for (int i = 0; i < SeedArray.Length; i++)
                                                    SeedArray[i] = m_RespMsg[i + 3];
                                                fConvert.N2S_seedToKey(SeedArray, out KeyArray, 9);   //according response seed caculate security access key

                                                m_ReqMsg = new byte[6];
                                                m_ReqMsg[0] = 0x27;
                                                m_ReqMsg[1] = 0x0A;
                                                m_ReqMsg[2] = KeyArray[0];
                                                m_ReqMsg[3] = KeyArray[1];
                                                m_ReqMsg[4] = KeyArray[2];
                                                m_ReqMsg[5] = KeyArray[3];

                                                nSendResult = Write_CANMessage(m_ReqMsg, true);
                                                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                                                if (bGetPositiveResp)
#endif
                                                {
                                                    IncludeTextMessage("Security access pass.");

                                                    //write DIDs value(008C) in Programing session
                                                    writeDID = new byte[3];
                                                    writeDID[0] = 0x2E;
                                                    writeDID[1] = 0x00;
                                                    writeDID[2] = 0x8C;

                                                    if (!bClear)
                                                    {
                                                        bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1);//
                                                    }
                                                    else
                                                    {
                                                        bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1, true);
                                                        if (bDID_Right)
                                                        {
                                                            IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                                            //If not write DID 0xF190,then do this.otherwise marked following to line codes.
                                                            File.Delete(strIniFile);
                                                            return;
                                                        }
                                                    }

                                                    ////write DIDs value(F190) in Programing session
                                                    //writeDID = new byte[3];
                                                    //writeDID[0] = 0x2E;
                                                    //writeDID[1] = 0xF1;
                                                    //writeDID[2] = 0x90;

                                                    //if (!bClear)
                                                    //{
                                                    //    bDID_Right = Excute_Write_DID(strIniFile, "F190", writeDID, 17, 1);//
                                                    //}
                                                    //else
                                                    //{
                                                    //    bDID_Right = Excute_Write_DID(strIniFile, "F190", writeDID, 17, 1, true);
                                                    //    if (bDID_Right)
                                                    //    {
                                                    //        IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                                    //        File.Delete(strIniFile);
                                                    //        return;
                                                    //    }
                                                    //}

                                                    //if (!bDID_Right)
                                                    //{
                                                    //    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                                    //    return;
                                                    //}

                                                    //②EN_DIS_WriteDID_Button(false); //not use
                                                    //soft restart request(jump to App mode for write residue DIDs)  not need
                                                    m_ReqMsg = new byte[] { 0x11, 0x01 };
                                                    Write_DID_CANMessage(m_ReqMsg, true);
                                                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 20, 0, 0x11);
                                                    if (bGetPositiveResp)
                                                    {
                                                        int nReadResult = -1;
                                                        Thread.Sleep(500);

                                                        //read 0x22 F1 8C
                                                        readDID[0] = 0x22;
                                                        readDID[1] = 0xF1;
                                                        readDID[2] = 0x8C;
                                                        nReadResult |= N2S_ManauallyReadMessage(readDID, ref respMsg, 17); // len:14 + 3(0x22 F1 8C)

                                                        ////read 0x22 F1 90
                                                        //readDID[0] = 0x22;
                                                        //readDID[1] = 0xF1;
                                                        //readDID[2] = 0x90;
                                                        //nReadResult |= N2S_ManauallyReadMessage(readDID, ref respMsg, 20); //len:17 + 3(0x22 F1 90)

                                                        SetWriteDID_ButtonColor("Write DID1", Color.LimeGreen);
                                                        IncludeTextMessage("PTC will jump from boot to app mode, system restart. please wait for a moument.");
                                                        IncludeTextMessage(@"***Now please press Download button to flash App file into PTC,***");
                                                        IncludeTextMessage(@"***After flash App file success,then press Write DID1 button again to write residue DIDs.***");
                                                        m_nWriteDID_Times++;
                                                    }
                                                    else
                                                        SetWriteDID_ButtonColor("Write DID1", Color.Red);
                                                }
                                                else
                                                    SetWriteDID_ButtonColor("Write DID1", Color.Red);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }


                }
                else if (PRODUCT_TYPE == PRJTYPE._CANUDS40 || PRODUCT_TYPE == PRJTYPE._CANUDS01)
                {
                    m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                    Write_DID_CANMessage(m_ReqMsg, true);

                    bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                    if (bGetPositiveResp)//Enter extension session
                    {
                        m_ReqMsg = new byte[] { 0x27, 0x01 }; //Programe session
                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                        bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 5, 0, 0x27);
                        if (bGetPositiveResp)//Enter extension session
                        {
                            byte[] SeedArray = new byte[4];
                            byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                            for (int i = 0; i < SeedArray.Length; i++)
                                SeedArray[i] = m_RespMsg[i + 3];

                            fConvert.seedToKey2(SeedArray, out KeyArray, MASK);

                            m_ReqMsg = new byte[6];
                            m_ReqMsg[0] = 0x27;
                            m_ReqMsg[1] = 0x02;
                            m_ReqMsg[2] = KeyArray[0];
                            m_ReqMsg[3] = KeyArray[1];
                            m_ReqMsg[4] = KeyArray[2];
                            m_ReqMsg[5] = KeyArray[3];

                            nSendResult = Write_DID_CANMessage(m_ReqMsg, true);
                            bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 30, 0, 0x27);
                            if (bGetPositiveResp)
                            {
                                IncludeTextMessage("Security access pass.");
                                writeDID = new byte[3];
                                writeDID[0] = 0x2E;
                                writeDID[1] = 0x00;
                                writeDID[2] = 0x8C;

                                if (!bClear)
                                {
                                    bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1);//
                                }
                                else
                                {
                                    bDID_Right = Excute_Write_DID(strIniFile, "008C", writeDID, 14, 1, true);
                                    if (bDID_Right)
                                    {
                                        IncludeTextMessage(string.Format("DID::{0} has been cleared.", BitConverter.ToString(writeDID)));
                                        return;
                                    }
                                }
                                if (!bDID_Right)
                                {
                                    IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                    return;
                                }

                                //②EN_DIS_WriteDID_Button(false); //not use
                                //soft restart request(jump to App mode for write residue DIDs)  not need
                                m_ReqMsg = new byte[] { 0x11, 0x01 };
                                Write_DID_CANMessage(m_ReqMsg, true);
                                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, 500, 0, 0x11);
                                if (bGetPositiveResp)
                                {
                                    SetWriteDID_ButtonColor("Write DID1", Color.LimeGreen);
                                    IncludeTextMessage("PTC will jump from boot to app mode, system restart. please wait for a moument.");
                                    IncludeTextMessage(@"***Now please press Download button to flash App file into PTC,***" + "\t\n" + @"***After flash App file success,then press Write DID1 button again to write residue DIDs.***");
                                    m_nWriteDID_Times++;
                                }
                                else
                                    SetWriteDID_ButtonColor("Write DID1", Color.Red);

                            }
                        }
                    }
                    tmrDisplay.Enabled = false;
                    RefreshDBGridView();
                }
                else if (PRODUCT_TYPE == PRJTYPE._Chery_CBF)
                { 
                
                
                }
            }
            catch (Exception ex)
            {
                IncludeTextMessage(string.Format("Error occured when write DID info:{0}", ex.Message));
            }
        }

        private void btnWriteDID_Click(object sender, EventArgs e)
        {
            if (m_bus != null) {
                if (m_bus.BusType == Bus.Type.LIN_BUS)
                    WriteDID(false);
                else if (m_bus.BusType == Bus.Type.CAN_BUS)
                    CAN_WriteDID(false);
            }
        }

        /// <summary>
        /// Set WriteDID Button BackColor
        /// </summary>
        public void SetWriteDID_ButtonColor(string strBtnText, Color color)
        {
            this.btnWriteDID.SuspendLayout();
            btnWriteDID.Text = strBtnText;
            btnWriteDID.BackColor = color;
            this.btnWriteDID.ResumeLayout();
            this.btnWriteDID.PerformLayout();
        }

        private void btnResetDID_Click(object sender, EventArgs e)
        {
            if (m_bus.BusType == Bus.Type.LIN_BUS)
                WriteDID(true);
            else if (m_bus.BusType == Bus.Type.CAN_BUS)
                CAN_WriteDID(true);
        }

        private void tmrDisplay_Tick(object sender, EventArgs e)
        {
            if (m_bus.BusType == Bus.Type.CAN_BUS)
            {
                //after 0x10 02/03 request sent,0x3E send cyclie
                if (m_bEnable_0x3E)
                {
                    byte[] ReqMsg0 = new byte[] { 0x3E, 0x80 };
                    Write_CANMessage(ReqMsg0, true);
                }
            }

            if (m_bus.BusType == Bus.Type.LIN_BUS)
            {
                if (/*PRODUCT_TYPE == PRJTYPE._7Kw &&*/ m_bEnable_0x3E)
                {
                    byte[] ReqMsg0 = new byte[] { 0x3E, 0x80 };
                    Write_Message(ReqMsg0);
                }
            }
        }

        private void cbEnAPPMsg_CheckedChanged(object sender, EventArgs e)
        {
            if (cbEnAPPMsg.Checked)
            {
                MessageFilter(false);
                m_DisplayAppMsg = true;
                tbDownload.Enabled = true;
            }
            else
            {
                MessageFilter(true);
                m_DisplayAppMsg = false;
                tbDownload.Enabled = false;
            }
        }

        /// <summary>
        /// Manual read response message when send 0x19 request
        /// </summary>
        /// <param name="ReadDTC">Read DTC request(include 3 subFunction process)</param>
        /// <param name="respMsg">resoonse message</param>
        /// <returns>return 0 if succeed</returns>
        private int ManualReadDTC(byte[] ReadDTC, ref byte[] respMsg)
        {
            int nResult = -1;
            //int n = 0;
            int nSubFunc = 0;
            int nRespMsgLen = 0;
            int nResidueBytes = 0;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] tmpRespMsg;

            try
            {
                m_bReadWriteDID = true; //read response message manually
                Write_DID_CANMessage(ReadDTC, true);

                bGetPositiveResp = N2S_canResp_TH(ref nMaxNumOfBlock, P2_ServerTime/2, 0, 0x19);
                if (bGetPositiveResp)
                {
                    nSubFunc = ReadDTC[1];
                    //Report Number Of DTC By Status Mask
                    if (nSubFunc == 1)
                    {
                        nRespMsgLen = m_RespMsg[2] * 4 + 2;
                    }
                    else if (nSubFunc == 0x02)
                    { 
                        nRespMsgLen = m_RespMsg[2] * 4 + 3;
                    }
                    else if(nSubFunc == 0x0A)
                    {
                        nRespMsgLen = m_RespMsg[2] + 5;
                    }
                
                    tmpRespMsg = new byte[nRespMsgLen];

                    //Get response message only,no need manual collect response message for 0x19 request.
                    //Array.Copy(m_RespMsg, 1, tmpRespMsg, 0, 7);
                    //n += 7;

                    ////follow ctrl frame request for get residue response bytes
                    byte[] ReadBuf = new byte[8];
                    byte[] FollowCtrl = new byte[1] { 0x30 };
                    Write_DID_CANMessage(FollowCtrl, true, true);
                    Thread.Sleep(P2_ServerTime * 3);

                    int nEndBytes = 0, BlockSize = 7;
                    nResidueBytes = nRespMsgLen - 7;

                    ReadMessage(ref ReadBuf);

                    if (nResidueBytes > BlockSize)
                        nEndBytes = nResidueBytes % BlockSize;
                    else
                        nEndBytes = 0; //resedue bytes less than 7 bytes

                    int n7ByteGroups = nResidueBytes / BlockSize;
                    for (int i = 0; i < n7ByteGroups; i++)
                    {                        
                        Thread.Sleep(P2_ServerTime * 3);
                        ReadMessage(ref ReadBuf);

                        //Array.Copy(ReadBuf, 1, tmpRespMsg, n, BlockSize);
                        //n += 7;
                    }
                    //if (nEndBytes > 0)
                    //{
                    //    Array.Copy(ReadBuf, 1, tmpRespMsg, n, nEndBytes);
                    //}

                    respMsg = tmpRespMsg;
                }
                nResult = 0;
                m_bReadWriteDID = false; //read response message manually
            }
            catch { }

            return nResult;
        }

        private void tmrMsg_Tick(object sender, EventArgs e)
        {
            //if(m_bEnable_Trace) //refresh per 100ms since download start,no need get positive response at 0x3E 0x00
                RefreshDBGridView();
        }

        private void cbWholeTrace_CheckedChanged(object sender, EventArgs e)
        {
            if(cbWholeTrace.Checked)
            {
                m_WholeTrace = true;
            }
            else
            {
                m_WholeTrace = false;
            }
        }

        private void btnFlashAddr_Click(object sender, EventArgs e)
        {
            FlashAdressSet FAS = new FlashAdressSet();
            DialogResult dr = FAS.ShowDialog();
            if(dr == DialogResult.OK)
            {
                m_bAppAddr_Enable = FAS.Enable_AppAddr;
                m_bCalAddr_Enable = FAS.Enable_CalAddr;

                m_uAppEndAddr = FAS.AppEndAddress;
                m_uCalStartAddr = FAS.CalStartAddress;
            }
        }

        private void btnResetECU_Click(object sender, EventArgs e)
        {
            m_ReqMsg = new byte[] { 0x11, 0x01 }; //reset ecu
            Write_CANMessage(m_ReqMsg, true);
            IncludeTextMessage("ECU has been reseted!");
        }

        /// <summary>
        /// provide a interface for control 'WriteDiD' button enable or not
        /// </summary>
        /// <param name="bEnable">enable or not </param>
        public void EN_DIS_WriteDID_Button(bool bEnable)
        {
            if (bEnable)
                btnWriteDID.Enabled = true;
            else
            {
                SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                btnWriteDID.Enabled = false;
            }
        }

        public class CBFParser : CBFParserBase
        {
            CBFParserBase.Header m_header;
            //header length
            uint m_uHeaderLen;
            //CRC32 of CBF file
            uint m_uCBF_CRC;

            public CBFParser()
            {
                m_FlashDataLst = new List<DataBlock>();

                m_header.hw_part_number = "v1.0";
                m_header.hw_part_number_DID = 0xF187;
                m_header.sw_part_number = "dop_2408";
                m_header.sw_part_number_DID = 0xF013;
                m_header.sw_version = "v1.1";
                m_header.sw_version_DID = 0xF189;
                m_header.system_supplier_identifier = "MANHUI";
                m_header.system_supplier_identifier_DID = 0xF18A;
                m_header.tester_request_CAN_ID = 0x7E0;
                m_header.ECU_response_CAN_ID = 0x7E8;
                m_header.hash_algorithm = "SHA256";
                m_header.RSA_algorithm = "RSA3072";
                m_header.security_access_algorithm = "SA_9";
                m_header.file_integrity_check = "sw_signature_asymmetric";
                m_header.verification_block_root_hash = "6a34ffb97af13062019c597a76a03cbcb397d0a7f21f6d8719d69b69969dfba2";

            }

            /// <summary>
            /// read CBF file content(encapsulation flashdriver / app data into one .cbf file)
            /// </summary>
            /// <returns></returns>
            public override bool ReadCBFFile0(string strFileName)
            {
                bool bResult = false;
                bool bBinFile = false;
                int k = 0;
                int nEraseStartAddr = 0;
                int nEraseInfoLen = 0;
                int nBlockIdx = 0;
                uint nToltalLen;
                byte bCurrByte;
                BinaryReader BR = null;

                string strTempField;
                string strHeaderContent;
                string strHeaderLen;
                string strCBFCRC;

                string strEraseAddr;
                string strEraseLen;

                string[] strCBFContent;
                string[] strCBFChildContent;

                byte[] Header = new byte[0x8];
                byte[] HeaderLen = new byte[0x8];
                byte[] CBFCRC = new byte[0x8];
                try
                {
                    if (m_cbf_filename == string.Empty)
                        return bResult;

                    nToltalLen = (uint)new FileInfo(m_cbf_filename).Length;
                    BR = new BinaryReader(new FileStream(m_cbf_filename, FileMode.Open));

                    for (uint i = 0; i < nToltalLen; i++)
                    {
                        bCurrByte = BR.ReadByte();

                        //CBF header length
                        if (i > (0xf + 0x2) && i <= (0xf + 0xa))
                        {
                            HeaderLen[k++] = bCurrByte;
                        }
                        else if (i > (0xf + 0xa + 0x1))
                        {
                            if (i == (0xf + 0xa + 0x2))
                            {
                                k = 0;
                                strHeaderLen = Encoding.ASCII.GetString(HeaderLen);
                                m_uHeaderLen = (uint)Convert.ToInt32(strHeaderLen, 16);
                                Header = new byte[m_uHeaderLen];
                            }
                            else
                            {
                                //CBF file CRC 
                                if (i > (0xf + 0xa) && i < (0xf + 2 * 0xa + 0x1))
                                {
                                    CBFCRC[k++] = bCurrByte;
                                }
                                else
                                {
                                    if (i == (0xf + 2 * 0xa + 0x1))
                                    {
                                        strCBFCRC = Encoding.ASCII.GetString(CBFCRC);
                                        m_uCBF_CRC = (uint)Convert.ToInt32(strCBFCRC, 16);
                                        k = 0;
                                    }
                                    else
                                    {
                                        //Header start with 0x7B('{‘）, end with 0x7D('}' )
                                        if (i > (0xf + 2 * 0xa + 0x1) && i < (0xf + 2 * 0xa + 0x1 + m_uHeaderLen - 3)) //pure header content 
                                        {
                                            Header[k++] = bCurrByte;
                                        }
                                        else
                                        {
                                            //CBF content filled
                                            if (Header[0] != 0x00 && Header[1] != 0x00)
                                            {
                                                strHeaderContent = Encoding.ASCII.GetString(Header);
                                                strCBFContent = strHeaderContent.Split(',');

                                                for (int j = 0; j < strCBFContent.Length; j++)
                                                {
                                                    strCBFChildContent = strCBFContent[j].Split(':');

                                                    if (strCBFChildContent[0].Contains("hw_part_number"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.hw_part_number = strCBFChildContent[1];
                                                        else
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                            m_header.hw_part_number_DID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_part_number"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.hw_part_number = strCBFChildContent[1];
                                                        else
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                            m_header.sw_part_number_DID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_version"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.sw_version = strCBFChildContent[1];
                                                        else
                                                        {
                                                            if (!strCBFChildContent[1].Contains("null"))
                                                            {
                                                                strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                                m_header.sw_version_DID = Convert.ToUInt16(strTempField, 16);
                                                            }
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_part_type"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.sw_part_type = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("system_supplier_identifier"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("identifier_DID"))
                                                            m_header.system_supplier_identifier = strCBFChildContent[1];
                                                        else
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                            m_header.system_supplier_identifier_DID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("hash_algorithm"))
                                                    {
                                                        m_header.hash_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("RSA_algorithm"))
                                                    {
                                                        m_header.RSA_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("tester_request_CAN_ID"))
                                                    {
                                                        if (!strCBFChildContent[1].Contains("null"))
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                            m_header.tester_request_CAN_ID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("ECU_response_CAN_ID"))
                                                    {
                                                        if (!strCBFChildContent[1].Contains("null"))
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                            m_header.ECU_response_CAN_ID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("security_access_algorithm"))
                                                    {
                                                        m_header.security_access_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("file_integrity_check"))
                                                    {
                                                        m_header.file_integrity_check = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("verification_block_root_hash"))
                                                    {
                                                        m_header.verification_block_root_hash = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("erase"))  /* split stHeaderContent,for get erase info*/
                                                    {
                                                        nEraseInfoLen = strCBFChildContent[2].Length;
                                                        if (nEraseInfoLen <= 0xc) //.hex
                                                            strEraseAddr = strCBFChildContent[2].Substring(3, sizeof(UInt64));
                                                        else  //.bin
                                                        {
                                                            strEraseAddr = strCBFChildContent[2].Substring(11, sizeof(UInt64));
                                                            bBinFile = true;
                                                        }
                                                        //FlashDriver Erase address
                                                        m_header.erase.start_address = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                        m_DataBlock.StartAddr_Block = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                    }
                                                    else if (strCBFChildContent[0].Contains("start_address"))
                                                    {
                                                        nEraseStartAddr = strCBFChildContent[1].Length;
                                                        if (nEraseStartAddr <= 0xc) //.hex
                                                            strEraseAddr = strCBFChildContent[1].Substring(3, sizeof(UInt64));
                                                        else  //.bin
                                                        {
                                                            strEraseAddr = strCBFChildContent[1].Substring(11, sizeof(UInt64));
                                                            bBinFile = true;
                                                        }
                                                        //FlashDriver Erase address
                                                        m_header.erase.start_address = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                        m_DataBlock.StartAddr_Block = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                    }
                                                    else if (strCBFChildContent[0].Contains("length"))
                                                    {
                                                        if (nBlockIdx == 0) //flash drvier info
                                                        {
                                                            nEraseInfoLen = strCBFChildContent[1].Length;
                                                            if (nEraseInfoLen <= 0x10)//.hex
                                                                strEraseLen = strCBFChildContent[1].Substring(3, sizeof(UInt64));
                                                            else
                                                            {
                                                                strEraseLen = strCBFChildContent[1].Substring(11, sizeof(UInt64));
                                                                bBinFile = true;
                                                            }
                                                            //Ease length
                                                            m_header.erase.length = (uint)Convert.ToInt32(strEraseLen, 16);
                                                            m_DataBlock.Length_Block = (uint)Convert.ToInt32(strEraseLen, 16);

                                                            if (!bBinFile)
                                                            {
                                                                m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                            }
                                                            else
                                                            {
                                                                m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                            }

                                                            m_DataBlock.CheckSum = new byte[2];
                                                            m_FlashDataLst.Add(m_DataBlock);
                                                        }
                                                        else if (nBlockIdx == 1) //flash data info
                                                        {
                                                            nEraseInfoLen = strCBFChildContent[1].Length;
                                                            if (nEraseInfoLen <= 0x10)//.hex
                                                                strEraseLen = strCBFChildContent[1].Substring(3, sizeof(UInt64));
                                                            else
                                                            {
                                                                strEraseLen = strCBFChildContent[1].Substring(11, sizeof(UInt64));
                                                                bBinFile = true;
                                                            }
                                                            //Ease length
                                                            m_header.erase.length = (uint)Convert.ToInt32(strEraseLen, 16);
                                                            m_DataBlock.Length_Block = (uint)Convert.ToInt32(strEraseLen, 16);

                                                            if (!bBinFile)
                                                            {
                                                                m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                            }
                                                            else
                                                            {
                                                                m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                            }

                                                            m_DataBlock.CheckSum = new byte[2];
                                                            m_FlashDataLst.Add(m_DataBlock);

                                                            Header[0] = 0x00;
                                                            Header[1] = 0x00;
                                                        }
                                                        nBlockIdx++;
                                                        k = 0;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //if (!bBinFile) 
                                                {
                                                    //.hex data (if input signle file,do following marked code)                   

                                                    //Skip the header and special characters (", {},[])
                                                    if (i > (2 * 0xF + 0xD + m_uHeaderLen) &&
                                                        i <= (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                    {
                                                        if (m_header.erase.length > 0)
                                                            m_FlashDataLst[0].Data[k++] = bCurrByte;
                                                    }

                                                    if (i == (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                        k = 0;

                                                    //Skip the header and special characters (", {},[]), and take the data from the previous block.
                                                    //The starting address and length (8 bytes) of the next block follow, which are also skipped.
                                                    //The pure data is then extracted based on the length of the second block.
                                                    if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA) &&
                                                        i <= (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block))
                                                    {
                                                        if (m_header.erase.length > 0)
                                                            m_FlashDataLst[1].Data[k++] = bCurrByte;
                                                    }
                                                }
                                                //else
                                                {
                                                    //.bin data                    
                                                    //if (i > (2 * 0xF + 0xD + m_uHeaderLen) && i < (2 * 0xF + 0xD + m_uHeaderLen + m_header.erase.length))
                                                    //{
                                                    //    if (m_header.erase.length > 0)
                                                    //        m_DataBlock.Data[k++] = bCurrByte;
                                                    //}
                                                }

                                                //.bin file checksum(block1) 
                                                if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block) &&
                                                    i <= (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                {
                                                    if (i == (2 * 0xF + 0xE + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                    {
                                                        k = 0;
                                                        m_FlashDataLst[0].CheckSum[k++] = bCurrByte;
                                                    }
                                                    if (i == (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                    {
                                                        m_FlashDataLst[0].CheckSum[k] = bCurrByte;
                                                        k = 0;

                                                        bResult = true;
                                                    }
                                                }
                                                //.bin file checksum(block2)
                                                if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block) &&
                                                    i <= (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block))
                                                {
                                                    if (i == (2 * 0xF + 0xE + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block))
                                                    {
                                                        k = 0;
                                                        m_FlashDataLst[1].CheckSum[k++] = bCurrByte;
                                                    }
                                                    if (i == (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block))
                                                    {
                                                        m_FlashDataLst[1].CheckSum[k] = bCurrByte;
                                                        k = 0;

                                                        bResult = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                catch (IOException ioex)
                {
                    Console.Write("Some issue occoured when read .cbf file, for detail:" + ioex.Message);
                }
                finally
                {
                    if (BR != null)
                        BR.Close();
                }

                return bResult;
            }


            /// <summary>
            /// read CBF file content(splilt flashdriver / app data into two .cbf file)
            /// </summary>
            /// <returns></returns>
            public override bool ReadCBFFile1(string strFileName, int nSeq)
            {
                bool bResult = false;
                bool bBinFile = false;
                int k = 0;
                int nEraseStartAddr = 0;
                int nEraseInfoLen = 0;
                uint nToltalLen;
                byte bCurrByte;
                BinaryReader BR = null;

                string strTempField;
                string strHeaderContent;
                string strHeaderLen;
                string strCBFCRC;

                string strEraseAddr;
                string strEraseLen;

                string[] strCBFContent;
                string[] strCBFChildContent;

                byte[] Header = new byte[0x8];
                byte[] HeaderLen = new byte[0x8];
                byte[] CBFCRC = new byte[0x8];
                try
                {
                    if (m_cbf_filename == string.Empty)
                        return bResult;

                    m_cbf_filename = strFileName;
                    nToltalLen = (uint)new FileInfo(m_cbf_filename).Length;
                    BR = new BinaryReader(new FileStream(m_cbf_filename, FileMode.Open));

                    for (uint i = 0; i < nToltalLen; i++)
                    {
                        bCurrByte = BR.ReadByte();

                        //CBF header length
                        if (i > (0xf + 0x2) && i <= (0xf + 0xa))
                        {
                            HeaderLen[k++] = bCurrByte;
                        }
                        else if (i > (0xf + 0xa + 0x1))
                        {
                            if (i == (0xf + 0xa + 0x2))
                            {
                                k = 0;
                                strHeaderLen = Encoding.ASCII.GetString(HeaderLen);
                                m_uHeaderLen = (uint)Convert.ToInt32(strHeaderLen, 16);
                                Header = new byte[m_uHeaderLen];
                            }
                            else
                            {
                                //CBF file CRC 
                                if (i > (0xf + 0xa) && i < (0xf + 2 * 0xa + 0x1))
                                {
                                    CBFCRC[k++] = bCurrByte;
                                }
                                else
                                {
                                    if (i == (0xf + 2 * 0xa + 0x1))
                                    {
                                        strCBFCRC = Encoding.ASCII.GetString(CBFCRC);
                                        m_uCBF_CRC = (uint)Convert.ToInt32(strCBFCRC, 16);
                                        k = 0;
                                    }
                                    else
                                    {
                                        //Header start with 0x7B('{‘）, end with 0x7D('}' )
                                        if (i > (0xf + 2 * 0xa + 0x1) && i < (0xf + 2 * 0xa + 0x1 + m_uHeaderLen - 3)) //pure header content 
                                        {
                                            Header[k++] = bCurrByte;
                                        }
                                        else
                                        {
                                            //CBF content filled
                                            if (Header[0] != 0x00 && Header[1] != 0x00)
                                            {
                                                strHeaderContent = Encoding.ASCII.GetString(Header);
                                                strCBFContent = strHeaderContent.Split(',');

                                                for (int j = 0; j < strCBFContent.Length; j++)
                                                {
                                                    strCBFChildContent = strCBFContent[j].Split(':');

                                                    if (strCBFChildContent[0].Contains("hw_part_number"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.hw_part_number = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("hw_part_number_DID"))
                                                    {
                                                        if (strCBFChildContent[0].Contains("DID"))
                                                        { 
                                                            strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                            m_header.hw_part_number_DID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_part_number"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.hw_part_number = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_part_number_DID"))
                                                    {
                                                        if (strCBFChildContent[0].Contains("DID")) 
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                            m_header.sw_part_number_DID = Convert.ToUInt16(strTempField, 16);
                                                        }                                                    
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_part_type"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.sw_part_type = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_version"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.sw_version = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("sw_version_DID"))
                                                    {
                                                        if (strCBFChildContent[0].Contains("DID"))
                                                        {
                                                            if (!strCBFChildContent[1].Contains("null"))
                                                            {
                                                                strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                                m_header.sw_version_DID = Convert.ToUInt16(strTempField, 16);
                                                            }
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("system_supplier_identifier"))
                                                    {
                                                        if (!strCBFChildContent[0].Contains("DID"))
                                                            m_header.system_supplier_identifier = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("system_supplier_identifier_DID"))
                                                    {
                                                        if (strCBFChildContent[0].Contains("identifier_DID"))
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                            m_header.system_supplier_identifier_DID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("hash_algorithm"))
                                                    {
                                                        m_header.hash_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("RSA_algorithm"))
                                                    {
                                                        m_header.RSA_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("tester_request_CAN_ID"))
                                                    {
                                                        if (!strCBFChildContent[1].Contains("null"))
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                            m_header.tester_request_CAN_ID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("ECU_response_CAN_ID"))
                                                    {
                                                        if (!strCBFChildContent[1].Contains("null"))
                                                        {
                                                            strTempField = strCBFChildContent[1].Substring(1, strCBFChildContent[1].Length - 2);
                                                            m_header.ECU_response_CAN_ID = Convert.ToUInt16(strTempField, 16);
                                                        }
                                                    }
                                                    else if (strCBFChildContent[0].Contains("security_access_algorithm"))
                                                    {
                                                        m_header.security_access_algorithm = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("file_integrity_check"))
                                                    {
                                                        m_header.file_integrity_check = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("verification_block_root_hash"))
                                                    {
                                                        m_header.verification_block_root_hash = strCBFChildContent[1];
                                                    }
                                                    else if (strCBFChildContent[0].Contains("erase"))  /* split stHeaderContent,for get erase info*/
                                                    {
                                                        nEraseInfoLen = strCBFChildContent[2].Length;
                                                        if (nEraseInfoLen <= 0xc) //.hex
                                                            strEraseAddr = strCBFChildContent[2].Substring(3, sizeof(UInt64));
                                                        else  //.bin
                                                        {
                                                            strEraseAddr = strCBFChildContent[2].Substring(11, sizeof(UInt64));
                                                            bBinFile = true;
                                                        }
                                                        //FlashDriver Erase address
                                                        m_header.erase.start_address = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                        m_DataBlock.StartAddr_Block = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                    }
                                                    else if (strCBFChildContent[0].Contains("start_address"))
                                                    {
                                                        nEraseStartAddr = strCBFChildContent[1].Length;
                                                        if (nEraseStartAddr <= 0xc) //.hex
                                                            strEraseAddr = strCBFChildContent[1].Substring(3, sizeof(UInt64));
                                                        else  //.bin
                                                        {
                                                            strEraseAddr = strCBFChildContent[1].Substring(11, sizeof(UInt64));
                                                            bBinFile = true;
                                                        }
                                                        //FlashDriver Erase address
                                                        m_header.erase.start_address = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                        m_DataBlock.StartAddr_Block = (uint)Convert.ToInt32(strEraseAddr, 16);
                                                    }
                                                    else if (strCBFChildContent[0].Contains("length"))
                                                    {
                                                        nEraseInfoLen = strCBFChildContent[1].Length;
                                                        if (nEraseInfoLen <= 0x10)//.hex
                                                            strEraseLen = strCBFChildContent[1].Substring(3, sizeof(UInt64));
                                                        else
                                                        {
                                                            strEraseLen = strCBFChildContent[1].Substring(11, sizeof(UInt64));
                                                            bBinFile = true;
                                                        }
                                                        //Ease length
                                                        m_header.erase.length = (uint)Convert.ToInt32(strEraseLen, 16);
                                                        m_DataBlock.Length_Block = (uint)Convert.ToInt32(strEraseLen, 16);

                                                        if (!bBinFile)
                                                        {
                                                            m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                        }
                                                        else
                                                        {
                                                            m_DataBlock.Data = new byte[m_DataBlock.Length_Block];
                                                        }

                                                        m_DataBlock.CheckSum = new byte[2];
                                                        m_DataBlock.header = m_header;
                                                        m_FlashDataLst.Add(m_DataBlock);

                                                        Header[0] = 0x00;
                                                        Header[1] = 0x00;

                                                        k = 0;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                //if (!bBinFile) 
                                                {
                                                    //.hex data (if input signle file,do following marked code)                   

                                                    //Skip the header and special characters (", {},[])
                                                    //if(nSeq == 0) //FLD flashdrv file

                                                    if (i > (2 * 0xF + 0xD + m_uHeaderLen) &&
                                                        i <= (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block))
                                                    {
                                                        if (m_header.erase.length > 0)
                                                            m_FlashDataLst[nSeq].Data[k++] = bCurrByte;
                                                    }

                                                    if (i == (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block))
                                                        k = 0;

                                                    /*
                                                     if (m_header.sw_part_type.Contains("FLD"))
                                                    {
                                                        if (i > (2 * 0xF + 0xD + m_uHeaderLen) &&
                                                            i <= (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                        {
                                                            if (m_header.erase.length > 0)
                                                                m_FlashDataLst[0].Data[k++] = bCurrByte;
                                                        }

                                                        if (i == (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                            k = 0;
                                                    }
                                                    //else if(nSeq == 1) // ASW app file
                                                    else if(m_header.sw_part_type.Contains("ASW"))
                                                    {
                                                        if (i > (2 * 0xF + 0xD + m_uHeaderLen) &&
                                                             i <= (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[1].Length_Block))
                                                        {
                                                            if (m_header.erase.length > 0)
                                                                m_FlashDataLst[1].Data[k++] = bCurrByte;
                                                        }

                                                        if (i == (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[1].Length_Block))
                                                            k = 0;
                                                    }
                                                     */


                                                }
                                                //else
                                                {
                                                    //.bin data                    
                                                    //if (i > (2 * 0xF + 0xD + m_uHeaderLen) && i < (2 * 0xF + 0xD + m_uHeaderLen + m_header.erase.length))
                                                    //{
                                                    //    if (m_header.erase.length > 0)
                                                    //        m_DataBlock.Data[k++] = bCurrByte;
                                                    //}
                                                }

                                                //.bin file checksum(block1) 
                                                if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block) &&
                                                    i <= (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block))
                                                {
                                                    if (i == (2 * 0xF + 0xE + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block))
                                                    {
                                                        k = 0;
                                                        m_FlashDataLst[nSeq].CheckSum[k++] = bCurrByte;
                                                    }
                                                    if (i == (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[nSeq].Length_Block))
                                                    {
                                                        m_FlashDataLst[nSeq].CheckSum[k] = bCurrByte;
                                                        k = 0;
                                                        bResult = true;
                                                    }
                                                }

                                                //if (m_header.sw_part_type.Contains("FLD"))
                                                //{
                                                //    if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block) &&
                                                //        i <= (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                //    {
                                                //        if (i == (2 * 0xF + 0xE + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                //        {
                                                //            k = 0;
                                                //            m_FlashDataLst[0].CheckSum[k++] = bCurrByte;
                                                //        }
                                                //        if (i == (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
                                                //        {
                                                //            m_FlashDataLst[0].CheckSum[k] = bCurrByte;
                                                //            k = 0;
                                                //            bResult = true;
                                                //        }
                                                //    }
                                                //}
                                                ////else if (nSeq == 1)
                                                //else if (m_header.sw_part_type.Contains("ASW"))
                                                //{
                                                //    if (i > (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[1].Length_Block) &&
                                                //        i <= (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[1].Length_Block))
                                                //    {
                                                //        if (i == (2 * 0xF + 0xE + m_uHeaderLen + m_FlashDataLst[1].Length_Block))
                                                //        {
                                                //            k = 0;
                                                //            m_FlashDataLst[1].CheckSum[k++] = bCurrByte;
                                                //        }
                                                //        if (i == (2 * 0xF + 0xF + m_uHeaderLen + m_FlashDataLst[1].Length_Block))
                                                //        {
                                                //            m_FlashDataLst[1].CheckSum[k] = bCurrByte;
                                                //            k = 0;
                                                //            bResult = true;
                                                //        }
                                                //    }

                                                //}
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                }
                catch (IOException ioex)
                {
                    Console.Write("Some issue occoured when read .cbf file, for detail:" + ioex.Message);
                }
                finally
                {
                    if (BR != null)
                        BR.Close();
                }

                return bResult;
            }
        }
    }
}
