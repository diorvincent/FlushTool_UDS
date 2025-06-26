/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Dec 9, 2023
 * Modify on: Jan 10, 2024
 * Author: He Jingchi
 * Modifier: He Jingchi
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using USB2XXX;

/// <summary>
/// Inclusion of PEAK PCAN-Basic namespace
/// </summary>
using Peak.Can.Basic;
using TPCANHandle = System.UInt16;
using System.Runtime.InteropServices;
using TPCANBitrateFD = System.String;
using TPCANTimestampFD = System.UInt64;

namespace Diag_BUS
{
    class Bus
    {
        public enum Type : byte
        {
            CAN_BUS = 0,
            LIN_BUS
        }

        public struct CANParam
        {
            public bool bCANFD;
            public string Bitrate;
            public string IO;
            public string Interrupt;
            public TPCANType HwType;
        }

        public static CANParam cCANParams;

        public virtual Type BusType
        {
            get;
        }

        public virtual uint Init(ref string strmsg, ushort BaudRate = 500, object canOBJ=null)
        {
            return 0;
        }

        public virtual UInt16 Close()
        {
            return 0;
        }

        public virtual int SendMessage(object BusMsg)
        {
            return 0;
        }

        public virtual int ReceiveMessage(out object BusMsg)
        {
            BusMsg = new object();
            return 0;
        }
        
        public virtual int ReceiveMessage(out object BusMsg, int MsgLen=8)
        {
            BusMsg = new object();
            return 0;
        }

        public static Bus Initialize(ref string strmsg, ushort BaudRate, object canOBJ)
        {
            ushort nConnected = 0;
            Bus bus = null;
            CAN_Bus CANBus = new CAN_Bus();
            LIN_Bus LINBus = new LIN_Bus();

            if (nConnected == CANBus.Init(ref strmsg, BaudRate, canOBJ))
            {
                bus = (Bus)CANBus;
                return bus;
            }

            if (0 == LINBus.Init(ref strmsg, BaudRate))
            {
                bus = (Bus)LINBus;
                return bus;
            }

            return null;
        }
    }

    class CAN_Bus : Bus
    {
        /// <summary>
        /// Saves the handle of a PCAN hardware
        /// </summary>
        public TPCANHandle m_PcanHandle;
        private CANMsgs m_CANMsg;

        public override Bus.Type BusType
        {
            get { return Type.CAN_BUS; }
        }

        public TPCANHandle PCANHANDLE
        { 
            get { return m_PcanHandle; } 
        }

        private UInt16 ScanDevice()
        {
            TPCANStatus stsResult;
            uint iChannelsCount;
            // Checks for available Plug&Play channels
            //
            stsResult = PCANBasic.GetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_ATTACHED_CHANNELS_COUNT, out iChannelsCount, sizeof(uint));
            if (stsResult == TPCANStatus.PCAN_ERROR_OK)
            {
                TPCANChannelInformation[] info = new TPCANChannelInformation[iChannelsCount];

                stsResult = PCANBasic.GetValue(PCANBasic.PCAN_NONEBUS, TPCANParameter.PCAN_ATTACHED_CHANNELS, info);
                if (stsResult == TPCANStatus.PCAN_ERROR_OK)
                    // Include only connectable channels
                    //
                    foreach (TPCANChannelInformation channel in info)
                        if ((channel.channel_condition & PCANBasic.PCAN_CHANNEL_AVAILABLE) == PCANBasic.PCAN_CHANNEL_AVAILABLE)
                        {
                            TPCANHandle channel_handle = channel.channel_handle;
                            return channel_handle;
                        }
            }
            return 0;

        }

        public override uint Init(ref string strmsg, ushort BaudRate, object canOBJ)
        {
            TPCANStatus strResult;

            m_PcanHandle = ScanDevice();
            if(m_PcanHandle==0)
            {
                strmsg = "No can device connected!";
                Console.WriteLine(strmsg);
                strResult = TPCANStatus.PCAN_ERROR_INITIALIZE;

                return (uint)strResult;
            }

            cCANParams = (CANParam)canOBJ;

            if (cCANParams.bCANFD)
            {
                strResult = PCANBasic.InitializeFD(m_PcanHandle, cCANParams.Bitrate);
            }
            else
            {
                strResult = PCANBasic.Initialize(m_PcanHandle,
                                                                (TPCANBaudrate)BaudRate,
                                                                 cCANParams.HwType, //TPCANType.PCAN_TYPE_ISA,
                                                                 Convert.ToUInt32(cCANParams.IO, 16),
                                                                 Convert.ToUInt16(cCANParams.Interrupt));
                                                                //0x64,
                                                                //0x3); 
            }

            
            strmsg = string.Format("{0:g}", strResult);

            return (uint)strResult;     //base.Init(ref strmsg);
        }

        public override ushort Close()
        {
            TPCANStatus status;
            status = PCANBasic.Uninitialize(m_PcanHandle);

            return (ushort)status;     //base.Close();
        }

