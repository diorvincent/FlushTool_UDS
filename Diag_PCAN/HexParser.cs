/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Nov 1, 2023
 * Author: He Jingchi
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;

namespace Diag_BUS
{
    public class HexParser
    {
        int m_nProjectNum = 0;
        string m_strFileName = string.Empty;
        public struct RecordAddrInfo
        {
            public uint uBaseAddress;   //current block base address
            public uint uEndAddress;    //a data block end address
            public uint uPerRecordAddress;//per record address
            public int uRecordLength;   //a data block length
            public int nBlockNum;       //block number
            public int nTotalLen;       //hex total length
            public byte[] Data;
        }
        RecordAddrInfo m_recAddrInfo;
        RecordAddrInfo m_recDataInfo;
        public List<RecordAddrInfo> lstRecAddr;
        public List<RecordAddrInfo> lstRecData;

        public HexParser(string strFileName, int nPrjNum=0)
        {
            m_strFileName = strFileName;
            lstRecAddr = new List<RecordAddrInfo>();
            lstRecData = new List<RecordAddrInfo>();

            m_nProjectNum = nPrjNum;
        }

        public List<RecordAddrInfo> ReadHex()
        {
            int nTotalDataLen = 0;
            int nLastDataSegRecLen = 0;
            int nBlockNum = 0;
            int nLineNum = 0;
            uint uStartAddr = 0;
            uint uExtendAddr = 0;
            uint uEndAddr = 0;
            bool bBlockChangEnd = false;
            bool bExtendAddr = false;
            string strLine = ":";
            
            m_recAddrInfo = new RecordAddrInfo();
            m_recDataInfo = new RecordAddrInfo();
            StreamReader sr = new StreamReader(m_strFileName);

            while(true)
            {
                strLine = sr.ReadLine();
                if (strLine == null)
                    break;

                Record rec = Record.Parse(strLine);
                if(rec!=null)
                {
                    if (rec.RecordType == Record.Type.DATA_RECORD)
                    {
                        DataRecord esar = (DataRecord)rec;
                        if (bBlockChangEnd)
                        {
                            uEndAddr = esar.LoadOffset; 
                            m_recAddrInfo.uBaseAddress = uStartAddr + uEndAddr;
                            bBlockChangEnd = false;
                        }

                        nLastDataSegRecLen += esar.RecordLength;
                        nTotalDataLen += esar.RecordLength;

                        if (bExtendAddr)
                        {
                            uEndAddr = esar.LoadOffset;
                            m_recAddrInfo.uRecordLength = esar.RecordLength;
                            m_recAddrInfo.uBaseAddress = uExtendAddr + uEndAddr;
                            lstRecAddr.Add(m_recAddrInfo);

                            bExtendAddr = false;
                        }

                        //add data record
                        m_recDataInfo.uRecordLength = esar.RecordLength;
                        m_recDataInfo.uBaseAddress = m_recAddrInfo.uBaseAddress;
                        m_recDataInfo.uPerRecordAddress = (uint)(nTotalDataLen - esar.RecordLength);    //uStartAddr + esar.LoadOffset;  
                        m_recDataInfo.Data = esar.Data;
                        lstRecData.Add(m_recDataInfo);
                    }
                    else if (rec.RecordType == Record.Type.EXTEND_SEGMENT_ADDRESS_RECORD)
                    {
                        ExtendSegmentAddressRecord esar = (ExtendSegmentAddressRecord)rec;
                        uExtendAddr = esar.ExtendSegmentBaseAddress;
                        bExtendAddr = true;
                    }
                    else if (rec.RecordType == Record.Type.START_SEGMENT_ADDRESS_RECORD)
                    {
                        StartSegmentAddressRecord ssar = (StartSegmentAddressRecord)rec;
                    }
                    else if (rec.RecordType == Record.Type.EXTEND_LINEAR_ADDRESS_RECORD)
                    {
                        ExtendLinearAddressRecord elar = (ExtendLinearAddressRecord)rec;

                        nBlockNum++;
                        if(nBlockNum>1)
                        {
                            m_recAddrInfo.nBlockNum++;
                            m_recAddrInfo.uRecordLength = nLastDataSegRecLen;
                            m_recAddrInfo.uEndAddress = m_recAddrInfo.uBaseAddress + (uint)(nLastDataSegRecLen - 1);
                            lstRecAddr.Add(m_recAddrInfo);
                            uStartAddr = elar.UpperLinearBaseAddress;

                            uEndAddr = 0;
                            nBlockNum = 1;
                        }
                        else if(nBlockNum == 1)
                        {
                            uStartAddr = elar.UpperLinearBaseAddress;
                        }
                        nLastDataSegRecLen = 0;
                        bBlockChangEnd = true;
                    }
                    else if (rec.RecordType == Record.Type.START_LINEAR_ADDRESS_RECORD)
                    {
                        StartLinearAddressRecord slar = (StartLinearAddressRecord)rec;
                        //add data record
                        if(m_nProjectNum != 0x4)
                        {
                            m_recDataInfo.uRecordLength = slar.RecordLength;
                            m_recDataInfo.Data = slar.Data;
                            lstRecData.Add(m_recDataInfo);
                        }
                    }
                    else if (rec.RecordType == Record.Type.END_OF_FILE_RECORD)
                    {
                        EndOfFileRecord efr = (EndOfFileRecord)rec;

                        m_recAddrInfo.nTotalLen = nTotalDataLen;
                        m_recAddrInfo.nBlockNum++;
                        m_recAddrInfo.uBaseAddress = uStartAddr + uEndAddr ;
                        m_recAddrInfo.uRecordLength = nLastDataSegRecLen;
                        m_recAddrInfo.uEndAddress = m_recAddrInfo.uBaseAddress + (uint)(nLastDataSegRecLen - 1);
                        lstRecAddr.Add(m_recAddrInfo);
                    }
                }
                nLineNum++;//line number plus plus after read info of hex file 1 line
            }

            sr.Close();
            return lstRecAddr;
        }
    }

