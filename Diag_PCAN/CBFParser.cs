using System;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace Diag_BUS
{
    internal class CBFParser
    {
        #region  Member variables

        //CBF file full path name
        string m_cbf_filename;
        //header length
        public uint m_uHeaderLen;
        //CRC32 of CBF file
        public uint m_uCBF_CRC;

        public struct Erase
        {
            public uint start_address;
            public uint length;
        }

        public struct Header
        {
            public string        hw_part_number;
            public UInt16      hw_part_number_DID;
            public string        sw_part_number;
            public UInt16      sw_part_number_DID;
            public string        sw_version;
            public UInt16      sw_version_DID;
            public string        system_supplier_identifier;
            public UInt16      system_supplier_identifier_DID;
            public UInt16      tester_request_CAN_ID;
            public UInt16      ECU_response_CAN_ID;
            public string        hash_algorithm; 
            public string        RSA_algorithm;
            public string        security_access_algorithm;
            public string        file_integrity_check;
            public string        verification_block_root_hash;
            public Erase        erase;

            //following field not use now...

            //symmetric_algorithm;                      //: null
            //ECU_parallel_flash_sequence_ID;     //: null

            //CRC_table;                                     //: null
            //cbf_version: v2.1.1
            //ECU_name: PTC_Eth
            //ECU_type: 0
            //AreaAB_enable: 0
            //Area_Info: null
            //sw_part_type: ASW1
            //flash_Package_type: MCU

            //data_format_identifier: 0x00
            //flash_sequence: EEA5.0
            //flash_type: UDS
            //Diagnostic_communication_type: Eth
            //Transmit_CAN_DL: null

            //External_tester_DoIP_LA: 0x0E80
            //Internal_tester_DoIP_LA: 0x0F00
            //ECU_DoIP_LA: 0x0107
            //ECU_IP: 192.168.69.8
            //ECU_MAC: 02-51-52-00-00-08
            //ECU_DoIP_Functional_LA: 0xE400
            //file_path:null
        }

        public Header m_header;

        public struct DataBlock
        {
          public uint StartAddr_Block;
          public uint Length_Block;
          public  byte[] Data;  //0:flashdriver data; 1:flash data
          public  byte[] CheckSum;
        }

        public DataBlock m_DataBlock;

        public List<DataBlock> m_FlashDataLst;
        #endregion

        public CBFParser(string strFileName)
        {
            m_cbf_filename = strFileName;
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

        public CBFParser(string cbf_filename, uint uHeaderLen, uint uCBF_CRC, Header header, DataBlock dataBlock, List<DataBlock> flashDataLst) : this(cbf_filename)
        {
            m_uHeaderLen = uHeaderLen;
            m_uCBF_CRC = uCBF_CRC;
            m_header = header;
            m_DataBlock = dataBlock;
            m_FlashDataLst = flashDataLst;
        }

        /// <summary>
        /// read CBF file content
        /// </summary>
        /// <returns></returns>
        public bool ReadCBFFile()
        {
            bool bResult = false;
            bool bBinFile = false;
            int k = 0;
            int nEraseStartAddr = 0;
            int nEraseInfoLen = 0;
            int nBlockIdx = 0;
            uint nToltalLen;
            byte bCurrByte;
            BinaryReader BR=null;

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
                    if (i > (0xf +0x2)  && i <= (0xf + 0xa))
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

                                                if(strCBFChildContent[0].Contains("hw_part_number"))
                                                {
                                                    if (!strCBFChildContent[0].Contains("DID"))
                                                        m_header.hw_part_number = strCBFChildContent[1];
                                                    else
                                                    {
                                                        strTempField = strCBFChildContent[1].Substring(3, sizeof(UInt32));
                                                        m_header.hw_part_number_DID = Convert.ToUInt16(strTempField ,  16);
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
                                                else if(strCBFChildContent[0].Contains("length"))
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
                                                            m_DataBlock.Data = new byte[m_DataBlock.Length_Block + 2];
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
                                                            m_DataBlock.Data = new byte[m_DataBlock.Length_Block /*+ 2*/];
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
                                                //if (i > (2 * 0xF + 0xB + m_uHeaderLen) && i < (2 * 0xF + 0xD + m_uHeaderLen + m_header.erase.length))
                                                //{
                                                //    if (m_header.erase.length > 0)
                                                //        m_DataBlock.Data[k++] = bCurrByte;
                                                //}

                                                //Skip the header and special characters (", {},[])
                                                if (i > (2 * 0xF + 0xB + m_uHeaderLen) && 
                                                    i < (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block))
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
                                                    i < (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block))
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
                                            if (i >   (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block) && 
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
                                            if (i >   (2 * 0xF + 0xD + m_uHeaderLen + m_FlashDataLst[0].Length_Block + 0xA + m_FlashDataLst[1].Length_Block) &&
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
                if(BR!=null)
                    BR.Close(); 
            }

            return bResult;
        }
    }
}
