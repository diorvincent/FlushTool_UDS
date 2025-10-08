/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Nov 1, 2023
 * Author: He Jingchi
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Diag_BUS
{
    /// <summary>
    /// Message Status structure used to show CAN Messages
    /// </summary>
    class MessageStatus
    {
        private LINMsg m_Msg;
        private CANMsgs m_MsgCAN;
        private String m_TimeStamp;
        private String m_oldTimeStamp;
        private int m_iIndex;
        private int m_Count;
        private bool m_bShowPeriod;
        private bool m_bWasChanged;
        private bool m_bFD;

        public MessageStatus(LINMsg linMsg, String canTimestamp, int listIndex)
        {
            m_Msg = linMsg;
            m_TimeStamp = canTimestamp;
            m_oldTimeStamp = canTimestamp;
            m_iIndex = listIndex;
            m_Count += 1;
            m_bShowPeriod = true;
            m_bWasChanged = false;
        }

        public MessageStatus(CANMsgs canMsg, String canTimestamp, int listIndex, bool isFD = false)
        {
            m_MsgCAN = canMsg;
            m_TimeStamp = canTimestamp;
            m_oldTimeStamp = canTimestamp;
            m_iIndex = listIndex;
            m_Count += 1;
            m_bShowPeriod = true;
            m_bWasChanged = false;
            m_bFD = isFD;
        }

        public void Update(LINMsg canMsg = null, String canTimestamp = "")
        {
            //m_Msg = canMsg;
            //m_oldTimeStamp = m_TimeStamp;
            //m_TimeStamp = canTimestamp;
            m_bWasChanged = true;
            m_Count += 1;
        }

        public CANMsgs CANMsg
        {
            get { return m_MsgCAN; }
        }
        public LINMsg LINMsg
        {
            get { return m_Msg; }
        }

        public String Timestamp
        {
            get { return m_TimeStamp; }
        }

        public int Position
        {
            get { return m_iIndex; }
        }

        public string IdString
        {
            get { return GetIdString(); }
        }

        public string DataString
        {
            get { return GetDataString(); }
        }

        public int Count
        {
            get { return m_Count; }
        }

        public bool ShowingPeriod
        {
            get { return m_bShowPeriod; }
            set
            {
                if (m_bShowPeriod ^ value)
                {
                    m_bShowPeriod = value;
                    m_bWasChanged = true;
                }
            }
        }

        public bool MarkedAsUpdated
        {
            get { return m_bWasChanged; }
            set { m_bWasChanged = value; }
        }

        public string TimeString
        {
            get { return GetTimeString(); }
        }

        private string GetTimeString()
        {
            return m_TimeStamp;
        }

        private string GetDataString()
        {
            string strTemp = "";
            if(m_Msg != null)
            {
                for (int i = 0; i < m_Msg.DLC /*Diag_LIN.GetLengthFromDLC(m_Msg.DLC, false)*/; i++)
                    strTemp += string.Format("{0:X2} ", m_Msg.data[i]);
            }
            else if(m_MsgCAN != null)
            {
                if (!m_bFD)
                {
                    for (int i = 0; i < Diag_LIN.GetLengthFromDLC(m_MsgCAN.CANMsg.LEN, false); i++)
                        strTemp += string.Format("{0:X2} ", m_MsgCAN.CANMsg.DATA[i]);
                }
                else
                {
                    for (int i = 0; i < Diag_LIN.GetLengthFromDLC(m_MsgCAN.CANFDMsg.DLC, false); i++)
                        strTemp += string.Format("{0:X2} ", m_MsgCAN.CANFDMsg.DATA[i]);
                }
            }

            return strTemp;
        }

        private string GetIdString()
        {
            // We format the ID of the message and show it
            //
            if (m_Msg != null)
                return string.Format("0x{0:X3}", m_Msg.ID);
            else if(m_MsgCAN != null)
            {
                if ((m_MsgCAN.CANMsg.MSGTYPE & Peak.Can.Basic.TPCANMessageType.PCAN_MESSAGE_STANDARD) == 0)
                {
                    if (!m_bFD)
                        return string.Format("0x{0:X3}", m_MsgCAN.CANMsg.ID);
                    else
                        return string.Format("0x{0:X3}", m_MsgCAN.CANFDMsg.ID);
                }
                else
                {
                    if (!m_bFD)
                        return string.Format("{0:X8}h", m_MsgCAN.CANMsg.ID);
                    else
                        return string.Format("{0:X8}h", m_MsgCAN.CANFDMsg.ID);
                }
            }
                            
            return string.Empty;
        }
    }
}
