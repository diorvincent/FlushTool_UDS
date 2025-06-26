using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using USB2XXX;
using Peak.Can.Basic;
using TPCANTimestampFD = System.UInt64;

namespace Diag_BUS
{
    class LINMsg
    {       
        public int ID;
        public int DLC;
        public string Dir;
        public byte[] data; //for Tx
        public int WaitTime;
        public LIN_UDS.LIN_UDS_ADDR lin_uds_addr;
        public USB2LIN_EX.LIN_EX_MSG lin_ex_msg; //for Rx

        public string msgID;
        public string MsgLen;
        public string Count;
        public string TimeString;
        public string DataString;

        public LINMsg() { }
        public LINMsg(string id, string dir, string len, string count, string Timestring, string Datastring)
        {
            msgID = id;
            Dir = dir;
            MsgLen = len;
            Count = count;
            TimeString = Timestring;
            DataString = Datastring;
        }
    }

    class CANMsgs
    {
        public uint ID;
        public string Dir;
        public TPCANMsg CANMsg;   
        public TPCANTimestamp CANTimeStamp;
        public TPCANStatus stsResult;

        public TPCANMsgFD CANFDMsg;
        public TPCANTimestampFD CANFDTimeStamp;

        public string msgID;
        public string MsgLen;
        public string Count;
        public string TimeString;
        public string DataString;

        public CANMsgs() { }
        public CANMsgs(string id, string dir, string len, string count, string Timestring, string Datastring) 
        {
            msgID = id;
            Dir = dir;
            MsgLen = len;
            Count = count;
            TimeString = Timestring;
            DataString = Datastring;
        }
    }
}