    //Hex文件结构基类
    internal abstract class Record
    {
        protected Boolean m_bAvailable = false;
        public Boolean Available
        {
            get { return m_bAvailable; }
        }

        internal enum Type : byte
        {
            DATA_RECORD = 0,                    //数据记录
            END_OF_FILE_RECORD = 1,             //文件结尾
            EXTEND_SEGMENT_ADDRESS_RECORD = 2,  //扩展段地址记录
            START_SEGMENT_ADDRESS_RECORD = 3,   //开始段地址记录
            EXTEND_LINEAR_ADDRESS_RECORD = 4,   //扩展线性地址
            START_LINEAR_ADDRESS_RECORD = 5     //开始线性地址记录
        }
        //数据段类型
        public abstract Type RecordType
        {
            get;
        }

        //记录长度
        public abstract Int32 RecordLength
        {
            get;
        }

        //每条记录都有一个LOAD OFFSET字段
        public virtual UInt16 LoadOffset
        {
            get { return 0; }
        }

        public abstract Byte[] Data
        {
            get;
        }

        /*
         * 解析记录字符串的简单方法
         * param tHexRecord一个记录字符串返回对新记录对象的引用   
         */
        public static Record Parse(String tHexRecord)
        {
            // check input
            if (null == tHexRecord)
            {
                return null;
            }

            tHexRecord = tHexRecord.Trim().ToUpper();
            if ("" == tHexRecord)
            {
                return null;
            }

            // check record head
            if (':' != tHexRecord[0])
            {
                return null;
            }
            if (!IsHexNumber(tHexRecord.Substring(1)))
            {
                return null;
            }

            Int32 tRecordLength = 0;
            Int32 tCheckSUM = 0;
            Int32 tLoadOffSet = 0;
            Byte tRecordType = 0;
            Byte[] tData = null;

            //try to get record length
            try
            {
                tRecordLength = Int32.Parse(tHexRecord.Substring(1, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return null;
            }
            tCheckSUM += tRecordLength;

            // check record string length
            if (tHexRecord.Length < (11 + tRecordLength * 2))
            {
                //! incomplete record
                return null;
            }

            // try to get LoadOffset
            try
            {
                tLoadOffSet = UInt16.Parse(tHexRecord.Substring(3, 4), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return null;
            }
            tCheckSUM += tLoadOffSet >> 8;
            tCheckSUM += tLoadOffSet & 0xFF;

            // try to get record type
            try
            {
                tRecordType = Byte.Parse(tHexRecord.Substring(7, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return null;
            }

            // check record type value
            if (tRecordType > 5)
            {
                return null;
            }
            else if (((Type)tRecordType != Type.DATA_RECORD)
                    && (tLoadOffSet != 0))
            {
                // illegal record loadoffset
                return null;
            }

            tCheckSUM += tRecordType;

            // get data byte
            tData = new Byte[tRecordLength];
            if (null == tData)
            {
                // failed to allocated memory
                return null;
            }

            // read all data bytes in a record
            for (Int32 n = 0; n < tData.Length; n++)
            {
                try
                {
                    tData[n] = Byte.Parse(tHexRecord.Substring(9 + n * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
                }
                catch (Exception Err)
                {
                    Err.ToString();
                    return null;
                }
                tCheckSUM += tData[n];
            }

            // get check sum
            try
            {
                tCheckSUM += Byte.Parse(tHexRecord.Substring(9 + tData.Length * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return null;
            }
            if (0 != (tCheckSUM & 0xFF))
            {
                // check sum error
                return null;
            }

            // check record type
            switch ((Type)tRecordType)
            {
                case Type.DATA_RECORD:                              //data record
                    return new DataRecord((UInt16)tLoadOffSet, tData);

                case Type.END_OF_FILE_RECORD:                       //EOF record
                    return new EndOfFileRecord();

                case Type.EXTEND_LINEAR_ADDRESS_RECORD:             //extend linear address record
                    return new ExtendLinearAddressRecord(tData);

                case Type.EXTEND_SEGMENT_ADDRESS_RECORD:            //extend segment address
                    return new ExtendSegmentAddressRecord(tData);

                case Type.START_LINEAR_ADDRESS_RECORD:              //start linear address record
                    return new StartLinearAddressRecord(tData);

                case Type.START_SEGMENT_ADDRESS_RECORD:
                    return new StartSegmentAddressRecord(tData);    //start segment address

                //default:
                //    // no such type
                //    return null;
            }

            return null;
        }

        //获取十六进制字符串
        public override String ToString()
        {
            StringBuilder tsbHexRecord = new StringBuilder();
            if (m_bAvailable)
            {
                return null;
            }

            UInt32 tCheckSUM = 0;

            // record head
            tsbHexRecord.Append(':');

            // record length
            tsbHexRecord.Append(this.RecordLength.ToString("X2"));
            tCheckSUM += (UInt32)RecordLength;

            // record load offset
            tsbHexRecord.Append(this.LoadOffset.ToString("X4"));
            tCheckSUM += (UInt32)(LoadOffset & 0x00FF);
            tCheckSUM += (UInt32)(LoadOffset >> 8);

            // record type
            tsbHexRecord.Append(((Byte)this.RecordType).ToString("X2"));
            tCheckSUM += (Byte)RecordType;

            // record data
            foreach (Byte tItem in Data)
            {
                tsbHexRecord.Append(tItem.ToString("X2"));
                tCheckSUM += tItem;
            }

            // record check sum
            tsbHexRecord.Append(((Byte)((UInt16)0x100 - (UInt16)(tCheckSUM & 0xFF))).ToString("X2"));

            return tsbHexRecord.ToString();
        }

        //用于检查字符串是否仅由十六进制值组成
        static public Boolean IsHexNumber(String tHexString)
        {
            if (null == tHexString)
            {
                return false;
            }

            tHexString = tHexString.Trim().ToUpper();
            if ("" == tHexString)
            {
                return false;
            }

            foreach (Char tSymble in tHexString.ToCharArray())
            {
                if (!Char.IsNumber(tSymble))
                {
                    if ((tSymble < 'A') || (tSymble > 'F'))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// 文件结束记录指定十六进制对象文件的结束
    /// </summary>
    internal class EndOfFileRecord : Record
    {
        // length always is zero.
        public override int RecordLength
        {
            get { return 0; }
        }


        // get record type
        public override Record.Type RecordType
        {
            get { return Type.END_OF_FILE_RECORD; }
        }

        //no data
        public override byte[] Data
        {
            get
            {
                return new Byte[0];
            }
        }
    }
    /// <summary>
    /// 32位扩展线性地址记录
    /// </summary>
    internal class ExtendLinearAddressRecord : Record
    {
        private UInt16 m_UpperLinearBaseAddress = 0;
        private Byte[] m_Data = null;

        public ExtendLinearAddressRecord(Byte[] tData)
        {
            if (null == tData)
            {
                return;
            }
            else if (2 != tData.Length)
            {
                return;
            }

            m_Data = tData;

            try
            {
                Byte[] tTemp = new Byte[2];
                tTemp[0] = tData[1];
                tTemp[1] = tData[0];

                m_UpperLinearBaseAddress = BitConverter.ToUInt16(tTemp, 0);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return;
            }

            m_bAvailable = true;
        }

        //记录数据长度应该总是2
        public override Int32 RecordLength
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }
                return 2;
            }
        }


        //记录类型
        public override Record.Type RecordType
        {
            get { return Type.EXTEND_LINEAR_ADDRESS_RECORD; }
        }

        //获取数据
        public override Byte[] Data
        {
            get
            {
                if (!m_bAvailable)
                {
                    return null;
                }

                return m_Data;
            }
        }

        //获取上线性基址
        public UInt32 UpperLinearBaseAddress
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }
                return ((UInt32)m_UpperLinearBaseAddress) << 16;
            }
        }

    }
    /// <summary>
    /// 开始段地址记录
    /// </summary>
    internal class StartSegmentAddressRecord : Record
    {
        private UInt32 m_StartSegmentAddress = 0;
        private Byte[] m_Data = null;

        public StartSegmentAddressRecord(Byte[] tData)
        {
            if (null == tData)
            {
                return;
            }
            else if (4 != tData.Length)
            {
                return;
            }

            m_Data = tData;

            try
            {
                Byte[] tTemp = new Byte[4];
                tTemp[0] = tData[3];
                tTemp[1] = tData[2];
                tTemp[2] = tData[1];
                tTemp[3] = tData[0];

                m_StartSegmentAddress = BitConverter.ToUInt32(tTemp, 0);
            }
            catch (Exception Err)
            {
                Err.ToString();
            }

            m_bAvailable = true;
        }

        //永远返回 4
        public override int RecordLength
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return 4;
            }
        }

        //获取记录类型
        public override Record.Type RecordType
        {
            get { return Type.START_SEGMENT_ADDRESS_RECORD; }
        }

        //获取数据
        public override Byte[] Data
        {
            get
            {
                if (!m_bAvailable)
                {
                    return null;
                }

                return m_Data;
            }
        }

        //获取开始段地址
        public UInt32 StartSegmentAddress
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return m_StartSegmentAddress & 0x000FFFFF;
            }
        }

        public UInt16 CS
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return (UInt16)(m_StartSegmentAddress & 0x0000FFFF);
            }
        }

        public UInt16 IP
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return (UInt16)((m_StartSegmentAddress >> 16) & 0x000F);
            }
        }
    }
    /// <summary>
    /// 数据类型记录
    /// </summary>
    internal class DataRecord : Record
    {
        private Byte[] m_Data = null;
        private UInt16 m_LoadOffset = 0;

        //数据类型记录构造函数
        public DataRecord(UInt16 tLoadOffset, Byte[] tData)
        {
            if (null == tData)
            {
                return;
            }
            else if (tData.Length > 255)
            {
                return;
            }

            m_Data = tData;
            m_LoadOffset = tLoadOffset;

            m_bAvailable = true;
        }

        //获取记录长度
        public override int RecordLength
        {
            get
            {
                if (null == m_Data)
                {
                    return 0;
                }

                return m_Data.Length;
            }
        }

        //获取负载偏移
        public override UInt16 LoadOffset
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return m_LoadOffset;
            }
        }

        //获取记录类型
        public override Record.Type RecordType
        {
            get { return Type.DATA_RECORD; }
        }

        //获取记录数据
        public override Byte[] Data
        {
            get
            {
                if (!m_bAvailable)
                {
                    return null;
                }

                return m_Data;
            }
        }
    }
    /// <summary>
    /// 扩展段地址记录
    /// </summary>
    internal class ExtendSegmentAddressRecord : Record
    {
        private UInt16 m_ExtendSegmentUpperBaseAddress = 0;
        private Byte[] m_Data = null;
        //扩展地址段记录构造函数
        public ExtendSegmentAddressRecord(Byte[] tData)
        {
            if (null == tData)
            {
                return;
            }
            else if (2 != tData.Length)
            {
                return;
            }

            m_Data = tData;

            try
            {
                Byte[] tTemp = new Byte[2];
                tTemp[0] = tData[1];
                tTemp[1] = tData[0];

                m_ExtendSegmentUpperBaseAddress = BitConverter.ToUInt16(tTemp, 0);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return;
            }

            m_bAvailable = true;
        }

        //记录长度
        public override int RecordLength
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return 2;
            }
        }

        //记录类型
        public override Record.Type RecordType
        {
            get { return Type.EXTEND_SEGMENT_ADDRESS_RECORD; }
        }

        //获取数据
        public override Byte[] Data
        {
            get
            {
                if (!m_bAvailable)
                {
                    return null;
                }

                return m_Data;
            }
        }

        //获取扩展段基地址
        public UInt32 ExtendSegmentBaseAddress
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return ((UInt32)m_ExtendSegmentUpperBaseAddress) << 4;
            }
        }

    }
    /// <summary>
    /// 起始线性地址记录,用于指定执行开始目标文件的地址。
    /// </summary>
    internal class StartLinearAddressRecord : Record
    {
        private UInt32 m_StartLinearAddress = 0;
        private Byte[] m_Data = null;
        //开始线性地址记录构造函数
        public StartLinearAddressRecord(Byte[] tData)
        {
            if (null == tData)
            {
                return;
            }
            else if (4 != tData.Length)
            {
                return;
            }

            m_Data = tData;

            try
            {
                Byte[] tTemp = new Byte[4];
                tTemp[0] = tData[3];
                tTemp[1] = tData[2];
                tTemp[2] = tData[1];
                tTemp[3] = tData[0];

                m_StartLinearAddress = BitConverter.ToUInt32(tTemp, 0);
            }
            catch (Exception Err)
            {
                Err.ToString();
                return;
            }

            m_bAvailable = true;
        }

        //永远返回4
        public override Int32 RecordLength
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return 4;
            }
        }

        //获取数据类型
        public override Record.Type RecordType
        {
            get { return Type.START_LINEAR_ADDRESS_RECORD; }
        }

        //获取数据
        public override Byte[] Data
        {
            get
            {
                if (!m_bAvailable)
                {
                    return null;
                }

                return m_Data;
            }
        }

        //获取开始线性地址(EIP)
        public UInt32 StartLinearAddress
        {
            get
            {
                if (!m_bAvailable)
                {
                    return 0;
                }

                return m_StartLinearAddress;
            }
        }

    }
}
