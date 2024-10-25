/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Mar 17, 2024
 * Updated on: Apr 11, 2024(add N2S project download functions "CAN_Flashing")
 * Updated on: Apr 24, 2024(add CANUDS standard dowload class "CANUDS_Flahing")
 * Updated on: May 25, 2024(add .hex file flash class on LIN "LIN_Flashing_Hex")
 * Updated on: Jun 3, 2024(add split flash class "Split_CANUDS_Flahing" )
 * Author: He Jingchi
 * Design pattern:  Strategy：Solve the problem of performing different actions in one or more methods depending on the situation
 */

using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Diag_BUS
{
    internal class FlashBase
    {
        public UInt32 CAN_ADDR = 0x08009000;
        public UInt32 CAN_SIZE = 0x00017000;
        public UInt32 MASK = 0x7D3EFD82;
        public UInt32 CANUDS_MASK = 0xEDB88320;
        public bool m_b1stFrm = false;
        public int m_n36SvrPackNum = 0;
        public int P2_ServerTime = 30;
        public int PACK_SIZE = 0;

        protected int m_nx36PackIntervalTimer = 5;
        /// <summary>
        /// process 0x36 data
        /// </summary>
        protected struct N2S_DataPack
        {
            public int nMaxNumOfBlock;
            public byte[] All0x36PackData;
        };
        protected N2S_DataPack m_N2SDataPack;
        /// <summary>
        /// waitting for 0x31 response
        /// </summary>
        public const int RESP_0x31_WAITTING_TIME = 6500;
        /// <summary>
        /// 0x3E service request message interval
        /// </summary>
        public const int REQ_3E_INTERVAL = 3000; //100;
        public Diag_LIN gDiag_Lin { get; set; }
        protected const int HEX_DATA_SIZE = 0x1700;
        public FlashBase() { }
        ~FlashBase() { }

        #region delegate for download thread

        public delegate bool FlashFirewareHandler(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize);
        public delegate bool N2S_FlashFirewareHandler(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack);
        public delegate int FlashFirewareHandlerForTP90(byte[] buff, int nCurrIndex, int n0x36PackCnt);

        #endregion


        public virtual bool WriteThreadFunc_TH(object diag_lin) { gDiag_Lin = (Diag_LIN)diag_lin;  return true; }
        protected virtual void UpgrateFirmware(object o0x36DataPack) { }
        protected virtual bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize) { return false; }
        protected virtual bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack) { return false; }
        protected virtual int FlashFirmware_TH_TP90(byte[] buffer, int nCurrIndex, int n0x36PackCnt) { return 1; }
        protected virtual bool Download_Finish() { return false; }
        protected virtual int Send34Request(uint startAddr, uint dataLen, int nRequestTime) { return 1; }
        protected virtual bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00) { return false; }        

        ///<summary>
        ///Write message to CAN bus
        ///<param name="msgs"/> diag request message</param>
        ///<param name="bSingleFrame">does frame data single or not?</param>
        ///<param name="b36Req">does request id is 0x36</param>
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
                    new_msg = gDiag_Lin.Combine(byte0, msgs);

                    // Send the message
                    nResult = gDiag_Lin.Write_Message(new_msg);
                    Thread.Sleep(P2_ServerTime);       // (m_nx36PackIntervalTimer);      
                }
                else
                {
                    //1st frame
                    {
                        nDatalen = msgs.Length;
                        svrID = msgs[0];

                        loBitDataLen = ((nDatalen >> 8) & 0x0F);
                        new_msg0[0] = Convert.ToByte(loBitDataLen + 0x10);
                        bReConter36 = true;

                        if (b36Req)
                        {
                            if (nLastMsgByteCount == 0x80)
                            {
                                hiBitDataLen = (PACK_SIZE + 2) & 0xFF;
                            }
                            else if (nLastMsgByteCount == 0x102)
                            {
                                hiBitDataLen = PACK_SIZE & 0xFF;
                            }
                            else
                                hiBitDataLen = (nLastMsgByteCount + 2) & 0xFF;
                        }
                        else
                            hiBitDataLen = (nDatalen & 0x00FF);

                        new_msg0[1] = Convert.ToByte(hiBitDataLen);

                        //copy 1st frame residue bytes(except 1st frame mark and frame length 2 byte)
                        for (k = 0; k < (new_msg.Length - new_msg0.Length); k++)
                            new_msg1[k] = msgs[k];
                        new_msg = gDiag_Lin.Combine(new_msg0, new_msg1);

                        // Send the message
                        nResult = gDiag_Lin.Write_Message(new_msg);

                        if (b36Req)
                        {
                            Thread.Sleep(m_nx36PackIntervalTimer);
                            //if( -1==gDiag_Lin.N2S_ProcessFollowCtrl(0x36))
                            //{
                            //    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Issue occured when process FollowCtrl message.")); }));
                            //    //return -1;
                            //}
                        }
                        else
                            Thread.Sleep(P2_ServerTime);
                    }
                    //backward frame
                    {
                        byte BlockSize = 7; //singel frame 1st byte is sequence number,so backfoward data len is 7
                        byte[] new_msgX;

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
                                nSingleFrmOfOneBlockSent = gDiag_Lin.Write_Message(new_msgX);
                                nResult = nSingleFrmOfOneBlockSent;

                                Console.Write("0x36 svr block{0:d},result{1:d}", x, nSingleFrmOfOneBlockSent);
                                //waitting for incoming message
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
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
                                gDiag_Lin.Write_Message(EndBytes);
                                nEndBytes = 0;
                                
                            }
                        }
                        else 
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = fConvert.CheckSum(msgs); //msgs;  //
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
                            }
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
                                nSingleFrmOfOneBlockSent = gDiag_Lin.Write_Message(new_msgX);
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
                                nResult = gDiag_Lin.Write_Message(EndBytes);
                                nEndBytes = 0;
                            }
                        }
                        else
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = msgs;
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
                            for (x = 0; x < n7ByteGroups; x++)
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                            }
                            if (nEndBytes > 0) //tail block(less than 7 bytes)
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
                                for (; p < nEndBytes; p++)
                                {
                                    if (k < msgs.Length)
                                        new_msgX[p + 1] = msgs[k++];
                                }

                                // Send the message
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                            }
                            #endregion

                        }

#endif
                    }
                }
            }
            catch (Exception ex)
            {
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited. {0}", ex.Message)); }));
            }

            return nResult;
        }


        ///<summary>
        ///Write message to LIN bus
        ///<param name="msgs"/> diag request message</param>
        ///<param name="bSingleFrame">does frame data single or not?</param>
        ///<param name="b36Req">does request id is 0x36</param>
        ///<param name="nLastMsgByteCount">use to make last block length while whole data length not be 0x80 divide</param>
        ///</summary>
        public int Write_LINMessage(byte[] msgs, bool bSingleFrame = false, bool b36Req = false, int nLastMsgByteCount = 0x80)
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
                    new_msg = gDiag_Lin.Combine(byte0, msgs);

                    // Send the message
                    if(b36Req)
                        nResult = gDiag_Lin.Write_Message(new_msg);
                    else
                        nResult = gDiag_Lin.Write_Message(msgs);
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
                        bReConter36 = true;

                        if (b36Req)
                        {
                            if (nLastMsgByteCount == 0x80)
                            {
                                hiBitDataLen = (PACK_SIZE + 2) & 0xFF;
                            }
                            else if (nLastMsgByteCount == 0x102)
                            {
                                hiBitDataLen = PACK_SIZE & 0xFF;
                            }
                            else
                                hiBitDataLen = (nLastMsgByteCount + 2) & 0xFF;
                        }
                        else
                            hiBitDataLen = (nDatalen & 0x00FF);

                        new_msg0[1] = Convert.ToByte(hiBitDataLen);

                        //copy 1st frame residue bytes(except 1st frame mark and frame length 2 byte)
                        for (k = 0; k < (new_msg.Length - new_msg0.Length); k++)
                            new_msg1[k] = msgs[k];
                        new_msg = gDiag_Lin.Combine(new_msg0, new_msg1);

                        // Send the message
                        nResult = gDiag_Lin.Write_Message(new_msg);

                        if (b36Req)
                            Thread.Sleep(m_nx36PackIntervalTimer - 1);
                        else
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
                                nSingleFrmOfOneBlockSent = gDiag_Lin.Write_Message(new_msgX);
                                nResult = nSingleFrmOfOneBlockSent;

                                Console.Write("0x36 svr block{0:d},result{1:d}", x, nSingleFrmOfOneBlockSent);
                                //waitting for incoming message
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
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
                                gDiag_Lin.Write_Message(EndBytes);
                                nEndBytes = 0;
                                
                            }
                        }
                        else 
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = fConvert.CheckSum(msgs); //msgs;  //
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                                
                            }
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
                                nSingleFrmOfOneBlockSent = gDiag_Lin.Write_Message(new_msgX);
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
                                nResult = gDiag_Lin.Write_Message(EndBytes);
                                nEndBytes = 0;
                            }
                        }
                        else
                        {
                            //caculate checksum byte
                            int nEndBytes = 0;
                            byte[] checksum = msgs;
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
                            for (x = 0; x < n7ByteGroups; x++)
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
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                            }
                            if (nEndBytes > 0) //tail block(less than 7 bytes)
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
                                for (; p < nEndBytes; p++)
                                {
                                    if (k < msgs.Length)
                                        new_msgX[p + 1] = msgs[k++];
                                }

                                // Send the message
                                nResult = gDiag_Lin.WriteFrame(new_msgX);
                                Thread.Sleep(m_nx36PackIntervalTimer);
                            }
                            #endregion

                        }