        public override int SendMessage(object CanMsg)
        {
            TPCANStatus status;
            m_CANMsg = (CANMsgs)CanMsg;

            status = PCANBasic.Write(m_PcanHandle, ref m_CANMsg.CANMsg);

            return (int)status;      // base.SendMessage(CanMsg);
        }

        public  int SendFDMessage(object CanMsg)
        {
            TPCANStatus status;
            m_CANMsg = (CANMsgs)CanMsg;

            status = PCANBasic.WriteFD(m_PcanHandle, ref m_CANMsg.CANFDMsg);

            return (int)status; 
        }

        public override int ReceiveMessage(out object BusMsg)
        {
            TPCANMsg CANMsg;
            TPCANTimestamp CANTimeStamp;
            TPCANStatus status;

            // We execute the "Read" function of the PCANBasic
            //
            status = PCANBasic.Read(m_PcanHandle, out CANMsg, out CANTimeStamp);
            if (status != TPCANStatus.PCAN_ERROR_QRCVEMPTY/*PCAN_ERROR_OK*/)
            {
                m_CANMsg = new CANMsgs();
                m_CANMsg.CANMsg = CANMsg;
                m_CANMsg.CANTimeStamp = CANTimeStamp;
                m_CANMsg.stsResult = status;
                BusMsg = (object)m_CANMsg;
            }
            else
                BusMsg = null;

            return (int)status;      //base.ReceiveMessage(out BusMsg);
        }

        /// <summary>
        /// Function for reading messages on FD devices
        /// </summary>
        /// <returns>A TPCANStatus error code</returns>
        public int ReadMessageFD(out object BusMsg)
        {
            TPCANMsgFD CANMsg;
            TPCANTimestampFD CANTimeStamp;
            TPCANStatus stsResult;

            // We execute the "ReadFD" function of the PCANBasic                
            //
            stsResult = PCANBasic.ReadFD(m_PcanHandle, out CANMsg, out CANTimeStamp);
            if (stsResult != TPCANStatus.PCAN_ERROR_QRCVEMPTY)
            {
                m_CANMsg = new CANMsgs();
                m_CANMsg.CANFDMsg = CANMsg;
                m_CANMsg.CANFDTimeStamp = CANTimeStamp;
                m_CANMsg.stsResult = stsResult;
                BusMsg = (object)m_CANMsg;
            }
            else
                BusMsg = null;

            return (int)stsResult;
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
            if (PCANBasic.GetErrorText(error, 0, strTemp) != TPCANStatus.PCAN_ERROR_OK)
                return string.Format("An error occurred. Error-code's text ({0:X}) couldn't be retrieved", error);
            else
                return strTemp.ToString();
        }

        /// <summary>
        /// Gets the current status of the PCAN-Basic message filter
        /// </summary>
        /// <param name="status">Buffer to retrieve the filter status</param>
        /// <param name="ifErr">Out error string if any</param>
        /// <returns>If calling the function was successfull or not</returns>
        private bool GetFilterStatus(out uint status, out string ifErr)
        {
            TPCANStatus stsResult;

            // Tries to get the sttaus of the filter for the current connected hardware
            //
            stsResult = PCANBasic.GetValue(PCANHANDLE, TPCANParameter.PCAN_MESSAGE_FILTER, out status, sizeof(UInt32));

            // If it fails, a error message is shown
            //
            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
            {
                ifErr = GetFormatedError(stsResult);
                return false;
            }

            ifErr = string.Empty;
            return true;
        }

