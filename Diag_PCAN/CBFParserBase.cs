/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Aug 19, 2024
 * Author: He Jingchi
 */
using System;
using System.Collections.Generic;

namespace Diag_BUS
{
    public abstract class CBFParserBase
    {
        #region  Member variables

        public struct EraseInfo 
        {
            public uint start_address;
            public uint length;
        }

        public struct Header
        {
            public string hw_part_number;
            public UInt16 hw_part_number_DID;
            public string sw_part_number;
            public UInt16 sw_part_number_DID;
            public string sw_part_type;
            public string sw_version;
            public UInt16 sw_version_DID;
            public string system_supplier_identifier;
            public UInt16 system_supplier_identifier_DID;
            public UInt16 tester_request_CAN_ID;
            public UInt16 ECU_response_CAN_ID;
            public string hash_algorithm;
            public string RSA_algorithm;
            public string security_access_algorithm;
            public string file_integrity_check;
            public string verification_block_root_hash;
            public EraseInfo erase;

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

        public struct DataBlock
        {
            public uint StartAddr_Block;
            public uint Length_Block;
            public byte[] Data;  //0:flashdriver data; 1:flash data
            public byte[] CheckSum;
            public Header header;
        }


        public DataBlock m_DataBlock;

        public List<DataBlock> m_FlashDataLst;
        //CBF file full path name
        public string m_cbf_filename;

        #endregion

        /// <summary>
        /// FOR 2 flash file(flashdriver, app) mixture into 1 .cbf file
        /// </summary>
        /// <param name="strFileName">.cbf file to parse</param>
        /// <returns></returns>
        public abstract bool ReadCBFFile0(string strFileName);//

        /// <summary>
        /// FOR 2 flash file(flashdriver, app) split into 2 .cbf file, one of FLD and another is ASW file
        /// </summary>
        /// <param name="strFileName">.cbf file name</param>
        /// <param name="nSeq">.cbf file sequence, 0: fld file;  1: asw file</param>
        /// <returns></returns>
        public abstract bool ReadCBFFile1(string strFileName, int nSeq=0); //
    }
}