#endif
                    }
                }
            }
            catch (Exception ex)
            {
                gDiag_Lin.IncludeTextMessage(string.Format("Issue occured when send message{0}", ex.Message));
                //Console.WriteLine(string.Format("Issue occured when send message{0}", ex.Message));
            }

            return nResult;
        }
    }

    internal class CAN_Flashing : FlashBase 
    {
        #region member variabels

        byte[] m_ReqMsg;
        int gCurrPackPos;
        
        #endregion

        public CAN_Flashing() 
        {
        }
        ~CAN_Flashing() { }

        public override bool WriteThreadFunc_TH(object diag_lin) 
        {
            gDiag_Lin = (Diag_LIN)diag_lin;

            int nSendResult = -1;
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {           
                gCurrPackPos = 0;   //reset 0x36 sent package counter
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                nSendResult = Write_CANMessage(m_ReqMsg, true);

                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);

                if (bGetPositiveResp)
                {
                    //Enable TestPresent 0x3E  & message view rolling
                    m_ReqMsg = new byte[] { 0x3E, 0x00 };
                    Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(100);
                    if (gDiag_Lin.m_RespMsg[1] == 0x7E && gDiag_Lin.m_RespMsg[2] == 0x00)
                    {
                        m_ReqMsg = new byte[] { 0x3E, 0x80 };
                        Write_CANMessage(m_ReqMsg, true);
                        Thread.Sleep(100);

                        lock (this)
                        {
                            gDiag_Lin.m_bEnable_0x3E = true;
                        }
                    }
                    else
                    {
                        gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                        return false;
                    }

#if _SecurityAccess

                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(150);
                    if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x01) //Not support 0x27 service now.
                    {
                        byte[] SeedArray = new byte[4];
                        byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                        for (int i = 0; i < SeedArray.Length; i++)
                            SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
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
                        if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x02)
#endif
                        {
                            gDiag_Lin.IncludeTextMessage("Security access pass.");

                            //Earse command
                            m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                            //Memory address MEMORY_ADDR, MEMORY_SIZE
                            string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR, 16);
                            byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                            //Memory size
                            string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                            byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                            byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                            byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                            //Earse whole command
                            nSendResult = Write_CANMessage(EraseMemory);
                            gDiag_Lin.IncludeTextMessage("Now earsing flash,please wait for amoument...");

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
                            bGetPositiveResp = Resp_TH(ref nBlocks, 100, 0, 0x31);
                            if (bGetPositiveResp)
                            {
                                gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                gDiag_Lin.IncludeTextMessage("System will download application file.");

                                bMainFlashOK = Download_Finish();
                                if (bMainFlashOK)
                                    gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                else
                                    gDiag_Lin.IncludeTextMessage("Dowload has interupted.");
                            }
                            else
                            {
                                gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                return false;
                            }
#if _SecurityAccess
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
#endif
                    }
                }
                else
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);


                //Back flashing step
                if (bMainFlashOK)
                {
                    //m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x01 }; //CheckProgrammingDependencies
                    //Write_CANMessage(m_ReqMsg, true);

                    int nBlockNum = 0;
                    //bGetPositiveResp = false;
                    //bGetPositiveResp = canResp_TH(ref nBlockNum, 10, 0, 0x31);

                    //if (gDiag_Lin.m_RespMsg[1] == 0x71 && gDiag_Lin.m_RespMsg[2] == 0x01/*gDiag_Lin.m_RespMsg[4] == 0x04*/)
                    {
                        gDiag_Lin.IncludeTextMessage("Application file down succeed.");
                        gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x03 }; //ECU reset(SoftReset)
                        Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 300, 0, 0x11);
                        if (bGetPositiveResp)
                        {
                            gDiag_Lin.IncludeTextMessage("ECU hard reset succeed.");

                            gDiag_Lin.SetDonwloadingStatus(true);
                            gDiag_Lin.RefreshDBGridView();

                            //m_ReqMsg = new byte[] { 0x14, 0xFF, 0xFF, 0xFF }; // Clear dianostic info
                            //Write_CANMessage(m_ReqMsg, true);

                            gDiag_Lin.IncludeTextMessage("Fireware download succeed.");
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);

                    }
                    //else if (m_RespMsg[1] == 0x71 && m_RespMsg[4] == 0x05)
                    //    NegativeMessage(0x31, m_RespMsg);
                }
                else
                    gDiag_Lin.m_bEnable_0x3E = false;

                return true;
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
                return false;
            }

        }

        protected override void UpgrateFirmware(object nMaxBlockSize) 
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
            nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[gCurrPackPos].uRecordLength);
            if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
            {
                nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                //bHasRemainder = false;
            }
            else
            {
                nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                //bHasRemainder = true;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            FlashFirewareHandler ffHandler = new FlashFirewareHandler(FlashFirmware);
            lock (Diag_LIN.m_obj)
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
                        gDiag_Lin.BeginInvoke(ffHandler, new object[] { x, n0x36PackNum++, nBlockNum });

                        while (!gDiag_Lin.m_b36SvrOneBlockOver)
                        {
                            Thread.Sleep(10);
                        }
                        gDiag_Lin.m_b36SvrOneBlockOver = false;
#if _CheckSum
                        Thread.Sleep(150);
#else
                        Thread.Sleep(10);
#endif

                        //wait for single block write response(0x36)
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 50, x + 1, 0x36); }));
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x)); }));

                        if (!bGetPositiveResp)
                        {
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
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
                    gDiag_Lin.m_bTransferDataOK = false;
            }
        }
        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize)
        {
            //sending data
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };

            nCurrPackPos = nCurrIndex;
            nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
            if (nCurrIndex < nMaxBlockSize - 1)
            {
                for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                    DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                gDiag_Lin.UpdateProgerss(nProgress);
            }
            else //last package size will not equal PackSize
            {
                int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                    DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                nLastMsgByteCount = DataBuffer.Length;

                gDiag_Lin.UpdateProgerss(100);
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            //single block 0x36 data package sent
            gDiag_Lin.m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return true;
        }
        protected override bool Download_Finish() 
        {
            gDiag_Lin.IncludeTextMessage("Security access pass.");
            //foreach (HexParser.RecordAddrInfo rdi in m_RecInfo)//mutiple block need embended program support
            {
                //   MEMORY_ADDR = rdi.uBaseAddress;
                //   MEMORY_SIZE = (uint)rdi.uRecordLength;

                Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);
                Thread.Sleep(30);

                int nMaxNumOfBlock = 0;
                bool bGetPositiveResp = false;
                bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 200, 0, 0x34);
                if (bGetPositiveResp)
                {
                    gDiag_Lin.IncludeTextMessage("Data transfer start.");
                    try
                    {
                        gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                        gDiag_Lin.m_WriteThread.IsBackground = true;
                        gDiag_Lin.m_WriteThread.Start(nMaxNumOfBlock);

                        bool IfTimesEnd = false;
                        bool IfRunOver = false;
                        while (!IfRunOver && gDiag_Lin.m_WriteThread != null)
                        {
                            IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                            Application.DoEvents();
                            if (!IfTimesEnd || IfRunOver || !gDiag_Lin.m_bTransferDataOK)
                            {
                                gDiag_Lin.m_WriteThread.Interrupt();
                                gDiag_Lin.m_WriteThread.Abort();
                                IfTimesEnd = false;
                                gDiag_Lin.gAddrOffset = 0;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                    }
                }
                else
                    gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);

                gCurrPackPos++;
            }

            if (gDiag_Lin.m_bTransferDataOK)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(30);
                if (gDiag_Lin.m_RespMsg[1] == 0x77)
                {
                    gDiag_Lin.IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    lock (this)
                    { gDiag_Lin.m_bEnable_0x3E = false; }
                    return true;
                }
                else
                    gDiag_Lin.NegativeMessage(0x37, gDiag_Lin.m_RespMsg);
            }

            return false;
        }
        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime) 
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req); 
        }
        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00) 
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
                gDiag_Lin.N2S_ProcessFollowCtrl((byte)reqID);

                if (reqID == 0x31)
                {
                    Thread.Sleep(300);
                    gDiag_Lin.ReadMessage(ref resp);
                }
                else if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else
                {
                    resp = gDiag_Lin.m_RespMsg;
                }

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
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response read/write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x6E || resp[2] == 0x62) //response read/write DID
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

    }

    internal class N2S_CAN_Flahing : FlashBase
    {
        #region member variabels

        byte[] m_ReqMsg;
        int gCurrPackPos;

        #endregion

        public N2S_CAN_Flahing()
        {
            
        }

        ~N2S_CAN_Flahing() { }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] respMsg = new byte[8];

            try
            {
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_bEnable_0x3E = false;
                gCurrPackPos = 0;   //reset 0x36 sent package counter 
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                nSendResult = Write_CANMessage(m_ReqMsg, true);
                bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                if (bGetPositiveResp)
                {
#if _SecurityAccess

                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(3 * P2_ServerTime);

                    if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x01) //Not support 0x27 service now.
                    {
                        byte[] SeedArray = new byte[4];
                        byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                        for (int i = 0; i < SeedArray.Length; i++)
                            SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                        fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                        m_ReqMsg = new byte[6];
                        m_ReqMsg[0] = 0x27;
                        m_ReqMsg[1] = 0x02;
                        m_ReqMsg[2] = KeyArray[0];
                        m_ReqMsg[3] = KeyArray[1];
                        m_ReqMsg[4] = KeyArray[2];
                        m_ReqMsg[5] = KeyArray[3];

                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                        Thread.Sleep(2 * P2_ServerTime);
                        if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x02)
#endif
                        {
                            m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5, 0, 0x10);
                            if (bGetPositiveResp)
                            {
                                //Enable TestPresent 0x3E  & message view rolling
                                m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                Write_CANMessage(m_ReqMsg, true);
                                Thread.Sleep(2 * P2_ServerTime);
                                if (gDiag_Lin.m_RespMsg[1] == 0x7E && gDiag_Lin.m_RespMsg[2] == 0x00)
                                {
                                    m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                    Write_CANMessage(m_ReqMsg, true);
                                    Thread.Sleep(2 * P2_ServerTime);

                                    lock (this)
                                    {
                                        gDiag_Lin.m_bEnable_0x3E = true;
                                    }
                                }
                                else
                                {
                                    gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                    return false;
                                }

#if _SecurityAccess
                                m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                                nSendResult = Write_CANMessage(m_ReqMsg, true);
                                Thread.Sleep(3 * P2_ServerTime);
                                if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x01)
                                {
                                    SeedArray = new byte[4];
                                    KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                    for (int i = 0; i < SeedArray.Length; i++)
                                        SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                                    fConvert.N2S_seedToKey(SeedArray, out KeyArray, 1);   //according response seed caculate security access key

                                    m_ReqMsg = new byte[6];
                                    m_ReqMsg[0] = 0x27;
                                    m_ReqMsg[1] = 0x02;
                                    m_ReqMsg[2] = KeyArray[0];
                                    m_ReqMsg[3] = KeyArray[1];
                                    m_ReqMsg[4] = KeyArray[2];
                                    m_ReqMsg[5] = KeyArray[3];

                                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                                    Thread.Sleep(2 * P2_ServerTime);
                                    if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x02)
#endif
                                    {
#if _SecurityAccess
                                        m_ReqMsg = new byte[] { 0x27, 0x09 }; //Security access,request seed
                                        nSendResult = Write_CANMessage(m_ReqMsg, true);
                                        Thread.Sleep(3 * P2_ServerTime);
                                        if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x09)
                                        {
                                            SeedArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                            KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                            for (int i = 0; i < SeedArray.Length; i++)
                                                SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                                            fConvert.N2S_seedToKey(SeedArray, out KeyArray, 9);   //according response seed caculate security access key

                                            m_ReqMsg = new byte[6];
                                            m_ReqMsg[0] = 0x27;
                                            m_ReqMsg[1] = 0x0A;
                                            m_ReqMsg[2] = KeyArray[0];
                                            m_ReqMsg[3] = KeyArray[1];
                                            m_ReqMsg[4] = KeyArray[2];
                                            m_ReqMsg[5] = KeyArray[3];

                                            nSendResult = Write_CANMessage(m_ReqMsg, true);
                                            Thread.Sleep(2 * P2_ServerTime);
                                            if (gDiag_Lin.m_RespMsg[1] == 0x67 && gDiag_Lin.m_RespMsg[2] == 0x0A)
#endif
                                            {
                                                gDiag_Lin.IncludeTextMessage("Security access pass.");

                                                //write DIDs value(F15A) in Programing session
                                                bool bDID_Right = false;
                                                string strIniFile;
                                                byte[] writeDID = new byte[3] { 0x2E, 0xF1, 0x5A };

                                                strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
                                                bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "F15A", writeDID, 9, 1);
                                                if (!bDID_Right)
                                                {
                                                    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                                    return false;
                                                }
                                                else
                                                {
                                                    gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                                    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                                    gDiag_Lin.EN_DIS_WriteDID_Button(true);
                                                }
                                                //-

                                                //Earse command
                                                m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x00 };
                                                //Memory address MEMORY_ADDR, MEMORY_SIZE
                                                string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR, 16);
                                                byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                                                //Memory size
                                                string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                                                byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                                                byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                                                byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                                                //Earse whole command
                                                nSendResult = Write_CANMessage(EraseMemory);
                                                gDiag_Lin.IncludeTextMessage("Now earsing flash,please wait for amoument...");

                                                int nWaitTime = 0;
                                                m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                                while (nWaitTime * REQ_3E_INTERVAL < RESP_0x31_WAITTING_TIME) //earsing need expenditure about 6000ms
                                                {
                                                    Write_CANMessage(m_ReqMsg, true);
                                                    Thread.Sleep(REQ_3E_INTERVAL);
                                                    nWaitTime++;
                                                }

                                                int nBlocks = 0;
                                                bGetPositiveResp = false;
                                                bGetPositiveResp = Resp_TH(ref nBlocks, 100, 0, 0);
                                                if (bGetPositiveResp)
                                                {
                                                    gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                                    gDiag_Lin.IncludeTextMessage("System will download application file.");

                                                    bMainFlashOK = Download_Finish();
                                                    if (bMainFlashOK)
                                                        gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                                    else
                                                        gDiag_Lin.IncludeTextMessage("Dowload has be interupted.");
                                                }
                                                else
                                                {
                                                    gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                                    gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                                    return false;
                                                }
                                            }
                                        }
                                        else
                                            gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);

#if _SecurityAccess
                                    }
                                    else
                                        gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
#endif
                                }
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);

                            //Back flashing step
                            if (bMainFlashOK)
                            {
                                //caculate checksum by c dll func
                                int y = 0;
                                byte[] CheckSumR = new byte[4];
                                uint cCheckSum = fConvert.N2S_CheckSum(gDiag_Lin.m_Total36Data, gDiag_Lin.N2S_MASK);
                                byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);
                                //big ending convert
                                for (int z = bCheckSum.Length - 1; z >= 0; z--)
                                    CheckSumR[y++] = bCheckSum[z];

                                m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                                Byte[] n2s_checksum = gDiag_Lin.Combine(m_ReqMsg, CheckSumR);
                                Write_CANMessage(n2s_checksum);

                                int nBlockNum = 0;
                                bGetPositiveResp = false;
                                bGetPositiveResp = Resp_TH(ref nBlockNum, 30, 0, 0x31);
                                if (bGetPositiveResp)
                                {
                                    gDiag_Lin.IncludeTextMessage("All of transfer hex data consistency check pass!");
                                    gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                                    m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset(SoftReset)
                                    Write_CANMessage(m_ReqMsg, true);

                                    bGetPositiveResp = Resp_TH(ref nBlockNum, 300, 0, 0x11);
                                    if (bGetPositiveResp)
                                    {
                                        gDiag_Lin.RefreshDBGridView();
                                        gDiag_Lin.SetDonwloadingStatus(false);

                                        gDiag_Lin.IncludeTextMessage("ECU hard reset succeed.");
                                        gDiag_Lin.IncludeTextMessage("Fireware download succeed.");
                                    }
                                    else
                                        gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);

                                }
                                else
                                    gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                            }
                            else
                                gDiag_Lin.m_bEnable_0x3E = false;
                        }
                    }
                }
                return true;
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
                return false;
            }
        }

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte number<paramref >
        /// </summary>
        protected override void UpgrateFirmware(object o0x36DataPack)
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
                nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[gCurrPackPos].uRecordLength);
                if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                }
                else
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                }
            }
            else if (PACK_SIZE == 0x3A)
            {
                PACK_SIZE -= 2;
                if (gDiag_Lin.m_Total36Data.Length % PACK_SIZE == 0)
                    nBlockNum = Total36Data.Length / PACK_SIZE;
                else
                    nBlockNum = Total36Data.Length / PACK_SIZE + 1;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            //N2S_FlashFirewareHandler ffHandler = new N2S_FlashFirewareHandler(FlashFirmware);
            lock (Diag_LIN.m_obj)
            {
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    if (x == gCurrPackPos)//1st frame
                        m_b1stFrm = true;
                    else
                        m_b1stFrm = false;

                    //gDiag_Lin.BeginInvoke(ffHandler, new object[] { x, n0x36PackNum++, nBlockNum, Total36Data });
                    //while (!gDiag_Lin.m_b36SvrOneBlockOver)
                    //{
                    //    Thread.Sleep(10);
                    //}

                    FlashFirmware(x, n0x36PackNum++, nBlockNum, Total36Data);
                    gDiag_Lin.m_b36SvrOneBlockOver = false;
#if _CheckSum
                    Thread.Sleep(150);
#else
                    Thread.Sleep(10);
#endif
                    //wait for single block write response(0x36)
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, P2_ServerTime * 10, x + 1, 0x36);
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x + 1)); }));

                    if (!bGetPositiveResp)
                    {
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                        break;
                    }

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x00;
                }

                if (!bGetPositiveResp)
                    gDiag_Lin.m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack)
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
                nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.UpdateProgerss(nProgress);
                }
                else //last package size will not equal PackSize
                {
                    int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                    for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                    nLastMsgByteCount = DataBuffer.Length;

                    gDiag_Lin.UpdateProgerss(100);
                }
            }
            else if (PACK_SIZE == 0x38)
            {
                int nLastPackSize = 0;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    DataBuffer = new byte[nLastMsgByteCount];
                    gDiag_Lin.m_nSourceIndex = nCurrPackPos * nLastMsgByteCount;
                    Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, nLastMsgByteCount);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.UpdateProgerss(nProgress);
                }
                else
                {
                    nLastPackSize = _0x36DataPack.Length % nLastMsgByteCount;
                    DataBuffer = new byte[nLastPackSize];

                    Array.Copy(_0x36DataPack, _0x36DataPack.Length - nLastPackSize, DataBuffer, 0, nLastPackSize);
                    nLastMsgByteCount = nLastPackSize;

                    gDiag_Lin.UpdateProgerss(100);
                }
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            if (nLastMsgByteCount > 4)
                Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            else
                Write_CANMessage(DataBuffer, true, true, nLastMsgByteCount);

            //single block 0x36 data package sent
            gDiag_Lin.m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return true;
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        protected override bool Download_Finish()
        {
            gDiag_Lin.IncludeTextMessage("Security access pass.");

            Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);
            Thread.Sleep(P2_ServerTime);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 200, 0, 0x34);
            if (bGetPositiveResp)
            {
                gDiag_Lin.IncludeTextMessage("Data transfer start.");
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[gDiag_Lin.m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = gDiag_Lin.m_Total36Data;

                    gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                    gDiag_Lin.m_WriteThread.IsBackground = true;
                    gDiag_Lin.m_WriteThread.Start(m_N2SDataPack);

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && gDiag_Lin.m_WriteThread != null)
                    {
                        IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver || !gDiag_Lin.m_bTransferDataOK)
                        {
                            gDiag_Lin.m_WriteThread.Interrupt();
                            gDiag_Lin.m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gDiag_Lin.gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                }
            }
            else
                gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);

            gCurrPackPos++;


            if (gDiag_Lin.m_bTransferDataOK)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(30);
                if (gDiag_Lin.m_RespMsg[1] == 0x77)
                {
                    gDiag_Lin.IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    lock (this)
                    {
                        gDiag_Lin.m_bEnable_0x3E = false;
                        gDiag_Lin.m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs
                    }

                    return true;
                }
                else
                    gDiag_Lin.NegativeMessage(0x37, gDiag_Lin.m_RespMsg);
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
        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00)
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
                gDiag_Lin.N2S_ProcessFollowCtrl((byte)reqID);

                if (reqID == 0x31)
                {
                    Thread.Sleep(300);
                    gDiag_Lin.ReadMessage(ref resp);
                }
                else if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else
                {
                    resp = gDiag_Lin.m_RespMsg;
                }

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
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response read/write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x6E || resp[2] == 0x62) //response read/write DID
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

    }

    internal class CANUDS40_Flahing : FlashBase   //AC7840
    {
        #region member variabels

        byte[] m_ReqMsg;
        int gCurrPackPos;

        #endregion

        public CANUDS40_Flahing()
        {
        }

        ~CANUDS40_Flahing() { }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] respMsg = new byte[8];

            try
            {               
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_ReadDTCEvent.Reset();

                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.m_bEnable_Trace = true;

                gCurrPackPos = 0;   //reset 0x36 sent package counter 
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                nSendResult = Write_CANMessage(m_ReqMsg, true);

                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                {
#if _DTC_Switch
                    m_ReqMsg = new byte[] { 0x85, 0x02 }; //DTC switch
                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                    {
#endif

#if _Com_Switch
                        m_ReqMsg = new byte[] { 0x28, 0x01, 0x01 }; //Disable APP message
                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                        if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x28))
                        {
#endif
                            m_ReqMsg = new byte[] { 0x10, 0x02}; //Programe session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                            {
#if _SecurityAccess
                                m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, gDiag_Lin.ST_MIN, 0, 0x27))//If security access pass.
                                {
                                    byte[] SeedArray = new byte[4];
                                    byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                    for (int i = 0; i < SeedArray.Length; i++)
                                        SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                                    fConvert.seedToKey2(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                                    m_ReqMsg = new byte[6];
                                    m_ReqMsg[0] = 0x27;
                                    m_ReqMsg[1] = 0x02;
                                    m_ReqMsg[2] = KeyArray[0];
                                    m_ReqMsg[3] = KeyArray[1];
                                    m_ReqMsg[4] = KeyArray[2];
                                    m_ReqMsg[5] = KeyArray[3];

                                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                                    if (Resp_TH(ref nMaxNumOfBlock, gDiag_Lin.ST_MIN, 0, 0x27))
#endif
                                    {
                                        //Enable TestPresent 0x3E  & message view rolling
                                        m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                        Write_CANMessage(m_ReqMsg, true);

                                        if (Resp_TH(ref nMaxNumOfBlock, gDiag_Lin.ST_MIN, 0, 0x3E))
                                        {
                                            m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                            Write_CANMessage(m_ReqMsg, true);
                                            Thread.Sleep(2 * P2_ServerTime);

                                            {
                                                //gDiag_Lin.m_bEnable_0x3E = true;
                                                //gDiag_Lin.m_bEnable_Trace = true;
                                            }
                                        }
                                        else
                                        {
                                            gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                            return false;
                                        }

                                        gDiag_Lin.IncludeTextMessage("Security access pass.");

                                        #region //write DID 0xF15A(暂时没有需求，先屏蔽)
                                ////write DIDs value(F15A) in Programing session
                                //bool bDID_Right = false;
                                //string strIniFile;
                                //byte[] writeDID = new byte[3] { 0x2E, 0xF1, 0x5A };

                                //strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
                                //bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "F15A", writeDID, 9, 1);
                                //if (!bDID_Right)
                                //{
                                //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                //    return false;
                                //}
                                //else
                                //{
                                //    gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                //}
                                ////-
                                #endregion

                                        //Earse command(Fixed address / memory size for earse memory)
                                        m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                                        //Memory address MEMORY_ADDR, MEMORY_SIZE
                                        string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR, 16);
                                        byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                                        //Memory size
                                        string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                                        byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                                        byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                                        byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                                        //Earse whole command
                                        nSendResult = Write_CANMessage(EraseMemory);
                                        gDiag_Lin.IncludeTextMessage("Now earsing flash,please wait for amoument...");

                                        int nWaitTime = 0;
                                        m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                        while (nWaitTime * REQ_3E_INTERVAL < RESP_0x31_WAITTING_TIME) //earsing need expenditure about 6000ms
                                        {
                                            Write_CANMessage(m_ReqMsg, true);
                                            Thread.Sleep(REQ_3E_INTERVAL);
                                            nWaitTime++;
                                        }

                                        int nBlocks = 0; ;
                                        if (Resp_TH(ref nBlocks, gDiag_Lin.ST_MIN, 0, 0))
                                        {
                                            gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                            gDiag_Lin.IncludeTextMessage("System will download application file.");

                                            bMainFlashOK = Download_Finish();
                                            if (bMainFlashOK)
                                                gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                            else
                                            {
                                                gDiag_Lin.IncludeTextMessage("Dowload has be interupted.");
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                            gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                            return false;
                                        }
                                    }
#if _SecurityAccess
                                }
                                else
                                    gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
#endif
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);
#if _Com_Switch
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x28, gDiag_Lin.m_RespMsg);
#endif