        /// <summary>
        /// Message fillter
        /// <paramref name="uReq"/>request message id
        /// <paramref name="uResp"/>response message id
        /// <paramref name="ifErr"/>error message if any when set message fillter on CAN bus
        /// <paramref name="bExtendFrm"/>message fillter will process STANDARD/EXTENDED frame?
        /// <paramref name="bClose"/>does open can id fillter or not?
        /// </summary>
        public TPCANStatus MessageFillter(uint uReq, uint uResp, out string ifErr, bool bExtendFrm = false, bool bClose = false)
        {
            UInt32 iBuffer;
            TPCANStatus stsResult = TPCANStatus.PCAN_ERROR_UNKNOWN;

            // Gets the current status of the message filter
            //
            if (!GetFilterStatus(out iBuffer, out ifErr))
                return stsResult;

            if (bClose) 
            {
                 // Sets the custom filter
                //
                stsResult = PCANBasic.FilterMessages(m_PcanHandle,
                                                                        uReq,
                                                                        uResp,
                                                                        bExtendFrm ?  TPCANMode.PCAN_MODE_EXTENDED: TPCANMode.PCAN_MODE_STANDARD );
                return stsResult;
            }

            // The filter will be full opened or complete closed
            //
            if (!bClose)
                iBuffer = PCANBasic.PCAN_FILTER_OPEN;

            // The filter is configured
            //
            stsResult = PCANBasic.SetValue(m_PcanHandle,
                                                            TPCANParameter.PCAN_MESSAGE_FILTER,
                                                            ref iBuffer,
                                                            sizeof(UInt32));

            if (stsResult != TPCANStatus.PCAN_ERROR_OK)
                ifErr = GetFormatedError(stsResult);

            return stsResult;
        }
    }

    class LIN_Bus : Bus
    {
        USB_DEVICE.DEVICE_INFO m_DevInfo;

        Int32[] m_DevHandles;
        Int32 m_DevHandle;
        
        Int32 m_LINDevNum;
        LINMsg m_lin_msg;
        bool m_state;
        byte[] m_resp;
        /// <summary>
        /// LIN adapter connect state
        /// </summary>
        public bool State
        { get { return m_state; } }

        /// <summary>
        /// LIN device info handle
        /// </summary>
        public USB_DEVICE.DEVICE_INFO DevInfo
        { get { return m_DevInfo; } }

        public byte[] RespData
        {
            set { m_resp = value; }
            get { return m_resp; }
        }

        public int DeviceHandle
        { 
            get { return m_DevHandle; } 
        }

        public override Bus.Type BusType
        {
            get { return Type.LIN_BUS; }
        }

        public override uint Init(ref string strmsg, ushort BaudRate, object canOBJ = null)
        {
            // Sets the connection status of the main-form
            //
            ushort bResult = 1;
            int nInit = 1;
            int nBaudRate = 0;
     
            try
            {
                m_DevHandles = new Int32[10];
                m_LINDevNum = USB_DEVICE.USB_ScanDevice(m_DevHandles); //扫描查找设备
                if (m_LINDevNum <= 0)
                {
                    strmsg = "No lin device connected!";
                    Console.WriteLine(strmsg);

                    return bResult;
                }
                else
                {
                    bResult = 0;
                    strmsg = String.Format("Have {0} lin device connected!", m_LINDevNum);
                    Console.WriteLine(strmsg);

                    m_lin_msg = new LINMsg();
                    m_DevHandle = m_DevHandles[0];
                }

                m_state = USB_DEVICE.USB_OpenDevice(m_DevHandle);  //打开设备
                if (!m_state)
                {
                    strmsg = "Open lin device error!";
                    Console.WriteLine(strmsg);
                    return bResult;
                }
                else
                {
                    bResult = 0;
                    strmsg = "Open lin device success!";
                    Console.WriteLine(strmsg);
                }

                switch(BaudRate)
                {
                    case (ushort)TPCANBaudrate.PCAN_BAUD_20K:
                        nBaudRate = 19200;
                        break;
                    case (ushort)TPCANBaudrate.PCAN_BAUD_10K:
                        nBaudRate = 14400;
                        break;
                    case (ushort)TPCANBaudrate.PCAN_BAUD_5K:
                        nBaudRate = 9600;
                        break;

                    default:
                        nBaudRate = 19200;
                        break;
                }

                nInit = USB2LIN_EX.LIN_EX_Init(m_DevHandle, 0, nBaudRate, 1); //初始化设备
                if (nInit != 0)
                {                  
                    strmsg = "Init lin device error!";
                    Console.WriteLine(strmsg);

                    return bResult;
                }
                else
                {
                    bResult = 0;
                    m_DevInfo = new USB_DEVICE.DEVICE_INFO();

                    strmsg = "Init device success!";
                    Console.WriteLine(strmsg);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Device not connect::%s" + ex.Message);
            }

            return bResult; //base.Init(ref strmsg);
        }

        public override ushort Close()
        {
            ushort uStatus = 1;
            // Releases a current connected PCAN-Basic channel
            //
            bool bClosed = USB_DEVICE.USB_CloseDevice(m_DevHandle);
            if (bClosed)
                uStatus = 0;

            return uStatus;     //base.Close();
        }

        public override int SendMessage(object LinMsg)
        {
            int nRet = -1;

            m_lin_msg = (LINMsg)LinMsg;
            nRet = LIN_UDS.LIN_UDS_Request(m_DevHandle, 0, ref m_lin_msg.lin_uds_addr, m_lin_msg.data, m_lin_msg.data.Length);

            return nRet;        // base.SendMessage(LinMsg);
        }

        public override int ReceiveMessage(out object BusMsg, int nMsgLen=8)
        {
            int nRet = -1;
            
             m_lin_msg.lin_ex_msg.Data = new byte[nMsgLen];//for receive DID response msg, which max length of response msg is "DID 0xF0B4"
            nRet = LIN_UDS.LIN_UDS_Response(m_DevHandle, 0, ref m_lin_msg.lin_uds_addr, m_lin_msg.lin_ex_msg.Data, m_lin_msg.WaitTime);
            
            if (nRet > 0)
                BusMsg = (object)m_lin_msg;
            else
                BusMsg = null;

            return nRet;        // base.ReceiveMessage(out BusMsg);
        }
    }
}