#if _DTC_Switch
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
#endif
                }
                else
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);

                //Back flashing step
                if (bMainFlashOK)
                {
                    //caculate checksum
                    int y = 0;
                    //for verify checksum algorithm with 'https://www.lddgo.net/encrypt/crc' caculate result
                    uint cCheckSum = fConvert.N2S_CheckSum(gDiag_Lin.m_Total36Data, CANUDS_MASK);
                    byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);

                    //big ending convert
                    byte[] CheckSumR = new byte[4];
                    for (int z = bCheckSum.Length - 1; z >= 0; z--)
                        CheckSumR[y++] = bCheckSum[z];

                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                    Byte[] n2s_checksum = gDiag_Lin.Combine(m_ReqMsg, CheckSumR);
                    Write_CANMessage(n2s_checksum);

                    if (gDiag_Lin.m_RecData.Count > HEX_DATA_SIZE)//wait 3s here,if hex data size greater than 256k(means boot software need spend more time for caclulate checksum)
                        Thread.Sleep(REQ_3E_INTERVAL);

                    int nBlockNum = 0;
                    if (Resp_TH(ref nBlockNum, 350, 0, 0x31))
                    {
                        gDiag_Lin.IncludeTextMessage("All of transfer hex data consistency check pass!");
                        gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset(SoftReset)
                        Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 300, 0, 0x11);
                        if (bGetPositiveResp)
                        {
                            //refresh Trace window message & set all of buttons enable
                            gDiag_Lin.RefreshDBGridView();
                            gDiag_Lin.SetDonwloadingStatus(false);
                            gDiag_Lin.m_bEnable_Trace = false;

                            Thread.Sleep(REQ_3E_INTERVAL/2);

                            gDiag_Lin.IncludeTextMessage("ECU hard reset succeed.");
                            gDiag_Lin.IncludeTextMessage("Fireware download succeed.");             

                            m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10)) 
                            {
    #if _DTC_Switch
                                m_ReqMsg = new byte[] { 0x85, 0x01 }; //DTC switch ON
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                                {
    #endif

    #if _Com_Switch
                                    m_ReqMsg = new byte[] { 0x28, 0x00, 0x01 }; //Enable APP message
                                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x28))
                                    {
    #endif

    #if _Com_Switch
                                    }
                                    else
                                        gDiag_Lin.NegativeMessage(0x28, gDiag_Lin.m_RespMsg);
    #endif

    #if _DTC_Switch
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
    #endif

                            }
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);
                    }
                    else
                    {
                        //refresh Trace window message & set all of buttons enable
                        gDiag_Lin.RefreshDBGridView();
                        //gDiag_Lin.SetDonwloadingStatus(false);
                        gDiag_Lin.m_bEnable_Trace = false;
                        gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                    }
                }
                else
                {
                    //gDiag_Lin.m_bEnable_0x3E = false;
                    gDiag_Lin.m_bEnable_Trace = false;
                }
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
                return false;
            }
            return true;
        }

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte number<paramref >
        /// </summary>
        protected override void UpgrateFirmware(object o0x36DataPack)
        {
            int nPerPackDataNum = 0;
            int nBlockNum = 0;
            int nMaxNumOfBlock = 0;
            int nDuring0x34BlockSize = 0;
            int n0x36PackNum = 0x01;
            bool bSendResult = false;
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
                nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[gCurrPackPos].uRecordLength);
                if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                }
                else
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                }
            }
            else if (PACK_SIZE == 0x3A)
            {
                PACK_SIZE -= 2;
                if (gDiag_Lin.m_Total36Data.Length % PACK_SIZE == 0)
                    nBlockNum = Total36Data.Length / PACK_SIZE;
                else
                    nBlockNum = Total36Data.Length / PACK_SIZE + 1;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            lock (Diag_LIN.m_obj)
            {
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    bSendResult = FlashFirmware(x, n0x36PackNum++, nBlockNum, Total36Data);
                    gDiag_Lin.m_b36SvrOneBlockOver = false;

                    //wait for single block write response(0x36)
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, P2_ServerTime * 10, x + 1, 0x36);                    
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x + 1)); }));

                    if (!bGetPositiveResp || !bSendResult)
                    {
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                        break;
                    }
                    else
                    {
                        //byte[] Req3E80Msg = new byte[] { 0x3E, 0x80 };
                        //Write_CANMessage(Req3E80Msg, true);
                    }

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x00;
                }

                if (!bGetPositiveResp)
                    gDiag_Lin.m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack)
        {
            //sending data
            bool bSendResult = false;
            int nWriteResult = -1;
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };

            nCurrPackPos = nCurrIndex;
            if(PACK_SIZE >= 0x80)
            {
                nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);                   
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(nProgress); }));
                }
                else //last package size will not equal PackSize
                {
                    int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                    for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                    nLastMsgByteCount = DataBuffer.Length;

                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(100); }));
                }
            }
            else if (PACK_SIZE == 0x38)
            {
                int nLastPackSize = 0;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    DataBuffer = new byte[nLastMsgByteCount];
                    gDiag_Lin.m_nSourceIndex = nCurrPackPos * nLastMsgByteCount;
                    Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, nLastMsgByteCount);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.UpdateProgerss(nProgress);
                }
                else
                {
                    nLastPackSize = _0x36DataPack.Length % nLastMsgByteCount;
                    DataBuffer = new byte[nLastPackSize];

                    Array.Copy(_0x36DataPack, _0x36DataPack.Length - nLastPackSize, DataBuffer, 0, nLastPackSize);
                    nLastMsgByteCount = nLastPackSize;

                    gDiag_Lin.UpdateProgerss(100);
                }
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            if (nLastMsgByteCount > 4)
                nWriteResult = Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            else
                nWriteResult = Write_CANMessage(DataBuffer, true, true, nLastMsgByteCount);

            bSendResult = (nWriteResult == 0) ? true : false;

            //single block 0x36 data package sent
            gDiag_Lin.m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return bSendResult;
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        protected override bool Download_Finish()
        {
            gDiag_Lin.IncludeTextMessage("Security access pass.");
            Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x34);
            if (bGetPositiveResp)
            {
                gDiag_Lin.IncludeTextMessage("Data transfer start.");
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[gDiag_Lin.m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = gDiag_Lin.m_Total36Data;

                    gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                    gDiag_Lin.m_WriteThread.IsBackground = true;
                    gDiag_Lin.m_WriteThread.Start(m_N2SDataPack);

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && gDiag_Lin.m_WriteThread != null)
                    {
                        IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver || !gDiag_Lin.m_bTransferDataOK)
                        {
                            gDiag_Lin.m_WriteThread.Interrupt();
                            gDiag_Lin.m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gDiag_Lin.gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                }
            }
            else
                gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);

            gCurrPackPos++;


            if (gDiag_Lin.m_bTransferDataOK && bGetPositiveResp)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(P2_ServerTime);
                if (gDiag_Lin.m_RespMsg[1] == 0x77)
                {
                    gDiag_Lin.IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    //lock (this)
                    //{
                        //gDiag_Lin.m_bEnable_0x3E = false;
                        //gDiag_Lin.m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs
                    //}

                    return true;
                }
                else
                    gDiag_Lin.NegativeMessage(0x37, gDiag_Lin.m_RespMsg);
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
        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00)
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
                //gDiag_Lin.N2S_ProcessFollowCtrl(reqID);
                gDiag_Lin.m_ReadDTCEvent.WaitOne(P2_ServerTime * 5);//must waitted response message here
  
                if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                     gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else
                {
                    resp = gDiag_Lin.m_RespMsg;
                }

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
                else if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0x02 && resp[4] == 0x02) //for CheckDependency
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
                else if (resp[1] == 0x7E && resp[2] == 0x00) //0x3E
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x01) //request seed
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x02) //send key
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
                    gDiag_Lin.ST_MIN = resp[4];
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
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response read/write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x6E || resp[2] == 0x62) //response read/write DID
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

                if (nNegResp > 5)
                    break;

                if (resp[1] == 0x7F)
                {
                    if (reqID == 0x31)//0x31 request has 2 0x7f response message,if beyong 2 then consider its wrong(Earse command, Consistency check)
                    {
                        if (nNegResp++ > 2)
                            break;
                        gDiag_Lin.m_ReadDTCEvent.Reset();
                    }
                    else
                        nNegResp++; 
                }

                Thread.Sleep(10);
                nLoop++;            
            }
            gDiag_Lin.m_ReadDTCEvent.Reset();
            return bResult;
        }

    }

    internal class CANUDS01_Flahing : FlashBase   //AC7801
    {
        #region member variabels

        byte[] m_ReqMsg;
        int gCurrPackPos;

        #endregion

        public CANUDS01_Flahing()
        {
        }

        ~CANUDS01_Flahing() { }

        /// <summary>
        /// write info into 
        /// </summary>
        /// <param name="strInfo"></param>
        /// <param name="msgID">negetive message id if occoured</param>
        /// <param name="bPositiveInfo">info or error?</param>
        private void WritelbMessage(string strInfo, byte msgID, bool bPositiveInfo)
        {
            if(bPositiveInfo)
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(strInfo); }));
            else     
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.NegativeMessage(msgID, gDiag_Lin.m_RespMsg); }));
        }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] respMsg = new byte[8];

            try
            {
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_ReadDTCEvent.Reset();

                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.m_bEnable_Trace = true;
                
                gCurrPackPos = 0;   //reset 0x36 sent package counter 
                //disable all of button which accoiate with diag message func when download start
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.SetDonwloadingStatus(true);  }));

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                nSendResult = Write_CANMessage(m_ReqMsg, true);

                 if (Resp_TH(ref nMaxNumOfBlock, 80, 0, 0x1003))
                {
#if _DTC_Switch
                    m_ReqMsg = new byte[] { 0x85, 0x02 }; //DTC switch
                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                    {
#endif

#if _Com_Switch
                    Thread.Sleep(30);
                    m_ReqMsg = new byte[] { 0x28, 0x01, 0x01 }; //Disable APP message
                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                    Thread.Sleep(500);//fix bug: can not get 0x28 resopnse after recived 0x50 03(req 0x10 03), cause interval time too short, so...
                    if (Resp_TH(ref nMaxNumOfBlock, 20, 0, 0x28))
                    {
#endif
                            m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programe session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            if (Resp_TH(ref nMaxNumOfBlock, 650, 0, 0x1002))
                            {
#if _SecurityAccess
                                m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, 150, 0, 0x27))//If security access pass.
                                {
                                    byte[] SeedArray = new byte[4];
                                    byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                                    for (int i = 0; i < SeedArray.Length; i++)
                                        SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                                    fConvert.seedToKey2(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                                    m_ReqMsg = new byte[6];
                                    m_ReqMsg[0] = 0x27;
                                    m_ReqMsg[1] = 0x02;
                                    m_ReqMsg[2] = KeyArray[0];
                                    m_ReqMsg[3] = KeyArray[1];
                                    m_ReqMsg[4] = KeyArray[2];
                                    m_ReqMsg[5] = KeyArray[3];

                                    nSendResult = Write_CANMessage(m_ReqMsg, true);
                                    if (Resp_TH(ref nMaxNumOfBlock, 150, 0, 0x27))
        #endif
                                    {
                                        //Enable TestPresent 0x3E  & message view rolling
                                        m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                        Write_CANMessage(m_ReqMsg, true);

                                        if (Resp_TH(ref nMaxNumOfBlock, 150, 0, 0x3E))
                                        {
                                            m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                            Write_CANMessage(m_ReqMsg, true);
                                            //Thread.Sleep(2 * P2_ServerTime);
                                            {
                                                //gDiag_Lin.m_bEnable_0x3E = true;
                                                //gDiag_Lin.m_bEnable_Trace = true;
                                            }
                                        }
                                        else
                                        {
                                            WritelbMessage("0x3E service not work normally.", 0x00, true);
                                            return false;
                                        }
                                        WritelbMessage("Security access pass.", 0x00, true);

                                        #region //write DID 0xF15A(暂时没有需求，先屏蔽)
                                        ////write DIDs value(F15A) in Programing session
                                        //bool bDID_Right = false;
                                        //string strIniFile;
                                        //byte[] writeDID = new byte[3] { 0x2E, 0xF1, 0x5A };

                                        //strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
                                        //bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "F15A", writeDID, 9, 1);
                                        //if (!bDID_Right)
                                        //{
                                        //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                        //    return false;
                                        //}
                                        //else
                                        //{
                                        //    gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                        //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                        //}
                                        ////-
                                        #endregion

                                        //Earse command(Fixed address / memory size for earse memory)
                                        m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                                        //Memory address MEMORY_ADDR, MEMORY_SIZE
                                        string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR, 16);
                                        byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                                        //Memory size
                                        string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                                        byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                                        byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                                        byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                                        //Earse whole command
                                        nSendResult = Write_CANMessage(EraseMemory);
                                        WritelbMessage("Now earsing flash,please wait for amoument...", 0x00, true);

                                        int nWaitTime = 0;
                                        m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                        while (nWaitTime * REQ_3E_INTERVAL < (gDiag_Lin.m_DisplayAppMsg ? RESP_0x31_WAITTING_TIME:(RESP_0x31_WAITTING_TIME - 1000))) //earsing need expenditure about 5000ms
                                        {
                                            Write_CANMessage(m_ReqMsg, true);
                                            Thread.Sleep(REQ_3E_INTERVAL);
                                            nWaitTime++;
                                        }

                                        int nBlocks = 0; ;
                                        if (Resp_TH(ref nBlocks, 350, 0, 0x31))
                                        {
                                            WritelbMessage("Ecu's application be earsed.", 0x00, true);
                                            WritelbMessage("System will download application file.", 0x00, true);

                                            bMainFlashOK = Download_Finish();
                                            if (bMainFlashOK)
                                                WritelbMessage("Application file has been finished download.", 0x00, true);
                                            else
                                            {
                                                WritelbMessage("Dowload has be interupted.", 0x00, true);
                                                return false;
                                            }
                                        }
                                        else
                                        {
                                            WritelbMessage("", 0x31, false);
                                            WritelbMessage("Some issue occure when earse ecu's application file.", 0x00, true);
                                            return false;
                                        }
                                    }
        #if _SecurityAccess
                                }
                                else
                                    WritelbMessage("", 0x27, false);
 #endif
                            }
                            else 
                                WritelbMessage("", 0x10, false);

#if _Com_Switch
                        }
                    else
                        WritelbMessage("", 0x28, false);
#endif

#if _DTC_Switch
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
#endif
                }
                else
                    WritelbMessage("", 0x10, false);

                //Back flashing step
                if (bMainFlashOK)
                {
                    //caculate checksum
                    int y = 0;
                    //for verify checksum algorithm with 'https://www.lddgo.net/encrypt/crc' caculate result
                    uint cCheckSum = fConvert.N2S_CheckSum(gDiag_Lin.m_Total36Data, CANUDS_MASK);
                    byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);

                    //big ending convert
                    byte[] CheckSumR = new byte[4];
                    for (int z = bCheckSum.Length - 1; z >= 0; z--)
                        CheckSumR[y++] = bCheckSum[z];

                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                    Byte[] n2s_checksum = gDiag_Lin.Combine(m_ReqMsg, CheckSumR);
                    Write_CANMessage(n2s_checksum);

                    if (gDiag_Lin.m_RecData.Count > HEX_DATA_SIZE)//wait 3s here,if hex data size greater than 256k(means boot software need spend more time for caclulate checksum)
                        Thread.Sleep(REQ_3E_INTERVAL);

                    int nBlockNum = 0;
                    if (Resp_TH(ref nBlockNum, 350, 0, 0x31))
                    {
                        WritelbMessage("All of transfer hex data consistency check pass!", 0x00, true);
                        WritelbMessage("ECU will reboot,please wait for a moment.", 0x00, true);

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset(SoftReset)
                        Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 300, 0, 0x11);
                        if (bGetPositiveResp)
                        {
                            //refresh Trace window message & set all of buttons enable
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.RefreshDBGridView(); }));
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.SetDonwloadingStatus(false); }));
                            gDiag_Lin.m_bEnable_Trace = false;

                            Thread.Sleep(REQ_3E_INTERVAL / 2);

                            WritelbMessage("ECU hard reset succeed.", 0x00, true);
                            WritelbMessage("Fireware download succeed.", 0x00, true);

                            m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                            {
#if _DTC_Switch
                                m_ReqMsg = new byte[] { 0x85, 0x01 }; //DTC switch ON
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                                {
#endif

#if _Com_Switch
                                    m_ReqMsg = new byte[] { 0x28, 0x00, 0x01 }; //Enable APP message
                                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x28))
                                    {
#endif
                                    
#if _Com_Switch
                                }
                                    else
                                        WritelbMessage("", 0x28, false);                                
#endif

#if _DTC_Switch
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
#endif
                            }
                        }
                        else
                            WritelbMessage("", 0x11, false);                        
                    }
                    else
                    {
                        //refresh Trace window message & set all of buttons enable
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.SetDonwloadingStatus(true); }));
                        gDiag_Lin.m_bEnable_Trace = false;
                        WritelbMessage("", 0x31, false);
                    }
                }
                else
                {
                    //gDiag_Lin.m_bEnable_0x3E = false;
                    gDiag_Lin.m_bEnable_Trace = false;
                }
            }
            catch (IOException ep)
            {
                WritelbMessage(ep.Message, 0x10, true);
                return false;
            }
            return true;
        }

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte number<paramref >
        /// </summary>
        protected override void UpgrateFirmware(object o0x36DataPack)
        {
            int nPerPackDataNum = 0;
            int nBlockNum = 0;
            int nMaxNumOfBlock = 0;
            int nDuring0x34BlockSize = 0;
            int n0x36PackNum = 0x01;
            bool bSendResult = false;
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
                nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[gCurrPackPos].uRecordLength);
                if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                }
                else
                {
                    nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                }
            }
            else if (PACK_SIZE == 0x3A)
            {
                PACK_SIZE -= 2;
                if (gDiag_Lin.m_Total36Data.Length % PACK_SIZE == 0)
                    nBlockNum = Total36Data.Length / PACK_SIZE;
                else
                    nBlockNum = Total36Data.Length / PACK_SIZE + 1;
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            lock (Diag_LIN.m_obj)
            {
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    bSendResult = FlashFirmware(x, n0x36PackNum++, nBlockNum, Total36Data);
                    gDiag_Lin.m_b36SvrOneBlockOver = false;

                    //wait for single block write response(0x36)
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, P2_ServerTime * 10, x + 1, 0x36);
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x + 1)); }));

                    if (!bGetPositiveResp || !bSendResult)
                    {
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                        break;
                    }

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x00;
                }

                if (!bGetPositiveResp)
                    gDiag_Lin.m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack)
        {
            //sending data
            bool bSendResult = false;
            int nWriteResult = -1;
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };

            nCurrPackPos = nCurrIndex;
            if (PACK_SIZE >= 0x80)
            {
                nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(nProgress); }));
                }
                else //last package size will not equal PackSize
                {
                    int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                    for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                    nLastMsgByteCount = DataBuffer.Length;

                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(100); }));
                }
            }
            else if (PACK_SIZE == 0x38)
            {
                int nLastPackSize = 0;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    DataBuffer = new byte[nLastMsgByteCount];
                    gDiag_Lin.m_nSourceIndex = nCurrPackPos * nLastMsgByteCount;
                    Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, nLastMsgByteCount);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.UpdateProgerss(nProgress);
                }
                else
                {
                    nLastPackSize = _0x36DataPack.Length % nLastMsgByteCount;
                    DataBuffer = new byte[nLastPackSize];

                    Array.Copy(_0x36DataPack, _0x36DataPack.Length - nLastPackSize, DataBuffer, 0, nLastPackSize);
                    nLastMsgByteCount = nLastPackSize;

                    gDiag_Lin.UpdateProgerss(100);
                }
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            if (nLastMsgByteCount > 4)
                nWriteResult = Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            else
                nWriteResult = Write_CANMessage(DataBuffer, true, true, nLastMsgByteCount);

            bSendResult = (nWriteResult == 0) ? true : false;

            //single block 0x36 data package sent
            gDiag_Lin.m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return bSendResult;
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        protected override bool Download_Finish()
        {
            WritelbMessage("Security access pass.", 0x00, true);
            Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 150, 0, 0x34);
            if (bGetPositiveResp)
            {
                WritelbMessage("Data transfer start.", 0x00, true);
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[gDiag_Lin.m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = gDiag_Lin.m_Total36Data;
                    UpgrateFirmware(m_N2SDataPack);
                }
                catch (Exception ex)
                {
                    string strErr = string.Format("Some issue occured::{0:s} when transfer data.", ex.Message);
                    WritelbMessage(strErr, 0x00, false);                    
                }
            }
            else
                WritelbMessage("", 0x34, false);            

            gCurrPackPos++;
            if (gDiag_Lin.m_bTransferDataOK && bGetPositiveResp)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(P2_ServerTime);
                if (gDiag_Lin.m_RespMsg[1] == 0x77)
                {
                    WritelbMessage("Download finished!", 0x00, true);

                    return true;
                }
                else
                    WritelbMessage("", 0x37, false);
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
        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x0000)
        {
            bool bResult = false;
            byte[] resp= { };
            int nLoop = 0, nNegResp = 0;
            while (true)
            {               
                if (nLoop > nBlocks)
                {
                    bResult = false;
                    break;
                }

                gDiag_Lin.m_ReadDTCEvent.WaitOne();//must waitted response message here
                resp = new byte[8];

                if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else
                {
                    if (!gDiag_Lin.m_DisplayAppMsg)
                        gDiag_Lin.ReadMessage(ref resp);
                    else
                        resp = gDiag_Lin.m_RespMsg;
                }

                //finish 0x31 routine control wait(earse memory command)
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
                else if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0x02 && resp[4] == 0x02) //for CheckDependency
                {
                    if (resp[5] == 0x0)
                    {
                        bResult = true;
                    }
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
                else if (resp[1] == 0x7E && resp[2] == 0x00) //0x3E
                {
                    bResult = true;
                    Buffer.BlockCopy(gDiag_Lin.m_RespMsg, 0, resp, 0, resp.Length * sizeof(byte));
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x01) //request seed(do not clear m_RespMsg here, cause 0x27 02s' parameter need use seed caculate key)
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x02) //send key
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
                    //Clear current global response buffer,so no influnce next response message estimate
                    //Buffer.BlockCopy(new byte[8], 0, gDiag_Lin.m_RespMsg, 0, resp.Length * sizeof(byte));

                    gDiag_Lin.ST_MIN = resp[4];
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x50 && resp[2] == 0x03) //service mode switch
                {
                    //Clear current global response buffer,so no influnce next response message estimate
                   // Buffer.BlockCopy(new byte[8], 0, gDiag_Lin.m_RespMsg, 0, resp.Length * sizeof(byte));

                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x51 && resp[2] == 0x01) //soft reset
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response read/write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x6E || resp[2] == 0x62) //response read/write DID
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
                    //Buffer.BlockCopy(new byte[8], 0, gDiag_Lin.m_RespMsg, 0, resp.Length * sizeof(byte));
                    bResult = true;
                    break;
                }

                if (nNegResp > 5)
                    break;

                if (resp[1] == 0x7F)
                {
                    if (reqID ==0x1002 ||reqID == 0x31)//0x1002, 0x31 request has 2 0x7f response message,if beyong 2 then consider its wrong(Earse command, Consistency check)
                    {
                        if (nNegResp++ > 2)
                            break;

                        //resp[1] = 0x00;//clear last negetive response byte 1, cause reFlash need waitting for some time(about 350ms) to confirm boot software restart after 0x10 02 sent.
                        gDiag_Lin.m_ReadDTCEvent.Reset();
                    }
                    else if(reqID == 0x28)
                    {
                        m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                        Write_CANMessage(m_ReqMsg, true);

                        Thread.Sleep(500);

                        m_ReqMsg = new byte[] { 0x28, 0x01, 0x01 }; //Disable APP message
                        Write_CANMessage(m_ReqMsg, true);

                        nNegResp++;
                        gDiag_Lin.m_ReadDTCEvent.Reset();
                    }
                    else
                    {
                        nNegResp++;
                        gDiag_Lin.m_ReadDTCEvent.Reset();
                    }
                }

                Thread.Sleep(10);
                nLoop++;
            }
            gDiag_Lin.m_ReadDTCEvent.Reset();

            return bResult;
        }

    }

    internal class Split_CANUDS_Flahing : FlashBase
    {
        #region member variabels

        byte[] m_ReqMsg;
        int gCurrPackPos;

        #endregion

        public Split_CANUDS_Flahing()
        {
        }

        ~Split_CANUDS_Flahing() { }

        /// <summary>
        /// Download bootlaoder & app file main work thread
        /// </summary>
        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            int nSendResult = -1;
            bool bMainFlashOK = false;
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            byte[] respMsg = new byte[8];

            try
            {
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_ReadDTCEvent.Reset();

                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.m_bEnable_Trace = false;

                gCurrPackPos = 0;   //reset 0x36 sent package counter 
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                nSendResult = Write_CANMessage(m_ReqMsg, true);

                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                {
#if _DTC_Switch
                    m_ReqMsg = new byte[] { 0x85, 0x02 }; //DTC switch
                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                    {
#endif

#if _Com_Switch
                        m_ReqMsg = new byte[] { 0x28, 0x01, 0x01 }; //Disable APP message
                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                        if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x28))
                        {
#endif
                    m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programe session
                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                    {
#if _SecurityAccess
                        m_ReqMsg = new byte[] { 0x27, 0x01 }; //Security access,request seed
                        nSendResult = Write_CANMessage(m_ReqMsg, true);

                        if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x27))//If security access pass.
                        {
                            byte[] SeedArray = new byte[4];
                            byte[] KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };
                            for (int i = 0; i < SeedArray.Length; i++)
                                SeedArray[i] = gDiag_Lin.m_RespMsg[i + 3];
                            fConvert.seedToKey2(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                            m_ReqMsg = new byte[6];
                            m_ReqMsg[0] = 0x27;
                            m_ReqMsg[1] = 0x02;
                            m_ReqMsg[2] = KeyArray[0];
                            m_ReqMsg[3] = KeyArray[1];
                            m_ReqMsg[4] = KeyArray[2];
                            m_ReqMsg[5] = KeyArray[3];

                            nSendResult = Write_CANMessage(m_ReqMsg, true);
                            if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x27))
#endif
                            {
                                //Enable TestPresent 0x3E  & message view rolling
                                m_ReqMsg = new byte[] { 0x3E, 0x00 };
                                Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x3E))
                                {
                                    m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                    Write_CANMessage(m_ReqMsg, true);
                                    Thread.Sleep(2 * P2_ServerTime);

                                    lock (this)
                                    {
                                        //gDiag_Lin.m_bEnable_0x3E = true;
                                        gDiag_Lin.m_bEnable_Trace = true;
                                    }
                                }
                                else
                                {
                                    gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                    return false;
                                }

                                gDiag_Lin.IncludeTextMessage("Security access pass.");

                                #region //write DID 0xF15A(暂时没有需求，先屏蔽)
                                //write DIDs value(F15A) in Programing session
                                //bool bDID_Right = false;
                                //string strIniFile;
                                //byte[] writeDID = new byte[3] { 0x2E, 0xF1, 0x5A };

                                //strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
                                //bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "F15A", writeDID, 9, 1);
                                //if (!bDID_Right)
                                //{
                                //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                //    return;
                                //}
                                //else
                                //{
                                //    gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                //    gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                //}
                                //-
                                #endregion

                                //Earse command
                                m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                                //Memory address MEMORY_ADDR, MEMORY_SIZE
                                string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR/* MEMORY_ADDR*/, 16);
                                byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                                //Memory size
                                string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                                byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                                byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                                byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                                //Earse whole command
                                nSendResult = Write_CANMessage(EraseMemory);
                                gDiag_Lin.IncludeTextMessage("Now earsing flash,please wait for amoument...");

                                int nWaitTime = 0;
                                m_ReqMsg = new byte[] { 0x3E, 0x80 };
                                while (nWaitTime * REQ_3E_INTERVAL < RESP_0x31_WAITTING_TIME) //earsing need expenditure about 6000ms
                                {
                                    Write_CANMessage(m_ReqMsg, true);
                                    Thread.Sleep(REQ_3E_INTERVAL);
                                    nWaitTime++;
                                }

                                int nBlocks = 0; ;
                                if (Resp_TH(ref nBlocks, 200, 0, 0))
                                {
                                    gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                    gDiag_Lin.IncludeTextMessage("System will download application file.");

                                    bMainFlashOK = Download_Finish();
                                    if (bMainFlashOK)
                                        gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                    else
                                        gDiag_Lin.IncludeTextMessage("Dowload has be interupted.");
                                }
                                else
                                {
                                    gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                    gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");

                                    return false;
                                }
                            }
#if _SecurityAccess
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
#endif
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);
#if _Com_Switch
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x28, gDiag_Lin.m_RespMsg);
#endif

#if _DTC_Switch
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
#endif
                }
                else
                {
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);
                    return false;
                }

                //Back flashing step
                if (bMainFlashOK)
                {
                    //caculate checksum
                    int y = 0;
                    //for verify checksum algorithm with 'https://www.lddgo.net/encrypt/crc' caculate result
                    uint cCheckSum = fConvert.N2S_CheckSum(gDiag_Lin.m_Total36Data, CANUDS_MASK);
                    byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);

                    //big ending convert
                    byte[] CheckSumR = new byte[4];
                    for (int z = bCheckSum.Length - 1; z >= 0; z--)
                        CheckSumR[y++] = bCheckSum[z];

                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                    Byte[] n2s_checksum = gDiag_Lin.Combine(m_ReqMsg, CheckSumR);
                    Write_CANMessage(n2s_checksum);

                    if (gDiag_Lin.m_RecData.Count > HEX_DATA_SIZE)//wait 3s here,if hex data size greater than 256k(means boot software need spend more time for caclulate checksum)
                        Thread.Sleep(REQ_3E_INTERVAL);

                    int nBlockNum = 0;
                    if (Resp_TH(ref nBlockNum, 350, 0, 0x31))
                    {
                        gDiag_Lin.IncludeTextMessage("All of transfer hex data consistency check pass!");
                        gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU reset(SoftReset)
                        Write_CANMessage(m_ReqMsg, true);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 300, 0, 0x11);
                        if (bGetPositiveResp)
                        {
                            //refresh Trace window message & set all of buttons enable
                            gDiag_Lin.RefreshDBGridView();
                            gDiag_Lin.SetDonwloadingStatus(false);
                            gDiag_Lin.m_bEnable_Trace = false;

                            Thread.Sleep(REQ_3E_INTERVAL / 2);

                            gDiag_Lin.IncludeTextMessage("ECU hard reset succeed.");
                            gDiag_Lin.IncludeTextMessage("Fireware download succeed.");

                            m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                            nSendResult = Write_CANMessage(m_ReqMsg, true);

                            if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x10))
                            {
#if _DTC_Switch
                                m_ReqMsg = new byte[] { 0x85, 0x01 }; //DTC switch ON
                                nSendResult = Write_CANMessage(m_ReqMsg, true);

                                if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x85))
                                {
#endif

#if _Com_Switch
                                    m_ReqMsg = new byte[] { 0x28, 0x00, 0x01 }; //Enable APP message
                                    nSendResult = Write_CANMessage(m_ReqMsg, true);

                                    if (Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x28))
                                    {
#endif

#if _Com_Switch
                                    }
                                    else
                                        gDiag_Lin.NegativeMessage(0x28, gDiag_Lin.m_RespMsg);
#endif

#if _DTC_Switch
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x85, gDiag_Lin.m_RespMsg);
#endif

                            }
                        }
                        else
                        {
                            gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);
                            return false;
                        }
                    }
                    else
                    {
                        //refresh Trace window message & set all of buttons enable
                        gDiag_Lin.RefreshDBGridView();
                        //gDiag_Lin.SetDonwloadingStatus(false);
                        gDiag_Lin.m_bEnable_Trace = false;
                        gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);

                        return false;
                    }
                }
                else
                {
                    //gDiag_Lin.m_bEnable_0x3E = false;
                    gDiag_Lin.m_bEnable_Trace = false;                   
                }            
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
            }
            return true;
        }

        ///<summary>
        ///Execute flash work flow thread
        ///<paramref name="nMaxBlockSize"/>singal block byte number<paramref >
        /// </summary>
        protected override void UpgrateFirmware(object o0x36DataPack)
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
                nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[gCurrPackPos].uRecordLength);
                if (gDiag_Lin.m_bAppAddr_Enable && gDiag_Lin.m_bCalAddr_Enable)    //collect hex file all of data
                {                   
                    if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
                    {
                        nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                    }
                    else
                    {
                        nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                    }
                }
                else if (gDiag_Lin.m_bAppAddr_Enable && !gDiag_Lin.m_bCalAddr_Enable)    //flash APP data only
                {                  
                    if (gDiag_Lin.m_nAppBlockNum % nPerPackDataNum == 0)
                    {
                        nBlockNum = gDiag_Lin.m_nAppBlockNum / nPerPackDataNum;
                    }
                    else
                    {
                        nBlockNum = gDiag_Lin.m_nAppBlockNum / nPerPackDataNum + 1;
                    }
                }
                else if (!gDiag_Lin.m_bAppAddr_Enable && gDiag_Lin.m_bCalAddr_Enable)   //flash CAL data only
                {
                    if ((gDiag_Lin.m_RecData.Count - gDiag_Lin.m_nCalBlockNum) % nPerPackDataNum == 0)
                    {
                        nBlockNum = (gDiag_Lin.m_RecData.Count - gDiag_Lin.m_nCalBlockNum) / nPerPackDataNum;
                    }
                    else
                    {
                        nBlockNum = (gDiag_Lin.m_RecData.Count - gDiag_Lin.m_nCalBlockNum) / nPerPackDataNum + 1;
                    }
                }
            }
            Console.WriteLine(string.Format("BlockNum::{0:d}", nBlockNum));

            lock (Diag_LIN.m_obj)
            {
                if (gDiag_Lin.m_bAppAddr_Enable && gDiag_Lin.m_bCalAddr_Enable)
                {
                    gCurrPackPos = 0;
                }
                else if (gDiag_Lin.m_bAppAddr_Enable && !gDiag_Lin.m_bCalAddr_Enable)    //flash APP data only
                {
                    gCurrPackPos = 0;
                }
                else if (!gDiag_Lin.m_bAppAddr_Enable && gDiag_Lin.m_bCalAddr_Enable)   //flash CAL data only
                {
                    gCurrPackPos = gDiag_Lin.m_nCalBlockNum;
                }            
                
                for (int x = gCurrPackPos; x < nBlockNum; x++)
                {
                    FlashFirmware(x, n0x36PackNum++, nBlockNum, Total36Data);
                    gDiag_Lin.m_b36SvrOneBlockOver = false;

                    //wait for single block write response(0x36)
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, P2_ServerTime * 10, x + 1, 0x36);
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", x + 1)); }));

                    if (!bGetPositiveResp)
                    {
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Can not receive 0x36 service positive response in transfering data, thread exited.")); }));
                        break;
                    }

                    /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                        completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
                    if (n0x36PackNum > 0xFF)
                        n0x36PackNum = 0x00;
                }

                if (!bGetPositiveResp)
                    gDiag_Lin.m_bTransferDataOK = false;
            }
        }

        /// <summary>
        /// hex data download THREAD
        /// </summary>
        /// <param name="nCurrIndex">current package index</param>
        /// <param name="n0x36PackCnt">mark transfer block number,if it greater 0xFF then make it to 0,and recounter again</param>
        /// <param name="nMaxBlockSize">hex file be splitted mutiple block data package on which of its' size</param>
        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize, byte[] _0x36DataPack)
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
                nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
                if (nCurrIndex < nMaxBlockSize - 1)
                {
                    for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                    nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(nProgress); }));
                }
                else //last package size will not equal PackSize
                {
                    int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                    for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                        DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                    nLastMsgByteCount = DataBuffer.Length;

                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(100); }));
                }
            }

            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            if (nLastMsgByteCount > 4)
                Write_CANMessage(DataBuffer, false, true, nLastMsgByteCount);
            else
                Write_CANMessage(DataBuffer, true, true, nLastMsgByteCount);

            //single block 0x36 data package sent
            gDiag_Lin.m_b36SvrOneBlockOver = true;
            m_n36SvrPackNum = 0;

            return true;
        }

        /// <summary>
        /// download work thread
        /// </summary>
        /// 
        protected override bool Download_Finish()
        {
            gDiag_Lin.IncludeTextMessage("Security access pass.");
            Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);

            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 50, 0, 0x34);
            if (bGetPositiveResp)
            {
                gDiag_Lin.IncludeTextMessage("Data transfer start.");
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[gDiag_Lin.m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = gDiag_Lin.m_Total36Data;

                    gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                    gDiag_Lin.m_WriteThread.IsBackground = true;
                    gDiag_Lin.m_WriteThread.Start(m_N2SDataPack);

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && gDiag_Lin.m_WriteThread != null)
                    {
                        IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver || !gDiag_Lin.m_bTransferDataOK)
                        {
                            gDiag_Lin.m_WriteThread.Interrupt();
                            gDiag_Lin.m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gDiag_Lin.gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                }
            }
            else
                gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);

            gCurrPackPos++;


            if (gDiag_Lin.m_bTransferDataOK && bGetPositiveResp)
            {
                //download finish
                int nResult = -1;
                byte[] respMsg = new byte[8];
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed

                nResult = Write_CANMessage(m_ReqMsg, true);
                Thread.Sleep(P2_ServerTime);
                if (gDiag_Lin.m_RespMsg[1] == 0x77)
                {
                    gDiag_Lin.IncludeTextMessage("Download finished!");
                    //stop 0x3E service
                    //lock (this)
                    //{
                    //gDiag_Lin.m_bEnable_0x3E = false;
                    //gDiag_Lin.m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs
                    //}

                    return true;
                }
                else
                    gDiag_Lin.NegativeMessage(0x37, gDiag_Lin.m_RespMsg);
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
        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            //memory address for download fireware
            if (nRequestTime == 0)
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            else
                m_ReqMsg = new byte[] { 0x34, 0x01, 0x44 };

            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return Write_CANMessage(Total34Req);
        }

        ///<summary>
        ///wait response message complete
        /// </summary>
        /// <param name="resp">response message</param>
        /// <param name="nMaxNumOfBlockLen">max number of block length</param>
        /// <param name="nBlocks">times of transfer data by 0x36 service. or wait times for other service</param>
        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00)
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
                //gDiag_Lin.N2S_ProcessFollowCtrl(reqID);
                gDiag_Lin.m_ReadDTCEvent.WaitOne(P2_ServerTime * 5);//must waitted response message here

                if (reqID == 0x22 || reqID == 0x2E)
                {
                    Thread.Sleep(50);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else if (reqID == 0x19)
                {
                    Thread.Sleep(P2_ServerTime);
                    gDiag_Lin.ReadMessage(ref resp);
                    gDiag_Lin.m_RespMsg = resp;
                }
                else
                {
                    resp = gDiag_Lin.m_RespMsg;
                }

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
                else if (resp[1] == 0x71 && resp[2] == 0x01 && resp[3] == 0x02 && resp[4] == 0x02) //for CheckDependency
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
                else if (resp[1] == 0x7E && resp[2] == 0x00) //0x3E
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x01) //request seed
                {
                    bResult = true;
                    break;
                }
                else if (resp[1] == 0x67 && resp[2] == 0x02) //send key
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
                else if (resp[1] == 0x6E || resp[1] == 0x62) //response read/write DID
                {
                    bResult = true;
                    break;
                }
                else if (resp[2] == 0x6E || resp[2] == 0x62) //response read/write DID
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

                if (resp[1] == 0x7F)
                {
                    if (reqID == 0x31)//0x31 request has 2 0x7f response message,if beyong 2 then consider its wrong(Earse command, Consistency check)
                    {
                        if (nNegResp++ > 2)
                            break;
                        gDiag_Lin.m_ReadDTCEvent.Reset();
                    }
                    else
                        break;
                }

                if (nNegResp > 5)
                    break;

                Thread.Sleep(10);
                nLoop++;


            }
            gDiag_Lin.m_ReadDTCEvent.Reset();
            return bResult;
        }

    }

    internal class LIN_Flashing_Bin : FlashBase //(7Kw .bin)
    {
        byte[] m_ReqMsg;

        public LIN_Flashing_Bin() 
        {
        }
        ~LIN_Flashing_Bin() { }

        public override bool WriteThreadFunc_TH(object diag_lin) 
        {
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                if (gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                {
                    gDiag_Lin.IncludeTextMessage("Message ID not correct.");
                    return false;
                }

                if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x03)
                {
                    m_ReqMsg = new byte[] { 0x27, 0x05 }; //SubFunc05、06

                    byte[] resp0x27 = new byte[18];
                    gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref resp0x27);

                    if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x05) //SubFunc05、06
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

                        gDiag_Lin.Send5TimeReqMsg(reqKEY, ref respMsg);
                        if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x06)
                        {
                            gDiag_Lin.IncludeTextMessage("Security access pass.");

                            m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                            gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                            if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x02)
                            {
                                //session mode changed, so need send 0x27 0x05, 0x27 0x06 once more
                                m_ReqMsg = new byte[] { 0x27, 0x05 }; //SubFunc05、06

                                byte[] resp0x27_2 = new byte[18];
                                gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref resp0x27_2);

                                if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x05) //SubFunc05、06
                                {
                                    gDiag_Lin.Send5TimeReqMsg(reqKEY, ref respMsg);
                                    if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x06)
                                    {
                                        //Enable TestPresent 0x3E  & message view rolling
                                        m_ReqMsg = new byte[] { 0x3E, 0x00 };

                                        gDiag_Lin.Write_Message(m_ReqMsg);
                                        Thread.Sleep(10);
                                        gDiag_Lin.ReadMessage(ref respMsg);
                                        if (respMsg[0] == 0x7E && respMsg[1] == 0x00)
                                        {
                                            lock (this)
                                            {
                                                gDiag_Lin.m_bEnable_0x3E = true;
                                            }
                                        }
                                        else
                                        {
                                            gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                            return false;
                                        }

                                        //Earse command
                                        m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x00, 0x10 };
                                        //cause test following diagnostic service,so marked now20240119
                                        gDiag_Lin.Write_Message(m_ReqMsg); // new earsing command
                                        //waitting for earse finish
                                        Thread.Sleep(100);

                                        int nBlocks = 0;
                                        bool bGetPositiveResp = false;
                                        bGetPositiveResp = Resp_TH(ref nBlocks, 350);
                                        if (bGetPositiveResp)
                                        {
                                            gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                            gDiag_Lin.IncludeTextMessage("System will download application file.");

                                            bMainFlashOK = Download_Finish();
                                            if (bMainFlashOK)
                                                gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                        }
                                        else
                                        {
                                            gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                            gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");
                                            return false;
                                        }
                                    }
                                }
                                else
                                    gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                            }
                            else
                            {
                                gDiag_Lin.IncludeTextMessage("Enter programme session failure.");
                                return false;
                            }
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                }
                else
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);
                

                //Back flashing step
                if (bMainFlashOK)
                {
                    //m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x01 }; //CheckProgrammingDependencies
                    //Write_Message(m_ReqMsg);

                    int nBlockNum = 0;
                    bool bGetPositiveResp = false;
                    //bGetPositiveResp = Resp_TH_TP90(ref nBlockNum, 10);
                    //if (gDiag_Lin.m_RespMsg[0] == 0x71 && gDiag_Lin.m_RespMsg[1] == 0x01/*gDiag_Lin.m_RespMsg[4] == 0x04*/)
                    {
                        gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU soft reset
                        gDiag_Lin.Write_Message(m_ReqMsg);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 50);
                        if (bGetPositiveResp)
                        {
                            gDiag_Lin.IncludeTextMessage("ECU soft reset succeed.");
                            gDiag_Lin.IncludeTextMessage("Fireware download succeed.");
                            
                            lock (this)
                            {
                                gDiag_Lin.m_bEnable_0x3E = false;
                                Thread.Sleep(500);
                                gDiag_Lin.m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs

                                gDiag_Lin.UpdateProgerss(100);
                                gDiag_Lin.RefreshDBGridView(); //refresh trace grid view for display newest message
                                gDiag_Lin.SetDonwloadingStatus(false);
                            }
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);

                    }
                    //else if (m_RespMsg[1] == 0x71 && m_RespMsg[4] == 0x05)
                    //    NegativeMessage(0x31, m_RespMsg);
                }
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
            }
            return true;
        }

        protected override void UpgrateFirmware(object BINADDRINFO) 
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
                Diag_LIN._Bin_Addr_Len binAddrInfo = (Diag_LIN._Bin_Addr_Len)BINADDRINFO;
                PACK_SIZE = (int)binAddrInfo.MaxBlockSize;
                FS = binAddrInfo.FS;

                if (gDiag_Lin.m_nTP90_ReadAddr_Times == 0)
                {
                    FS.Seek(0, SeekOrigin.Begin);
                    gDiag_Lin.m_FirmwareFileSize = binAddrInfo.BlockLen;
                }
                else if (gDiag_Lin.m_nTP90_ReadAddr_Times == 1)
                {
                    FS.Seek(gDiag_Lin.m_FirmwareFileSize, SeekOrigin.Begin);
                    gDiag_Lin.m_FirmwareFileSize = binAddrInfo.BlockLen;
                }

                lock (Diag_LIN.m_obj)
                {
                    for (AddrOffset = 0; AddrOffset < gDiag_Lin.m_FirmwareFileSize;)
                    {
                        DataBuffer = new Byte[PACK_SIZE];
                        read_data_num = FS.Read(DataBuffer, 0, PACK_SIZE);
                        Thread.Sleep(20);
                        if (read_data_num != PACK_SIZE) //if last package size not equal PACK_SIZE(0x80)
                        {
                            byte[] LastBlock = new byte[read_data_num];
                            Array.Copy(DataBuffer, LastBlock, read_data_num);
                            gDiag_Lin.BeginInvoke(ffhHandler, new object[] { LastBlock, 100, gDiag_Lin.m_n0x36PackNum++ });
                        }
                        else
                        {
                            gDiag_Lin.BeginInvoke(ffhHandler, new object[] { DataBuffer, nProgressStep, gDiag_Lin.m_n0x36PackNum++ });
                        }

                        Thread.Sleep(100);
                        nProgressStep = (int)(((float)(AddrOffset + read_data_num) / (float)gDiag_Lin.m_FirmwareFileSize) * 100.0f);

                        //wait for single block write response
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 10, gDiag_Lin.m_n0x36PackNum); }));
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", n0x36PackNum0++)); }));

                        if (!bGetPositiveResp)
                        {
                            gDiag_Lin.m_bTransferDataOK = false;
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage("downloading fireware failure."); }));
                            break;
                        }

                        if (gDiag_Lin.m_n0x36PackNum > 0xFF)
                            gDiag_Lin.m_n0x36PackNum = 0;

                        AddrOffset += read_data_num;
                    }
                }
                

                gDiag_Lin.m_nTP90_ReadAddr_Times++;
            }
            catch (IOException ioEx)
            {
                gDiag_Lin.IncludeTextMessage(ioEx.Message);
                return;
            }
        }

        protected override int FlashFirmware_TH_TP90(byte[] buffer, int nCurrIndex, int n0x36PackCnt) 
        {
            /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                  completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes0 = Convert.ToByte(n0x36PackCnt);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes0 });
            buffer = gDiag_Lin.Combine(_36Svr_Times, buffer);

            gDiag_Lin.Write_Message(buffer);
            gDiag_Lin.UpdateProgerss(nCurrIndex);

            return buffer.Length; //read_data_num;
        }

        protected override bool Download_Finish() 
        {
            Diag_LIN._Bin_Addr_Len BAL = new Diag_LIN._Bin_Addr_Len();
            byte[] newBlockSize = new byte[4];
            m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            FileStream fs = null;
            fs = new FileStream(gDiag_Lin.m_strHexFileName, FileMode.Open, FileAccess.Read);
            if (fs != null)
            {
                foreach (Diag_LIN._Bin_Addr_Len bal in gDiag_Lin.m_lstBinInfo)
                {
                    //memory address for download fireware
                    string strDownloadAddr = Convert.ToString(bal.StartAddress, 16);
                    byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
                    byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

                    //download size & address combine
                    string strDownloadLEN = Convert.ToString(bal.BlockLen, 16);
                    byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                    //request download command + memory address + memory size
                    byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);
                    gDiag_Lin.Write_Message(Total34Req);

                    int nMaxNumOfBlock = 0;
                    bool bGetPositiveResp = false;
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5);
                    if (bGetPositiveResp)
                    {
                        gDiag_Lin.IncludeTextMessage("Data transfer start.");
                        try
                        {
                            BAL.StartAddress = bal.StartAddress;
                            BAL.BlockLen = bal.BlockLen;
                            BAL.MaxBlockSize = (uint)nMaxNumOfBlock;
                            BAL.FS = fs;

                            gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                            gDiag_Lin.m_WriteThread.IsBackground = true;
                            gDiag_Lin.m_WriteThread.Start(BAL);

                            bool IfTimesEnd = false;
                            bool IfRunOver = false;
                            while (!IfRunOver && gDiag_Lin.m_WriteThread != null || !gDiag_Lin.m_bTransferDataOK)
                            {
                                IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                                Application.DoEvents();
                                if (!IfTimesEnd || IfRunOver)
                                {
                                    gDiag_Lin.m_WriteThread.Interrupt();
                                    gDiag_Lin.m_WriteThread.Abort();
                                    IfTimesEnd = false;
                                    gDiag_Lin.gAddrOffset = 0;
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                            return false;
                        }
                        finally
                        {
                            gDiag_Lin.IncludeTextMessage(string.Format("Transfer data succeed."));
                        }
                    }
                    else
                    {
                        gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);
                        return false;
                    }

                    //reset 0x36 transfer data sequence number after 1 block transfered.
                    gDiag_Lin.m_n0x36PackNum = 1;
                }

                gDiag_Lin.m_nTP90_ReadAddr_Times = 0;
                gDiag_Lin.m_FirmwareFileSize = 0;
                fs.Close();
            }


            if (gDiag_Lin.m_bTransferDataOK)
            {
                //download finish
                byte[] respMsg = new byte[] { 0 };
                m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed
                gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                if (respMsg[0] == 0x77)
                {
                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02, 0x10 }; //Integrity check package 
                    gDiag_Lin.Write_Message(m_ReqMsg);

                    int nMaxNumOfBlock = 0;
                    bool bGetPositiveResp = false;
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5);
                    if (bGetPositiveResp)
                    {
                        gDiag_Lin.IncludeTextMessage("Application file download succeed!");
                    }
                    else
                        gDiag_Lin.IncludeTextMessage("Download fauilure!");
                }
                else
                {
                    gDiag_Lin.NegativeMessage(0x37, respMsg);
                    return false;
                }
            }
            return true;
        }

        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00) 
        {
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;

            while (nLoop < nBlocks)
            {
                gDiag_Lin.ReadMessage(ref resp);

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

    }

    internal class LIN_Flashing_Hex : FlashBase //(.hex)
    {
        byte[] m_ReqMsg;

        public LIN_Flashing_Hex()
        {
        }
        ~LIN_Flashing_Hex() { }

        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];
            try
            {
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                //Main flashing step
                m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extension session
                if (gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                {
                    gDiag_Lin.IncludeTextMessage("Message ID not correct.");
                    return false;
                }

                if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x03)
                {
                    m_ReqMsg = new byte[] { 0x27, 0x01 }; //SubFunc01、02

                    byte[] resp0x27 = new byte[8];
                    gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref resp0x27);

                    if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x01) //SubFunc01、02
                    {
                        byte[] reqKEY = new byte[6];
                        byte[] SeedArray = new byte[4];
                        byte[] KeyArray = new byte[4];
                        for (int i = 0; i < 4; i++)
                            SeedArray[i] = resp0x27[i + 2];
                        fConvert.seedToKey2(SeedArray, out KeyArray, MASK);   //according response seed caculate security access key

                        reqKEY[0] = 0x27;
                        reqKEY[1] = 0x02; //                  SubFunc01、02
                        reqKEY[2] = KeyArray[0];
                        reqKEY[3] = KeyArray[1];
                        reqKEY[4] = KeyArray[2];
                        reqKEY[5] = KeyArray[3];

                        gDiag_Lin.Send5TimeReqMsg(reqKEY, ref respMsg);
                        if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x02)
                        {
                            gDiag_Lin.IncludeTextMessage("Security access pass.");

                            m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                            gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                            if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x02)
                            {
                                //Enable TestPresent 0x3E  & message view rolling
                                m_ReqMsg = new byte[] { 0x3E, 0x00 };

                                gDiag_Lin.Write_Message(m_ReqMsg);
                                Thread.Sleep(10);
                                gDiag_Lin.ReadMessage(ref respMsg);
                                if (respMsg[0] == 0x7E && respMsg[1] == 0x00)
                                {
                                    //lock (this)
                                    //{
                                    //    gDiag_Lin.m_bEnable_0x3E = true;
                                    //}
                                }
                                else
                                {
                                    gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                    return false;
                                }

                                //Earse command
                                m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x44 };
                                //Memory address MEMORY_ADDR, MEMORY_SIZE
                                string strDownloadADDR = Convert.ToString(gDiag_Lin.CAN_ADDR, 16);
                                byte[] DownloadADDR = gDiag_Lin.HexStringToByteArray(strDownloadADDR);

                                //Memory size
                                string strDownloadLEN = Convert.ToString(gDiag_Lin.CAN_SIZE, 16);
                                byte[] DownloadLEN = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                                byte[] EraseMemory1 = gDiag_Lin.Combine(m_ReqMsg, DownloadADDR);
                                byte[] EraseMemory = gDiag_Lin.Combine(EraseMemory1, DownloadLEN);

                                gDiag_Lin.Write_Message(EraseMemory); // new earsing command
                                //waitting for earse finish
                                gDiag_Lin.IncludeTextMessage("Now earsing flash,please wait for amoument...");

                                int nBlocks = 0;
                                bool bGetPositiveResp = false;
                                bGetPositiveResp = Resp_TH(ref nBlocks, 60);//cause ac7801 chip physic character maximum wait 6sec
                                if (bGetPositiveResp)
                                {
                                    gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                                    gDiag_Lin.IncludeTextMessage("System will download application file.");

                                    bMainFlashOK = Download_Finish();
                                    if (bMainFlashOK)
                                        gDiag_Lin.IncludeTextMessage("Application file has been finished download.");
                                }
                                else
                                {
                                    gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                                    gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");
                                    return false;
                                }
                            }
                            else
                            {
                                gDiag_Lin.IncludeTextMessage("Enter programme session failure.");
                                return false;
                            }
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                }
                else
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);
                

                //Back flashing step
                if (bMainFlashOK)
                {
                    int nBlockNum = 0;
                    bool bGetPositiveResp = false;
                    //caculate checksum
                    int y = 0;
                    //for verify checksum algorithm with 'https://www.lddgo.net/encrypt/crc' caculate result
                    uint cCheckSum = fConvert.N2S_CheckSum(gDiag_Lin.m_Total36Data, CANUDS_MASK);
                    byte[] bCheckSum = BitConverter.GetBytes(cCheckSum);

                    //big ending convert
                    byte[] CheckSumR = new byte[4];
                    for (int z = bCheckSum.Length - 1; z >= 0; z--)
                        CheckSumR[y++] = bCheckSum[z];

                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x02 }; //CheckSum verify
                    Byte[] n2s_checksum = gDiag_Lin.Combine(m_ReqMsg, CheckSumR);
                    Write_LINMessage(n2s_checksum, true);

                    if (gDiag_Lin.m_RecData.Count > HEX_DATA_SIZE)//wait 3s here,if hex data size greater than 256k(means boot software need spend more time for caclulate checksum)
                        Thread.Sleep(REQ_3E_INTERVAL);

                    if (Resp_TH(ref nBlockNum, 35, 0, 0x31))
                    {
                        gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                        m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU soft reset
                        gDiag_Lin.Write_Message(m_ReqMsg);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 50);
                        if (bGetPositiveResp)
                        {
                            gDiag_Lin.IncludeTextMessage("ECU soft reset succeed.");
                            gDiag_Lin.IncludeTextMessage("Fireware download succeed.");

                            lock (this)
                            {
                                //gDiag_Lin.m_bEnable_0x3E = false;
                                Thread.Sleep(500);
                                gDiag_Lin.m_nWriteDID_Times++;//after write DID F0F0, F199 in extended mode and finish flash App, then permit write residue DIDs

                                gDiag_Lin.UpdateProgerss(100);
                                gDiag_Lin.RefreshDBGridView(); //refresh trace grid view for display newest message
                                gDiag_Lin.SetDonwloadingStatus(false);
                            }
                        }
                        else
                            gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                }
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
                return false;
            }
            return true;
        }

        protected override void UpgrateFirmware(object o0x36DataPack)
        {
            try
            {
                int n0x36PackNum0 = 1;
                int nMaxNumOfBlock = 0;
                int nPerPackDataNum = 0;
                int nBlockNum = 0;
                int n0x36PackNum = 1;
                bool bGetPositiveResp = false;

                //ECU feedback max number of block size.
                N2S_DataPack n2s_datapack = (N2S_DataPack)o0x36DataPack;
                PACK_SIZE = n2s_datapack.nMaxNumOfBlock;
                byte[] DataBuff = n2s_datapack.All0x36PackData;

                if (PACK_SIZE == 0)
                {
                    gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage("Get 0x36 each package size failure through 0x34 request,flash flow interrupt."); }));
                    return;
                }

                //Here is pure data total length per package in which will download data. 
                if (PACK_SIZE >= 0x80)
                {
                    nPerPackDataNum = (PACK_SIZE / gDiag_Lin.m_RecData[0].uRecordLength);
                    if (gDiag_Lin.m_RecData.Count % nPerPackDataNum == 0)
                    {
                        nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum;
                    }
                    else
                    {
                        nBlockNum = gDiag_Lin.m_RecData.Count / nPerPackDataNum + 1;
                    }
                }//_

                lock (Diag_LIN.m_obj)
                {
                    for (int x = 0; x < nBlockNum; x++)
                    {
                        FlashFirmware(x, n0x36PackNum++, nBlockNum);
                        gDiag_Lin.m_b36SvrOneBlockOver = false;

                        //wait for single block write response
                        bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 10, n0x36PackNum);
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", n0x36PackNum0++)); }));

                        if (!bGetPositiveResp)
                        {
                            gDiag_Lin.m_bTransferDataOK = false;
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage("downloading fireware failure."); }));
                            break;
                        }

                        if (n0x36PackNum > 0xFF)
                            n0x36PackNum = 0;
                    }
                }
            }
            catch (IOException ioEx)
            {
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(ioEx.Message); }));
                return;
            }
        }

        protected override bool FlashFirmware(int nCurrIndex, int n0x36PackCnt, int nMaxBlockSize)
        {
            //sending data
            int nPack = 0;
            int nProgress = 0;
            int nCurrPackPos = 0;
            int nWriteStatus = 0;
            int nLastMsgByteCount = PACK_SIZE;
            byte[] DataBuffer = new byte[] { };

            nCurrPackPos = nCurrIndex;

            nPack = PACK_SIZE / gDiag_Lin.m_RecData[nCurrPackPos].uRecordLength;
            if (nCurrIndex < nMaxBlockSize - 1)
            {
                for (gDiag_Lin.gAddrOffset = nCurrPackPos * nPack; gDiag_Lin.gAddrOffset < (nCurrPackPos + 1) * nPack; gDiag_Lin.gAddrOffset++)
                    DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[gDiag_Lin.gAddrOffset].Data);

                nProgress = (int)(((float)nCurrPackPos / (float)nMaxBlockSize) * 100.0f);
                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(nProgress); }));
            }
            else //last package size will not equal PackSize
            {
                int nLastMsgCount = gDiag_Lin.m_RecData.Count;
                for (int i = gDiag_Lin.gAddrOffset; i < nLastMsgCount; i++)
                    DataBuffer = gDiag_Lin.Combine(DataBuffer, gDiag_Lin.m_RecData[i].Data);
                nLastMsgByteCount = DataBuffer.Length;

                gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.UpdateProgerss(100); }));
            }
            
            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes = Convert.ToByte(n0x36PackCnt & 0xFF);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes });
            DataBuffer = gDiag_Lin.Combine(_36Svr_Times, DataBuffer);

            nWriteStatus = gDiag_Lin.Write_Message(DataBuffer);

            #region split 0x102 length bytes to mutiple frames(do these copy methon from Write_CANMsg func) 
            //if (nLastMsgByteCount > 4)
            //    nWriteStatus = Write_LINMessage(DataBuffer, false, true, nLastMsgByteCount);
            //else
            //    nWriteStatus = Write_LINMessage(DataBuffer, true, true, nLastMsgByteCount);

            ////single block 0x36 data package sent
            //gDiag_Lin.m_b36SvrOneBlockOver = true;
            //m_n36SvrPackNum = 0;
            #endregion

            return nWriteStatus == 0 ? true : false;
        }

        protected override int Send34Request(uint startAddr, uint dataLen, int nRequestTime)
        {
            byte[] newBlockSize = new byte[4];
            m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
            
            //memory address for download fireware
            string strDownloadAddr = Convert.ToString(startAddr, 16);
            byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
            byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

            //download size & address combine
            string strDownloadLEN = Convert.ToString(dataLen, 16);
            byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

            //request download command + memory address + memory size
            byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);

            return gDiag_Lin.Write_Message(Total34Req);
        }

        protected override bool Download_Finish()
        {
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;

            Send34Request(gDiag_Lin.MEMORY_ADDR, gDiag_Lin.MEMORY_SIZE, 0);
            bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5);

            if (bGetPositiveResp)
            {
                gDiag_Lin.IncludeTextMessage("Data transfer start.");
                try
                {
                    m_N2SDataPack = new N2S_DataPack();
                    m_N2SDataPack.nMaxNumOfBlock = nMaxNumOfBlock;
                    m_N2SDataPack.All0x36PackData = new byte[gDiag_Lin.m_Total36Data.Length];
                    m_N2SDataPack.All0x36PackData = gDiag_Lin.m_Total36Data;

                    gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                    gDiag_Lin.m_WriteThread.IsBackground = true;
                    gDiag_Lin.m_WriteThread.Start(m_N2SDataPack);

                    bool IfTimesEnd = false;
                    bool IfRunOver = false;
                    while (!IfRunOver && gDiag_Lin.m_WriteThread != null || !gDiag_Lin.m_bTransferDataOK)
                    {
                        IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver)
                        {
                            gDiag_Lin.m_WriteThread.Interrupt();
                            gDiag_Lin.m_WriteThread.Abort();
                            IfTimesEnd = false;
                            gDiag_Lin.gAddrOffset = 0;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                    return false;
                }
            }
            else
            {
                gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);
                return false;
            }

            if (gDiag_Lin.m_bTransferDataOK)
            {
                //download finish
                byte[] respMsg = new byte[] { 0 };
                m_ReqMsg = new byte[] { 0x37 }; //transfer data flow exist request
                gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                if (respMsg[0] == 0x77)
                {
                    gDiag_Lin.IncludeTextMessage("Application file download succeed!");
                    return true;
                }
                else
                {
                    gDiag_Lin.NegativeMessage(0x37, respMsg);
                    return false;
                }        
            }
            return false;
        }

        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00)
        {         
            int nLoop = 0, nNegResp = 0;
            while (nLoop < nBlocks)
            {
                byte[] resp = new byte[8];
                gDiag_Lin.ReadMessage(ref resp);

                //finish 0x31 routine control wait
                if (resp[0] == 0x71 && resp[1] == 0x01 && resp[2] == 0xFF
                    && resp[3] == 0x00 && resp[4] == 0x00)
                {
                    return true;
                }
                else if (resp[0] == 0x71 && resp[1] == 0x01 && resp[2] == 0xFF      //finish earse memory response
                        && resp[3] == 0x44 && resp[4] == 0x00)
                {
                    return true;
                }
                else if (resp[0] == 0x71 && resp[1] == 0x01 && resp[2] == 0x02 && resp[3] == 0x02) //for CheckDependency
                {
                    if (resp[5] == 0x0)
                        return true;
                    else
                        return false;
                }
                else if (resp[0] == 0x74 && resp[1] == 0x40) //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = (resp[4] << 8) + resp[5];
                    return true;
                }
                else if (resp[0] == 0x76 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    return true;
                }
                else if (resp[0] == 0x37 + 0x40)
                {
                    return true;
                }
                else if (resp[0] == 0x50 && resp[1] == 0x02) //service mode switch
                {
                    return true;
                }
                else if (resp[0] == 0x51 && resp[1] == 0x01) //software reset
                {
                    return true;
                }
                else if (resp[0] == 0x51 && resp[1] == 0x03) //hard reset
                {
                    return true;
                }

                if (resp[0] == 0x7F)
                    nNegResp++;
                if (nNegResp > 5)
                    return false;

                //Send 0x3E 80 when ereasing
                //if ((nLoop * 10 / (REQ_3E_INTERVAL / 10)) == nLoop/2)
                //{
                //    m_ReqMsg = new byte[] { 0x3E, 0x80 };
                //    Write_LINMessage(m_ReqMsg, true);
                //}

                Thread.Sleep(10);
                nLoop++;
            }
            return false;
        }
    }

    internal class LIN_CBF_Flahing : FlashBase  //Chery CBF file flashing
    {
        #region member variabels

        int m_nFlashTimes; //marked for flash sequece(1st,send 0x31 01 DD 02 only; 2nd,do following request sequence)
        byte[] m_ReqMsg;

        byte P2ServerMaxHigh;
        byte P2ServerMaxLow;
        byte P2_Star_ServerMaxHigh; //P2*ServerMax (HighByte);
        byte P2_Star_ServerMaxLow; //P2*ServerMax (LowByte);

        #endregion

        public LIN_CBF_Flahing()
        {
            m_nFlashTimes = 0;
        }

        public override bool WriteThreadFunc_TH(object diag_lin)
        {
            int nBlocks = 0;
            bool bGetPositiveResp = false;
            bool bMainFlashOK = false;
            byte[] respMsg = new byte[8];

            bool bDID_Right = false;
            string strIniFile = string.Empty;
            byte[] writeDID = new byte[3];

            try
            {
                m_nFlashTimes = 0;                
                gDiag_Lin = (Diag_LIN)diag_lin;
                gDiag_Lin.m_nSourceIndex = 0;
                gDiag_Lin.m_bEnable_0x3E = false;
                gDiag_Lin.m_bBreakInDownloading = false;
                gDiag_Lin.SetDonwloadingStatus(true);//disable all of button which accoiate with diag message func when download start

                m_ReqMsg = new byte[] { 0x10, 0x01 }; //Default session
                if (gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg) == 0)
                {
                    gDiag_Lin.IncludeTextMessage("Message ID not correct.");
                    return false;
                }

                if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x01)
                {
                    m_ReqMsg = new byte[] { 0x10, 0x83 }; //Extension session
                    gDiag_Lin.Write_Message(m_ReqMsg);
                    //During the reprogramming process, a functional addressing message of $10 83 needs to be sent first to 
                    //request all ECUs in the network to enter the extended session mode. After sending the message, the
                    //Tester waits for 1s and then performs the subsequent operations.
                    Thread.Sleep(1000);

                    m_ReqMsg = new byte[] { 0x31, 0x01, 0x02, 0x03 }; // Programming Condition Check
                    gDiag_Lin.Write_Message(m_ReqMsg); 

                    bGetPositiveResp = Resp_TH(ref nBlocks, 350);
                    if (bGetPositiveResp)
                    {
                        m_ReqMsg = new byte[] { 0x28, 0x83, 0x03 }; //Communication Control
                        gDiag_Lin.Write_Message(m_ReqMsg);
                        Thread.Sleep(10);

                        /* Read Data By Identifier*/
                        //byte[] Resp = new byte[20];
                        //m_ReqMsg = new byte[] { 0x22, 0xF0, 0x13 }; // PartNumber                         
                        //ManauallyReadMessage(m_ReqMsg, ref Resp, 20);
                        //m_ReqMsg = new byte[] { 0x22, 0xF1, 0x87 }; // hardware fingerprint                         
                        //ManauallyReadMessage(m_ReqMsg, ref Resp, 20);
                        //m_ReqMsg = new byte[] { 0x22, 0xF1, 0x89 }; // software version 
                        //ManauallyReadMessage(m_ReqMsg, ref Resp, 20);
                        //m_ReqMsg = new byte[] { 0x22, 0xF1, 0x8A }; // SystemSupplier
                        //ManauallyReadMessage(m_ReqMsg, ref Resp, 20);
                        //_

                        m_ReqMsg = new byte[] { 0x10, 0x02 }; //Programme session
                        gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                        if (gDiag_Lin.m_RespMsg[0] == 0x50 && gDiag_Lin.m_RespMsg[1] == 0x02) 
                        {
                            byte[] resp0x27 = new byte[18];
                            m_ReqMsg = new byte[] { 0x27, 0x11 }; //SubFunc11
                            gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref resp0x27);

                            if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x11) //SubFunc11
                            {
                                byte[] reqKEY = new byte[18];
                                byte[] SeedArray = new byte[16];
                                byte[] KeyArray = new byte[16];
                                for (int i = 0; i < 16; i++)
                                    SeedArray[i] = resp0x27[i + 2];
                                KeyArray = fConvert.AES_128_CMAC(SeedArray, 0x11);   //according response seed caculate security access key

                                reqKEY[0] = 0x27;
                                reqKEY[1] = 0x12;                       // SubFunc12
                                reqKEY[2] = KeyArray[0];
                                reqKEY[3] = KeyArray[1];
                                reqKEY[4] = KeyArray[2];
                                reqKEY[5] = KeyArray[3];

                                reqKEY[6] = KeyArray[4];
                                reqKEY[7] = KeyArray[5];
                                reqKEY[8] = KeyArray[6];
                                reqKEY[9] = KeyArray[7];

                                reqKEY[10] = KeyArray[8];
                                reqKEY[11] = KeyArray[9];
                                reqKEY[12] = KeyArray[10];
                                reqKEY[13] = KeyArray[11];

                                reqKEY[14] = KeyArray[12];
                                reqKEY[15] = KeyArray[13];
                                reqKEY[16] = KeyArray[14];
                                reqKEY[17] = KeyArray[15];

                                gDiag_Lin.Send5TimeReqMsg(reqKEY, ref respMsg);
                                if (gDiag_Lin.m_RespMsg[0] == 0x67 && gDiag_Lin.m_RespMsg[1] == 0x12)
                                {
                                    gDiag_Lin.IncludeTextMessage("Security access pass.");

                                    //Enable TestPresent 0x3E  & message view rolling
                                    m_ReqMsg = new byte[] { 0x3E, 0x00 };

                                    gDiag_Lin.Write_Message(m_ReqMsg);
                                    Thread.Sleep(10);
                                    gDiag_Lin.ReadMessage(ref respMsg);
                                    if (respMsg[0] == 0x7E && respMsg[1] == 0x00)
                                    {
                                         //gDiag_Lin.m_bEnable_0x3E = true;
                                    }
                                    else
                                    {
                                        gDiag_Lin.IncludeTextMessage("0x3E service not work normally.");
                                        return false;
                                    }
                                 
                                    strIniFile = Directory.GetCurrentDirectory() + @"\DIDInfo.ini";
                                    //write DID(F18C, 008C）
                                    writeDID[0] = 0x2E;
                                    writeDID[1] = 0x00;
                                    writeDID[2] = 0x8C;
                                    bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "008C", writeDID, 36);//36

                                    if (!bDID_Right)
                                    {
                                        gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                        return false;
                                    }
                                    else
                                    {
                                        gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                        gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                    }
                                    // Fingerprint Data Writing
                                    writeDID[0] = 0x2E;
                                    writeDID[1] = 0xF1;
                                    writeDID[2] = 0x84;
                                    bDID_Right = gDiag_Lin.Excute_Write_DID(strIniFile, "F184", writeDID, 19);//

                                    if (!bDID_Right)
                                    {
                                        gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} failured.", BitConverter.ToString(writeDID)));
                                        return false;
                                    }
                                    else
                                    {
                                        gDiag_Lin.SetWriteDID_ButtonColor("Write DID", Color.Transparent);
                                        gDiag_Lin.IncludeTextMessage(string.Format("Write DID::{0} succeed.", BitConverter.ToString(writeDID)));
                                    }
                                    //_

                                    //Process of downloading traditional component files
                                    bMainFlashOK = Download_Finish();
                                    if (bMainFlashOK)
                                        gDiag_Lin.IncludeTextMessage("Application file has been finished download.");                                    
                                }
                                else
                                    gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                            }
                            else
                                gDiag_Lin.NegativeMessage(0x27, gDiag_Lin.m_RespMsg);
                        }
                    }
                }
                else
                    gDiag_Lin.NegativeMessage(0x10, gDiag_Lin.m_RespMsg);

                //Back flashing step
                if (bMainFlashOK)
                {
                    if (m_nFlashTimes == 0)
                        return true;

                    //CommunicationControl switch on
                    m_ReqMsg = new byte[] { 0x28, 0x80, 0x03 };
                    gDiag_Lin.Write_Message(m_ReqMsg);
                    Thread.Sleep(10);

                    gDiag_Lin.IncludeTextMessage("ECU will reboot,please wait for a moment.");

                    m_ReqMsg = new byte[] { 0x11, 0x01 }; //ECU soft reset
                    gDiag_Lin.Write_Message(m_ReqMsg);
                    int nBlockNum = 0;
                    bGetPositiveResp = Resp_TH(ref nBlockNum, 50);
                    if (bGetPositiveResp)
                    {
                        gDiag_Lin.IncludeTextMessage("ECU soft reset succeed.");
                        gDiag_Lin.IncludeTextMessage("Fireware download succeed.");
                        Thread.Sleep(500);

                        m_ReqMsg = new byte[] { 0x10, 0x03 }; //Extended Session
                        gDiag_Lin.Write_Message(m_ReqMsg);

                        bGetPositiveResp = Resp_TH(ref nBlockNum, 50);
                        if (bGetPositiveResp)
                        {
                            m_ReqMsg = new byte[] { 0x14, 0xFF, 0xFF, 0xFF }; //Clear DTC
                            gDiag_Lin.Write_Message(m_ReqMsg);
                            Thread.Sleep(10);
                            m_ReqMsg = new byte[] { 0x10, 0x81 }; //Default Session
                            gDiag_Lin.Write_Message(m_ReqMsg);

                            gDiag_Lin.m_bEnable_0x3E = false;

                            gDiag_Lin.UpdateProgerss(100);
                            gDiag_Lin.RefreshDBGridView(); //refresh trace grid view for display newest message
                            gDiag_Lin.SetDonwloadingStatus(false);
                        }
                    }
                    else
                        gDiag_Lin.NegativeMessage(0x11, gDiag_Lin.m_RespMsg);
                }
                else
                    return false;
            }
            catch (IOException ep)
            {
                gDiag_Lin.IncludeTextMessage(ep.Message);
            }
            return true;
        }

        protected override void UpgrateFirmware(object BINADDRINFO)
        {
            try
            {
                int n0x36PackNum0 = 1;
                int nMaxNumOfBlock = 0;
                int nProgressStep = 0;
                int read_data_num = 0;
                int AddrOffset = 0;
                bool bGetPositiveResp = false;
                byte[] DataBuffer = new Byte[] { };
                FlashFirewareHandlerForTP90 ffhHandler = new FlashFirewareHandlerForTP90(FlashFirmware_TH_TP90);

                //ECU feedback max number of block size.
                Diag_LIN._Bin_Addr_Len binAddrInfo = (Diag_LIN._Bin_Addr_Len)BINADDRINFO;
                PACK_SIZE = (int)binAddrInfo.MaxBlockSize;
                gDiag_Lin.m_FirmwareFileSize = binAddrInfo.BlockLen;

                int nCurrPackPos = 0;
                int nMaxBlockSize = 0;
                byte[] _0x36DataPack = binAddrInfo.TotalData;

                if(_0x36DataPack.Length % PACK_SIZE == 0)
                {
                    nMaxBlockSize = _0x36DataPack.Length / PACK_SIZE;
                }
                else
                {
                    nMaxBlockSize = _0x36DataPack.Length / PACK_SIZE + 1;
                }

                lock (Diag_LIN.m_obj)
                {
                    for (AddrOffset = 0; AddrOffset < gDiag_Lin.m_FirmwareFileSize;)
                    {       
                        if (nCurrPackPos < nMaxBlockSize - 1)
                        {
                            read_data_num = PACK_SIZE;
                            DataBuffer = new byte[PACK_SIZE];
                            gDiag_Lin.m_nSourceIndex = nCurrPackPos * PACK_SIZE;
                            Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, PACK_SIZE);
                        }
                        else //last block copy
                        {
                            int nLastPackSize = 0;
                            nLastPackSize = _0x36DataPack.Length % PACK_SIZE;
                            if (nLastPackSize > 0) 
                            {
                                read_data_num = nLastPackSize;
                                DataBuffer = new byte[nLastPackSize];
                                gDiag_Lin.m_nSourceIndex = nCurrPackPos * PACK_SIZE;
                                Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, read_data_num);
                            }
                            else
                            {
                                read_data_num = PACK_SIZE;
                                DataBuffer = new byte[PACK_SIZE];
                                gDiag_Lin.m_nSourceIndex = nCurrPackPos * PACK_SIZE;
                                Array.Copy(_0x36DataPack, gDiag_Lin.m_nSourceIndex, DataBuffer, 0, PACK_SIZE);
                            }
                        }

                        if (read_data_num != PACK_SIZE) //if last package size not equal PACK_SIZE(0x80)
                        {
                            gDiag_Lin.BeginInvoke(ffhHandler, new object[] { DataBuffer, 100, gDiag_Lin.m_n0x36PackNum++ });
                        }
                        else
                        {
                            Thread.Sleep(100);
                            nProgressStep = (int)(((float)(AddrOffset + read_data_num) / (float)gDiag_Lin.m_FirmwareFileSize) * 100.0f);
                            gDiag_Lin.BeginInvoke(ffhHandler, new object[] { DataBuffer, nProgressStep, gDiag_Lin.m_n0x36PackNum++ });
                        }

                        //wait for single block write response
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 10, gDiag_Lin.m_n0x36PackNum); }));
                        gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage(string.Format("Now downloading fireware block::{0:d}", n0x36PackNum0++)); }));

                        if (!bGetPositiveResp)
                        {
                            gDiag_Lin.m_bTransferDataOK = false;
                            gDiag_Lin.Invoke(new MethodInvoker(delegate () { gDiag_Lin.IncludeTextMessage("downloading fireware failure."); }));
                            break;
                        }

                        if (gDiag_Lin.m_n0x36PackNum > 0xFF)
                            gDiag_Lin.m_n0x36PackNum = 0;

                        AddrOffset += read_data_num;
                        nCurrPackPos++;

                    }
                }
            }
            catch (IOException ioEx)
            {
                gDiag_Lin.IncludeTextMessage(ioEx.Message);
                return;
            }
        }

        protected override int FlashFirmware_TH_TP90(byte[] buffer, int nCurrIndex, int n0x36PackCnt)
        {
            /*A single application software/data block might require multiple TransferData (0x36) request messages to be
                  completely transmitted (this is the case if the length of the block exceeds the maximum network layer buffer size).*/
            byte[] _36Svr_Times = new byte[] { 0 };
            byte bTimes0 = Convert.ToByte(n0x36PackCnt);
            _36Svr_Times = gDiag_Lin.Combine(new byte[] { 0x36 }, new byte[] { bTimes0 });
            buffer = gDiag_Lin.Combine(_36Svr_Times, buffer);

            gDiag_Lin.Write_Message(buffer);
            gDiag_Lin.UpdateProgerss(nCurrIndex);

            return buffer.Length; //read_data_num;
        }

        protected override bool Download_Finish()
        {
            int nIdx = 0; //0: flashdv(only download); 1:flash data(need excute erasememory before download)
            int nMaxNumOfBlock = 0;
            bool bGetPositiveResp = false;
            Diag_LIN._Bin_Addr_Len BAL = new Diag_LIN._Bin_Addr_Len();
            byte[] newBlockSize = new byte[4];

            //cause app.hex data be CBF tool fill in list first, second fill with flasshdrive.hex data(we need download flashdriver.hex data first)
            //CBFParserBase.DataBlock DB;
            //for (int i = gDiag_Lin.m_CBFParser.m_FlashDataLst.Count - 1; i>=0; i--) //for 1 .cbf that include 2 block data file use only 

            foreach (Diag_LIN.CBFParser.DataBlock DB in gDiag_Lin.m_CBFParser.m_FlashDataLst)
            {
                //DB = gDiag_Lin.m_CBFParser.m_FlashDataLst[i];
                //'Release' button pressed when downloading
                if (gDiag_Lin.m_bBreakInDownloading)
                {
                    gDiag_Lin.IncludeTextMessage("Downloading be interrupted by user.");
                    return false;
                }

                if (nIdx++ == 1)
                {
                    //Earse command
                    m_ReqMsg = new byte[] { 0x31, 0x01, 0xFF, 0x00, 0x44 };
                    string strEraseAddr = Convert.ToString(DB.StartAddr_Block, 16);
                    byte[] EraseAddr0 = gDiag_Lin.HexStringToByteArray(strEraseAddr);
                    byte[] EraseAddr = gDiag_Lin.Combine(m_ReqMsg, EraseAddr0);

                    //download size & address combine
                    string strEraseLEN = Convert.ToString(DB.Length_Block, 16);
                    byte[] EraseMemorySize = gDiag_Lin.HexStringToByteArray(strEraseLEN);

                    //request download command + memory address + memory size
                    byte[] Total31EraseReq = gDiag_Lin.Combine(EraseAddr, EraseMemorySize);

                    //cause test following diagnostic service,so marked now20240119
                    gDiag_Lin.Write_Message(Total31EraseReq); // new earsing command
                    //waitting for earse finish
                    bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 100);
                    if (bGetPositiveResp)
                    {
                        m_nFlashTimes = 1;
                        gDiag_Lin.IncludeTextMessage("Ecu's application be earsed.");
                        gDiag_Lin.IncludeTextMessage("System will download application file.");
                    }
                    else
                    {
                        gDiag_Lin.NegativeMessage(0x31, gDiag_Lin.m_RespMsg);
                        gDiag_Lin.IncludeTextMessage("Some issue occure when earse ecu's application file.");
                        return false;
                    }
                }

                //memory address for download fireware
                //gDiag_Lin.MEMORY_ADDR,       gDiag_Lin.MEMORY_SIZE   
                m_ReqMsg = new byte[] { 0x34, 0x00, 0x44 };
                string strDownloadAddr = Convert.ToString(DB.StartAddr_Block, 16);
                byte[] DownloadAddr0 = gDiag_Lin.HexStringToByteArray(strDownloadAddr);
                byte[] DownloadAddr = gDiag_Lin.Combine(m_ReqMsg, DownloadAddr0);

                //download size & address combine
                string strDownloadLEN = Convert.ToString(DB.Length_Block, 16);
                byte[] MemorySize = gDiag_Lin.HexStringToByteArray(strDownloadLEN);

                //request download command + memory address + memory size
                byte[] Total34Req = gDiag_Lin.Combine(DownloadAddr, MemorySize);
                gDiag_Lin.Write_Message(Total34Req);

                bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 5);
                if (bGetPositiveResp)
                {
                    gDiag_Lin.IncludeTextMessage(string.Format("Data transfer start block::{0}.", nIdx));
                    try
                    {
                        BAL.StartAddress = DB.StartAddr_Block;
                        BAL.BlockLen = DB.Length_Block;
                        BAL.MaxBlockSize = (uint)nMaxNumOfBlock;
                        BAL.TotalData = new byte[BAL.BlockLen];
                        BAL.TotalData = DB.Data;

                        gDiag_Lin.m_WriteThread = new System.Threading.Thread(UpgrateFirmware);
                        gDiag_Lin.m_WriteThread.IsBackground = true;
                        gDiag_Lin.m_WriteThread.Start(BAL);

                        bool IfTimesEnd = false;
                        bool IfRunOver = false;
                        while (!IfRunOver && gDiag_Lin.m_WriteThread != null || !gDiag_Lin.m_bTransferDataOK)
                        {
                            IfTimesEnd = gDiag_Lin.m_WriteThread.IsAlive;
                            Application.DoEvents();
                            if (!IfTimesEnd || IfRunOver)
                            {
                                gDiag_Lin.m_WriteThread.Interrupt();
                                gDiag_Lin.m_WriteThread.Abort();
                                IfTimesEnd = false;
                                gDiag_Lin.gAddrOffset = 0;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        gDiag_Lin.IncludeTextMessage(string.Format("Some issue occured::{0:s} when transfer data.", ex.Message));
                        return false;
                    }
                    finally
                    {
                        if (!gDiag_Lin.m_bBreakInDownloading)
                            gDiag_Lin.IncludeTextMessage(string.Format("Transfer data succeed."));
                    }
                }
                else
                {
                    gDiag_Lin.NegativeMessage(0x34, gDiag_Lin.m_RespMsg);
                    return false;
                }

                //reset 0x36 transfer data sequence number after 1 block transfered.
                gDiag_Lin.m_n0x36PackNum = 1;
                gDiag_Lin.m_nTP90_ReadAddr_Times = 0;
                gDiag_Lin.m_FirmwareFileSize = 0;

                if (gDiag_Lin.m_bTransferDataOK && !gDiag_Lin.m_bBreakInDownloading)
                {
                    //download finish
                    byte[] respMsg = new byte[] { 0 };
                    m_ReqMsg = new byte[] { 0x37 }; //Security access,request seed
                    gDiag_Lin.Send5TimeReqMsg(m_ReqMsg, ref respMsg);

                    if (respMsg[0] == 0x77)
                    {
                        //Security Signature Verification (CheckSum verify)
                        m_ReqMsg = new byte[] { 0x31, 0x01, 0xDD, 0x02 };
                        byte[] LardgeBytes = new byte[32];      //[384];
                        m_ReqMsg = gDiag_Lin.Combine(m_ReqMsg, LardgeBytes);

                        gDiag_Lin.Write_Message(m_ReqMsg);

                        bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, 550);
                        if (bGetPositiveResp)
                        {
                            gDiag_Lin.IncludeTextMessage("Application file download succeed!");
                        }
                        else
                        {
                            gDiag_Lin.IncludeTextMessage("Security Signature Verification fauilure!");
                            return false;
                        }
                    }
                    else
                    {
                        gDiag_Lin.NegativeMessage(0x37, respMsg);
                        return false;
                    }
                }              
            }

            return true;
        }

        protected override bool Resp_TH(ref int nMaxNumOfBlockLen, int nBlocks, int nDownloadTimes = 0, ushort reqID = 0x00)
        {
            byte[] resp = new byte[8];
            int nLoop = 0, nNegResp = 0;

            while (nLoop < nBlocks)
            {
                gDiag_Lin.ReadMessage(ref resp);

                //finish 0x31 routine control wait
                if (resp[0] == 0x71 && resp[1] == 0x01                                      //Erase complete?
                    && resp[2] == 0xFF && resp[3] == 0x00)
                {
                    if (resp[4] == 0x00)
                        return true;
                    else
                        return false;
                }
                else if (resp[0] == 0x71 && resp[1] == 0x01                               //Security Signature Verification
                        && resp[2] == 0xDD && resp[3] == 0x02) 
                {
                    if (resp[4] == 0x00)
                        return true;
                    else
                        return false;
                }
                else if (resp[0] == 0x71 && resp[1] == 0x01                               // Programming Condition Check
                    && resp[2] == 0x02 && resp[3] == 0x03)
                {
                    if (resp[4] == 0x00)
                        return true;
                    else
                        return false;
                }
                else if (resp[0] == 0x74 && resp[1] == 0x40) //get MaxNumberOfBlockLength in 0x34 service response msg
                {
                    nMaxNumOfBlockLen = resp[5] - 2;        //(resp[1] >> 4) + resp[5] - 2; 
                    return true;
                }
                else if (resp[0] == 0x76 /*&& resp[1] == nDownloadTimes*/)//finish file data transfer
                {
                    return true;
                }
                else if (resp[0] == 0x37 + 0x40)
                {
                    return true;
                }
                else if (resp[0] == 0x50 && resp[1] == 0x01) //default session switch
                {
                    P2ServerMaxHigh = resp[2];
                    P2ServerMaxLow = resp[3];
                    P2_Star_ServerMaxHigh = resp[4];
                    P2_Star_ServerMaxLow = resp[5];
                    return true;
                }
                else if (resp[0] == 0x50 && resp[1] == 0x02) //service mode switch
                {
                    return true;
                }
                else if (resp[0] == 0x50 && resp[1] == 0x03) //Extension session
                {
                    return true;
                }
                else if (resp[0] == 0x51 && resp[1] == 0x01) //software reset
                {
                    return true;
                }
                else if (resp[0] == 0x51 && resp[1] == 0x03) //hard reset
                {
                    return true;
                }
                else if (resp[0] == 0x54) //clear DTC
                {
                    return true;
                }
                else if (resp[0] == 0x6E || resp[0] == 0x62) //response read/write DID
                {
                    return true;
                }

                if (resp[0] == 0x7F)
                {
                    resp[0] = 0x0;
                    resp[1] = 0x0;
                    resp[3] = 0x0;
                    nNegResp++;
                }
                if (nNegResp > 20)
                    return false;

                Thread.Sleep(10);
                nLoop++;
            }
            return false;
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
                gDiag_Lin.m_bReadWriteDID = true; //read response message manually
                gDiag_Lin.Write_DID_CANMessage(ReadDID, true);

                bGetPositiveResp = Resp_TH(ref nMaxNumOfBlock, P2_ServerTime, 0, 0x22);
                if (bGetPositiveResp)
                {
                    if (tmpRespMsg.Length > 3)
                    {
                        Array.Copy(gDiag_Lin.m_RespMsg, 5, tmpRespMsg, 0, 3);
                        n += 3;
                    }
                    else
                    {
                        Array.Copy(gDiag_Lin.m_RespMsg, 5, tmpRespMsg, 0, tmpRespMsg.Length);
                        n += tmpRespMsg.Length;
                        respMsg = tmpRespMsg;
                        nResult = 0;
                        gDiag_Lin.m_bReadWriteDID = false; //read response message manually

                        return nResult;
                    }

                    ////follow ctrl frame request for get residue response bytes
                    byte[] ReadBuf = new byte[8];
                    byte[] FollowCtrl = new byte[1] { 0x30 };
                    gDiag_Lin.Write_DID_CANMessage(FollowCtrl, true, true);
                    Thread.Sleep(P2_ServerTime * 3);

                    int nEndBytes = 0, BlockSize = 7;
                    nResidueBytes = nRespMsgLen - 3;

                    gDiag_Lin.ReadMessage(ref ReadBuf);
                    if (nResidueBytes > BlockSize)
                        nEndBytes = nResidueBytes % BlockSize;
                    else
                        nEndBytes = 0; //resedue bytes less than 7 bytes

                    int n7ByteGroups = nResidueBytes / BlockSize;
                    for (int i = 0; i < n7ByteGroups; i++)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, BlockSize);
                        Thread.Sleep(P2_ServerTime * 3);
                        gDiag_Lin.ReadMessage(ref ReadBuf);
                        n += 7;
                    }
                    if (nEndBytes > 0)
                    {
                        Array.Copy(ReadBuf, 1, tmpRespMsg, n, nEndBytes);
                    }

                    respMsg = tmpRespMsg;
                }
                nResult = 0;
                gDiag_Lin.m_bReadWriteDID = false; //read response message manually
            }
            catch { }

            return nResult;
        }
    }

    internal class Context : IDisposable
    {
        private FlashBase _stg;
        private Object m_DiagLIN;
        private bool disposedValue;

        public Context(FlashBase stg, Object diag_lin) 
        {
            _stg = stg; 
            m_DiagLIN = diag_lin; 
        }

        public void DoDownload() 
        {
            _stg.WriteThreadFunc_TH(m_DiagLIN);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    class Flashing
    {
        internal enum Type : byte
        {
            _320vCompresor = 0,
            _400vCompresor = 1,
            _xc2234 = 2,
            _7Kw = 3,
            _N2S = 4,
            _CANUDS40 = 5,  //ac7840
            _CANUDS01 = 6, //ac7801
            _LINHex = 7,
            _SplitCAN = 8,
            _CheryCBF = 9,
        }       
        internal Type ProjectType{get; set;}
        FlashBase ps;
        Context pc;

        public Flashing() { }
        ~Flashing()
        {
            if(pc!=null)
                pc.Dispose();
        }
        public Flashing(object diag_lin_obj, byte Project) 
        {
            bool IfTimesEnd = false;
            bool IfRunOver = false;

            ProjectType = (Type)Project;
            switch(ProjectType)
            {
                case Type._320vCompresor:
                    ps = new CAN_Flashing();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._400vCompresor:

                    break;
                case Type._7Kw:
                    ps = new LIN_Flashing_Bin();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._N2S:
                    ps = new N2S_CAN_Flahing();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._xc2234:

                    break;
                case Type._CANUDS40:
                    ps = new CANUDS40_Flahing();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._CANUDS01:
                    ps = new CANUDS01_Flahing();
                    pc = new Context(ps, diag_lin_obj);

                    ((Diag_LIN)diag_lin_obj).m_ReadDTCEvent.Reset();
                    ((Diag_LIN)diag_lin_obj).m_WriteThread = new System.Threading.Thread(pc.DoDownload);
                    ((Diag_LIN)diag_lin_obj).m_WriteThread.IsBackground = true;
                    ((Diag_LIN)diag_lin_obj).m_WriteThread.Start();

                    while (!IfRunOver && ((Diag_LIN)diag_lin_obj).m_WriteThread != null)
                    {
                        IfTimesEnd = ((Diag_LIN)diag_lin_obj).m_WriteThread.IsAlive;
                        Application.DoEvents();
                        if (!IfTimesEnd || IfRunOver || !((Diag_LIN)diag_lin_obj).m_bTransferDataOK)
                        {
                            ((Diag_LIN)diag_lin_obj).m_WriteThread.Interrupt();
                            ((Diag_LIN)diag_lin_obj).m_WriteThread.Abort();
                            IfTimesEnd = false;
                            ((Diag_LIN)diag_lin_obj).gAddrOffset = 0;
                            break;
                        }
                    }
                    break;
                case Type._LINHex:
                    ps = new LIN_Flashing_Hex();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._SplitCAN:
                    ps = new Split_CANUDS_Flahing();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;
                case Type._CheryCBF:    //lin bus
                    ps = new LIN_CBF_Flahing();
                    pc = new Context(ps, diag_lin_obj);
                    pc.DoDownload();
                    break;

                default:
                    break;
            }
        }
    }

}
