/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Nov 1, 2023
 * Author: He Jingchi
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
//using Bin_CRC;

namespace Diag_BUS
{
    public unsafe struct TRange
    {
        public char* pMem;
        public long lLen;
    };

    public struct AesCmacCtx
    {
        public byte[] X;
        public byte[] M_last;
        public int M_n;
    }

    public unsafe class fConvert
    {
        static char[] ASIItab = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        
        #region DllImport

        [DllImport("msvcrt.dll", EntryPoint = "memcmp")]
        private static extern int memcmp(byte[] b1, byte[] b2, uint count);
        [DllImport("msvcrt.dll", EntryPoint = "memcpy")]
        private static extern int memcpy(byte[] b1, byte[] b2, uint count);

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CRC16Check")]
        private static extern ushort CRC16Check(IntPtr pCheckSum, byte* LPduPrt, byte LEN);

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CHECKSUM")]
        private static extern ushort CHECKSUM(IntPtr pCheckSum, byte* LPduPrt, byte LEN, bool bBigEnding);

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "N2S_CHECKSUM")]
        private static extern uint N2S_CHECKSUM(IntPtr pCheckSum, byte* LPduPrt, uint length, uint crcOrigin);

        //void AES128CMAC(const uint8_t* buffer, uint16_t size, const uint8_t* key, uint8_t* token);
        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "AES128CMAC")]
        private static extern uint AES128CMAC(IntPtr buffer, ushort size, IntPtr key, IntPtr token);

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "CreateObj")]
        private static extern IntPtr CreateObj();

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "DeleteObj")]
        private static extern void DeleteObj(IntPtr pCheckSum);

        [DllImport("GenerateKeyExImpl.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GenerateKeyEx")]
        private static extern IntPtr GenerateKeyEx(IntPtr ipSeedArray, uint iSeedArraySize,uint iSecurityLevel, IntPtr ipVariant, IntPtr iopKeyArray, uint iMaxKeyArraySize,uint* oActualKeyArraySize);

        [DllImport("GenerateKeyExImpl.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "GenerateKey")]
        private static extern IntPtr GenerateKey(IntPtr ipSeedArray, UInt32 iSeedArraySize, uint iSecurityLevel, IntPtr ipVariant, IntPtr iopKeyArray, uint iMaxKeyArraySize, uint* oActualKeyArraySize);

        [DllImport("ac7801CheckSum.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "convert")]
        private static extern uint convert(string[] args);

        /// <summary>
        /// 修改INI文件内容
        /// </summary>
        /// <param name="lpApplicationName">节点名称(段落名称)section</param>
        /// <param name="lpKeyName">要设置的项名，Key</param>
        /// <param name="lpString">要写入的新字符串Value</param>
        /// <param name="lpFileName">INI文件晚挣路径</param>
        /// <returns>0表示失败，非零表示成功</returns>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string lpApplicationName,string lpKeyName,string lpString,string lpFileName);

        // <summary>
        /// 获取INI中指定字符串
        /// </summary>
        /// <param name="lpAppName">节点名称(段落名称)section</param>
        /// <param name="lpKeyName">项名，Key</param>
        /// <param name="lpDefault">未找到指定项时返回的默认值</param>
        /// <param name="lpReturnedString">指定一个字符串缓冲区，长度至少为nSize</param>
        /// <param name="nSize">指定装载到lpReturnedString缓冲区的最大字符数量</param>
        /// <param name="lpFileName">INI文件路径</param>
        /// <returns>复制到lpReturnedString中的字节数量</returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName,string lpKeyName,string lpDefault,StringBuilder lpReturnedString,int nSize,string lpFileName);
        #endregion
        

        public static bool CalcChecksum(TRange[] ptr, int nRanges, byte[] pnChecksum, uint* pnSignificant, short nFlags)
        {
            bool bRet = true;
            long crc = 0;
            //*pnSignificant = 4;
            for (int i = 0; i < nRanges; i++)
            {
                for (int j = 0; j < ptr[i].lLen; j++) {
                    crc += ptr[i].pMem[j];
                }
            }
            byte[] bCrc = Encoding.UTF8.GetBytes(crc.ToString());
            if ((nFlags & 0x0001) > 0)
            {
                bRet = memcmp(bCrc, pnChecksum, *pnSignificant) == 0;
            }

            memcpy(pnChecksum, bCrc, (uint)(bCrc.Length));
            return bRet;
        }
        // CRC高位字节值表
        static byte[] auch1CRCHi =
        {   0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
            0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
            0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
            0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40, 0x00, 0xC1, 0x81, 0x40,
            0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0, 0x80, 0x41, 0x00, 0xC1,
            0x81, 0x40, 0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41,
            0x00, 0xC1, 0x81, 0x40, 0x01, 0xC0, 0x80, 0x41, 0x01, 0xC0,
            0x80, 0x41, 0x00, 0xC1, 0x81, 0x40 };
        // CRC低位字节值表
        static byte[] auch1CRCLo =
        {    0x00, 0xC0, 0xC1, 0x01, 0xC3, 0x03, 0x02, 0xC2, 0xC6, 0x06,
             0x07, 0xC7, 0x05, 0xC5, 0xC4, 0x04, 0xCC, 0x0C, 0x0D, 0xCD,
             0x0F, 0xCF, 0xCE, 0x0E, 0x0A, 0xCA, 0xCB, 0x0B, 0xC9, 0x09,
             0x08, 0xC8, 0xD8, 0x18, 0x19, 0xD9, 0x1B, 0xDB, 0xDA, 0x1A,
             0x1E, 0xDE, 0xDF, 0x1F, 0xDD, 0x1D, 0x1C, 0xDC, 0x14, 0xD4,
             0xD5, 0x15, 0xD7, 0x17, 0x16, 0xD6, 0xD2, 0x12, 0x13, 0xD3,
             0x11, 0xD1, 0xD0, 0x10, 0xF0, 0x30, 0x31, 0xF1, 0x33, 0xF3,
             0xF2, 0x32, 0x36, 0xF6, 0xF7, 0x37, 0xF5, 0x35, 0x34, 0xF4,
             0x3C, 0xFC, 0xFD, 0x3D, 0xFF, 0x3F, 0x3E, 0xFE, 0xFA, 0x3A,
             0x3B, 0xFB, 0x39, 0xF9, 0xF8, 0x38, 0x28, 0xE8, 0xE9, 0x29,
             0xEB, 0x2B, 0x2A, 0xEA, 0xEE, 0x2E, 0x2F, 0xEF, 0x2D, 0xED,
             0xEC, 0x2C, 0xE4, 0x24, 0x25, 0xE5, 0x27, 0xE7, 0xE6, 0x26,
             0x22, 0xE2, 0xE3, 0x23, 0xE1, 0x21, 0x20, 0xE0, 0xA0, 0x60,
             0x61, 0xA1, 0x63, 0xA3, 0xA2, 0x62, 0x66, 0xA6, 0xA7, 0x67,
             0xA5, 0x65, 0x64, 0xA4, 0x6C, 0xAC, 0xAD, 0x6D, 0xAF, 0x6F,
             0x6E, 0xAE, 0xAA, 0x6A, 0x6B, 0xAB, 0x69, 0xA9, 0xA8, 0x68,
             0x78, 0xB8, 0xB9, 0x79, 0xBB, 0x7B, 0x7A, 0xBA, 0xBE, 0x7E,
             0x7F, 0xBF, 0x7D, 0xBD, 0xBC, 0x7C, 0xB4, 0x74, 0x75, 0xB5,
             0x77, 0xB7, 0xB6, 0x76, 0x72, 0xB2, 0xB3, 0x73, 0xB1, 0x71,
             0x70, 0xB0, 0x50, 0x90, 0x91, 0x51, 0x93, 0x53, 0x52, 0x92,
             0x96, 0x56, 0x57, 0x97, 0x55, 0x95, 0x94, 0x54, 0x9C, 0x5C,
             0x5D, 0x9D, 0x5F, 0x9F, 0x9E, 0x5E, 0x5A, 0x9A, 0x9B, 0x5B,
             0x99, 0x59, 0x58, 0x98, 0x88, 0x48, 0x49, 0x89, 0x4B, 0x8B,
             0x8A, 0x4A, 0x4E, 0x8E, 0x8F, 0x4F, 0x8D, 0x4D, 0x4C, 0x8C,
             0x44, 0x84, 0x85, 0x45, 0x87, 0x47, 0x46, 0x86, 0x82, 0x42,
             0x43, 0x83, 0x41, 0x81, 0x80, 0x40 };

        /// <summary>
        /// CRC16 checksum alogrithm
        /// </summary>
        /// <param name="puchMsg">要进行CRC校验的消息</param>
        /// <returns></returns>
        private static short CRC_16(byte[] puchMsg)
        {
            #region C dll calculate checksum value of 0x36 service send data block

            //ushort CRC = 0x0000;
            //byte copylen = (byte)puchMsg.Length;
            //string sendmsg = puchMsg.ToString();
            //try
            //{
            //    // Allocate HGlobal memory for source
            //    IntPtr sptr = Marshal.StringToHGlobalAuto(sendmsg); 
            //    IntPtr pCheckSum = CreateObj();

            //    byte* src = (byte*)sptr.ToPointer();
            //    CRC = CRC16Check(pCheckSum, src, copylen);

            //    // Free HGlobal memory and checksum dll's object
            //    DeleteObj(pCheckSum);
            //    Marshal.FreeHGlobal(sptr);
            //}
            //catch (Exception ex)
            //{
            //    Console.Write(ex.Message);
            //}

            //return (short)CRC;
            #endregion

            #region normal way to calculate checksum value of 0x36 service send data block

            byte uchCRCHi = 0xFF; //高CRC字节初始化
            byte uchCRCLo = 0xFF; //低CRC 字节初始化
            byte uIndex = 0;    // CRC循环中的索引
            byte i = 0;
            for (i = 2; i < puchMsg.Length; i++) // 传输消息缓冲区(去除服务ID，数据块次序。2字节)
            {
                uIndex = Convert.ToByte(uchCRCHi ^ puchMsg[i]); // 计算CRC
                uchCRCHi = Convert.ToByte(uchCRCLo ^ auch1CRCHi[uIndex]);
                uchCRCLo = auch1CRCLo[uIndex];
            }

            return (short)(uchCRCHi << 8 | uchCRCLo);
            #endregion
        }

        /// <summary>
        /// For bin checksum verify
        /// </summary>
        /// <param name="strFileName">bin file name</param>
        /// <returns></returns>
        public static uint BINCRC(string strFileName)
        {
            uint CRC = 0xffffffff;
            string[] args = new string[1];
            args[0] = strFileName;

            #region C# dll invoke(need reference Bin_CRC.dll )
            //CRC crcObj = new CRC();
            //return crcObj.convert(args); 
            #endregion

            #region c dynamic library way

            CRC = convert(args);

            #endregion

            return CRC;
        }


        /// <summary>
        /// N2S project checksum algorithm(c dll progame)
        /// </summary>
        /// <param name="puchMsg"></param>
        /// <returns></returns>
        public static uint N2S_CheckSum(byte[] puchMsg, UInt32 MASK)
        {
            uint crcOrigin = 0xffffffff;
            uint CRC = 0xffffffff;
            uint copylen = (uint)puchMsg.Length;           
            string sendmsg = puchMsg.ToString();
            try
            {
                #region c dynamic library way(not OK)
                //// Allocate HGlobal memory for source
                //IntPtr sptr = Marshal.StringToHGlobalAuto(sendmsg);         //StringToHGlobalUni(sendmsg);
                //IntPtr pCheckSum = CreateObj();

                //byte* src = (byte*)sptr.ToPointer();
                //CRC = N2S_CHECKSUM(pCheckSum, src, copylen, crcOrigin);

                //// Free HGlobal memory and checksum dll's object
                //DeleteObj(pCheckSum);
                //Marshal.FreeHGlobal(sptr);
                #endregion

                #region tanslate to C# code from check CRC-32 alogrithym

                uint i = 0, j = 0, tmp = 0;
                while (i < copylen)
                {
                    CRC = CRC ^ puchMsg[i];
                    for (j = 8; j > 0; --j)
                    {
                        if ((CRC & 1) > 0)
                        {
                            tmp = crcOrigin;
                        }
                        else
                        {
                            tmp = 0;
                        }
                        CRC = (CRC >> 1) ^ (MASK & tmp);
                    }
                    i++;
                }

                CRC ^= crcOrigin;

                #endregion


            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
            }

            return CRC;
        }

        public static byte[] CheckSum(byte[] src)
        {
            short usCRC16;
            short dev = 0;
            int srclen = src.Length;
            byte[] dst = new byte[srclen + 2];

            /* Calculate CRC16 checksum */
            usCRC16 = CRC_16(src);
            for (int i = 0; i < srclen; i++)
                dst[dev++] = src[i];
            dst[dev++] = (byte)(usCRC16 >> 8 & 0xFF);
            dst[dev] = (byte)(usCRC16 & 0xFF);

            return dst;
        }

        /// <summary>
        /// Security access alogrithm
        /// </summary>
        /// <param name="SeedArray">seed that response from ecu</param>
        /// <param name="KeyArray">Through alogrithm caculated key</param>
        /// <param name="MASK">Mask code</param>
        /// <returns></returns>
        public static UInt32 seedToKey(byte[] SeedArray, out byte[] KeyArray, UInt32 MASK)
        {
            UInt32 dwordkey = 0;
            UInt32 seed;
            int i;
            seed = (UInt32)((SeedArray[0] << 24) + (SeedArray[1] << 16) + (SeedArray[2] << 8) + SeedArray[3]);
            if (seed != 0)
            {
                for (i = 0; i < 35; i++)
                {
                    if ((seed & 0x80000000) > 0)
                    {
                        seed = seed << 1;
                        seed = seed ^ MASK;
                    }
                    else
                    {
                        seed = seed << 1;
                    }
                }
                dwordkey = seed;
            }
            KeyArray = new byte[] { 0x0, 0x0, 0x0, 0x0 };

            KeyArray[0] = (byte)seed;
            KeyArray[1] = (byte)(seed >> 8);
            KeyArray[2] = (byte)(seed >> 16);
            KeyArray[3] = (byte)(seed >> 24);

            return dwordkey;
        }

        /// <summary>
        /// Security access alogrithm2
        /// </summary>
        /// <param name="SeedArray">seed that response from ecu</param>
        /// <param name="KeyArray">Through alogrithm caculated key</param>
        /// <param name="MASK">Mask code</param>
        /// <returns></returns>
        public static UInt32 seedToKey2(byte[] SeedArray, out byte[] KeyArray, UInt32 MASK)
        {
            UInt32 dwordkey = 0;
            UInt32 seed = 0;

            seed = (UInt32)((SeedArray[0] << 24) + (SeedArray[1] << 16) + (SeedArray[2] << 8) + SeedArray[3]);
            if (seed != 0)
            {
                dwordkey = (seed >> 7) | (seed << 16);
                dwordkey *= 2;
                dwordkey ^= MASK;
                dwordkey = (dwordkey << 5) | (dwordkey >> 12);
            }
            KeyArray = new byte[] { 0x0, 0x0, 0x0, 0x0 };
            KeyArray[0] = (byte)((dwordkey & 0xFF000000) >> 24);
            KeyArray[1] = (byte)((dwordkey & 0xFF0000) >> 16);
            KeyArray[2] = (byte)((dwordkey & 0xFF00) >> 8);
            KeyArray[3] = (byte)(dwordkey & 0xFF);

            return dwordkey;
        }

        /// <summary>
        /// N2S project security access 
        /// </summary>
        /// <param name="SeedArray">seed array</param>
        /// <param name="KeyArray">output caculate key</param>
        /// <param name="nSecurityLevel">Security Level</param>
        /// <returns></returns>
        public static UInt32 N2S_seedToKey(byte[] SeedArray, out byte[] KeyArray, int nSecurityLevel)
        {
            byte[] Cal = new byte[4];
            KeyArray = new byte[4] { 0x0, 0x0, 0x0, 0x0 };

            Cal[0] = (byte)(SeedArray[0] ^ 0xE4);
            Cal[1] = (byte)(SeedArray[1] ^ 0x2F);
            Cal[2] = (byte)(SeedArray[2] ^ 0x45);
            Cal[3] = (byte)(SeedArray[3] ^ 0x92);

            if (nSecurityLevel == 1)
            {
                KeyArray[0] = (byte)(((Cal[2] & 0xF0) << 4) | (Cal[3] & 0xF0));
                KeyArray[1] = (byte)(((Cal[3] & 0x2F) << 2) | (Cal[1] & 0x03));
                KeyArray[2] = (byte)(((Cal[1] & 0xFC) >> 2) | (Cal[0] & 0xC0));
                KeyArray[3] = (byte)(((Cal[0] & 0x0F) << 4) | (Cal[2] & 0x0F));
                return 0;
            }
            else if (nSecurityLevel == 9)
            {
                KeyArray[0] = (byte)(((Cal[2] & 0x0F) << 4) | (Cal[1] & 0x0F));
                KeyArray[1] = (byte)(((Cal[0] & 0x0F) << 4) | (Cal[3] & 0x0F));
                KeyArray[2] = (byte)(((Cal[0] & 0xF0) >> 2) | ((Cal[2] & 0xF0) >> 4));
                KeyArray[3] = (byte)(((Cal[3] & 0xF0) >> 4) | ((Cal[1] & 0xF0) >> 4));
                return 0;
            }
            return 1;
        }

        /// <summary>
        /// Security access alogrithm3
        /// </summary>
        /// <param name="SeedArray">seed that response from ecu</param>
        /// <param name="KeyArray">Through alogrithm caculated key</param>
        /// <param name="MASK">Mask code</param>
        /// <returns></returns>
        public static UInt32 seedToKey3(byte[] SeedArray, out byte[] KeyArray, UInt32 MASK)
        {
            UInt32 dwordkey = 4;
            IntPtr pzKeyArray = IntPtr.Zero;
            IntPtr pKeyArray = IntPtr.Zero;
            KeyArray = new byte[dwordkey];
       
            try
            {
                IntPtr pSeedArray = Marshal.UnsafeAddrOfPinnedArrayElement(SeedArray, 0);
                byte[] iVariant = BitConverter.GetBytes(MASK);
                IntPtr pVariant = Marshal.UnsafeAddrOfPinnedArrayElement(iVariant, 0);

                pKeyArray = Marshal.AllocHGlobal(4);
                pzKeyArray = GenerateKey(pSeedArray, dwordkey, 0, pVariant, pKeyArray, dwordkey, &dwordkey);
                Marshal.Copy(pzKeyArray, KeyArray, 0, 4);
            }
            catch(Exception ex)
            {
                Console.Write(ex.Message);
            }
            finally
            {
                Marshal.FreeHGlobal(pKeyArray);
            }

            return dwordkey;
        }

        #region from AES128 c code convert

        static readonly uint[] WKey = new uint[44];  //44*4=176字节
        static readonly uint[] RndKey = new uint[4];
        static readonly byte[,] ByteRndKey = new byte[4, 4];
        static readonly byte[] RC = new byte[] { 0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1B, 0x36, };

        /// <summary>
        /// Security access alogrithm3
        /// </summary>
        /// <param name="seed"></param>
        /// <param name="key"></param>
        /// <param name="rnd_key_idx"></param>
        public static void seedToKey4(byte[] seed, out byte[] key, byte rnd_key_idx)
        {
            byte[] k = new byte[16];

            KeyExp(seed);
            GetByteRndKey(rnd_key_idx);

            byte t = 0;
            for (byte j =  0; j<4; j++)
            {
                for(byte l = 0; l<4; l++)
                {
                    k[t++] = ByteRndKey[j, l];
                }
            }
            key = new byte[16];
            Array.Copy(k, key, sizeof(byte)*16);
        }

        static byte Sout(byte x)
        {
            byte rslt = 0;
            //伽罗瓦域乘2就是将其左移1位，若原先值的最高位为1则还要异或0x1B
            rslt = (byte)((x << 1) ^ (((x & 0x80) == 1) ? 0x1B : 0x00));
            return rslt;
        }

        static uint WGen(uint input, byte round)
        {
            byte[] in0 = new byte[4];
            byte[] temp = new byte[4];
            uint int_temp;
            byte i = 0x0;
 
            in0[0] = Convert.ToByte(input & 0x000000FF);          //B3 LSB
            in0[1] = Convert.ToByte((input & 0x0000FF00) >> 8);   //B2
            in0[2] = Convert.ToByte((input & 0x00FF0000) >> 16);  //B1
            in0[3] = Convert.ToByte((input & 0xFF000000) >> 24);  //B0 MSB
            for (i = 0; i < 4; i++)
            {
                temp[i] = in0[(i + 3) % 4]; //左移是将MSB往左移
            }

            for (i = 0; i < 4; i++)
            {
                temp[i] = Sout(temp[i]);
            }

            temp[3] ^= RC[round];
            int_temp = temp[0] | ((uint)temp[1]) << 8 | ((uint)temp[2]) << 16 | ((uint)temp[3]) << 24;

            return int_temp;
        }

        static void KeyExp(byte[] key)
        {
            byte i;
            //以B0/B1/B2/B3为例，MSB取B0，LSB取B3，以此类推
            WKey[0] = ((uint)(key[0]) << 24 | (uint)(key[1]) << 16 | (uint)(key[2]) << 8 | key[3]);
            WKey[1] = ((uint)(key[4]) << 24 | (uint)(key[5]) << 16 | (uint)(key[6]) << 8 | key[7]);
            WKey[2] = ((uint)(key[8]) << 24 | (uint)(key[9]) << 16 | (uint)(key[10]) << 8 | key[11]);
            WKey[3] = ((uint)(key[12]) << 24 | (uint)(key[13]) << 16 | (uint)(key[14]) << 8 | key[15]);
 
            for (i = 4; i <= 43; i++)
            {
                if ((i % 4) == 0u)
                {
                    WKey[i] = WKey[i - 4] ^ WGen(WKey[i - 1], (byte)(i/4));  //i/4就是round，[1, 10]
                }
                else
                {
                    WKey[i] = WKey[i - 4] ^ WKey[i - 1];
                }
            }
 
        }
        //先运行fpKeyExp，秘钥扩展后存入WKey[44]，然后每次秘钥轮加的时候运行GetRoundKey从WKey[44]里抽取需要的4个组成一个数组与State进行异或       
        static void GetRoundKey(byte rnd_key_idx)
        {
            RndKey[0] = WKey[rnd_key_idx * 4];
            RndKey[1] = WKey[rnd_key_idx * 4 + 1];
            RndKey[2] = WKey[rnd_key_idx * 4 + 2];
            RndKey[3] = WKey[rnd_key_idx * 4 + 3];
        }

        //以RndKey[4]为基础生成ByteRndKey[4][4]，和State里的每个字节异或，先调用GetByteRndKey，然后就可以使用ByteRndKey[4][4]      
        static void GetByteRndKey(byte rnd_key_idx)
        {
            byte i;

            GetRoundKey(rnd_key_idx);
            //从int[4]转char[4][4]时要注意空间位置
            for (i = 0; i < 4; i++)
            {
                ByteRndKey[i,0] = Convert.ToByte((RndKey[0] >> ((3 - i) * 8)) & (uint)0xFF);
            }
            for (i = 0; i < 4; i++)
            {
                ByteRndKey[i,1] = Convert.ToByte((RndKey[1] >> ((3 - i) * 8)) & (uint)0xFF);
            }
            for (i = 0; i < 4; i++)
            {
                ByteRndKey[i,2] = Convert.ToByte((RndKey[2] >> ((3 - i) * 8)) & (uint)0xFF);
            }
            for (i = 0; i < 4; i++)
            {
                ByteRndKey[i,3] = Convert.ToByte((RndKey[3] >> ((3 - i) * 8)) & (uint)0xFF);
            } 
        }


        #endregion

        #region C# cmac verification is written according to C-LORAWAN protocol

        public static byte[] AES_128_CMAC(byte[] seed, byte level)
        {
            ushort SeedSize = 16;
            IntPtr pKeyArray = IntPtr.Zero;
            byte[] secretKey_Leve1 = new byte[]{0xF3, 0x2D, 0x76, 0xD9, 0xEE, 0x59, 0x3B, 0x10, 0x3F, 0x5E, 0x13, 0x9C, 0x71, 0x70, 0x3A, 0x17};
            byte[] secretKey_Leve11 = new byte[]{0xF8, 0x02, 0x69, 0x9F, 0xDA, 0x9E, 0x6D, 0xA3, 0xC9, 0x9B, 0x05, 0xB9, 0xEA, 0x7B, 0xD2, 0xF8};
            byte[] test_key = new byte[] { 0xD0, 0xDF, 0xAA, 0x15, 0x88, 0xE0, 0x4B, 0x5B, 0x14, 0xCE, 0x83, 0x4E, 0x65, 0xE6, 0x21, 0xCD };
            byte[] key = new byte[SeedSize];

            #region c library way for caculate aes128 key

            //try
            //{
            //    IntPtr pSeedArray = Marshal.UnsafeAddrOfPinnedArrayElement(seed, 0);
            //    IntPtr pKey_Level1 = Marshal.UnsafeAddrOfPinnedArrayElement(secretKey_Leve1, 0);
            //    IntPtr pKey_Level11 = Marshal.UnsafeAddrOfPinnedArrayElement(secretKey_Leve11, 0);
            //    IntPtr pKey_Test = Marshal.UnsafeAddrOfPinnedArrayElement(test_key, 0);//for test

            //    pKeyArray = Marshal.AllocHGlobal(4);
            //    if (level == 0x01)
            //        AES128CMAC(pSeedArray, SeedSize, pKey_Level1, pKeyArray);
            //    else if (level == 0x11)
            //        AES128CMAC(pSeedArray, SeedSize, pKey_Test, pKeyArray); //for test
            //        //AES128CMAC(pSeedArray, SeedSize, pKey_Level11, pKeyArray);
            //    Marshal.Copy(pKeyArray, key, 0, SeedSize);
            //}
            //catch (Exception ex)
            //{
            //    Console.Write(ex.Message);
            //}
            //finally
            //{
            //    Marshal.FreeHGlobal(pKeyArray);
            //}


            #endregion

            #region C# way for caculate aes128 key

            if (level == 0x01)
                key = Aes_Cmac(secretKey_Leve1, seed);
            else if (level == 0x11)
                key = Aes_Cmac(secretKey_Leve11/*test_key*/, seed);

            #endregion

            return key;
        }

        /// <summary>
        /// Get cmac
        /// </summary>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte[] Aes_Cmac(byte[] key, byte[] data)
        {
            // SubKey generation
            // step 1, AES-128 with key K is applied to an all-zero input block.
            byte[] L = AesEncrypt(key, new byte[16], new byte[16]);

            // step 2, K1 is derived through the following operation:
            byte[] FirstSubkey = Rol(L); //If the most significant bit of L is equal to 0, K1 is the left-shift of L by 1 bit.
            if ((L[0] & 0x80) == 0x80)
                FirstSubkey[15] ^= 0x87; // Otherwise, K1 is the exclusive-OR of const_Rb and the left-shift of L by 1 bit.

            // step 3, K2 is derived through the following operation:
            byte[] SecondSubkey = Rol(FirstSubkey); // If the most significant bit of K1 is equal to 0, K2 is the left-shift of K1 by 1 bit.
            if ((FirstSubkey[0] & 0x80) == 0x80)
                SecondSubkey[15] ^= 0x87; // Otherwise, K2 is the exclusive-OR of const_Rb and the left-shift of K1 by 1 bit.

            // MAC computing
            if (((data.Length != 0) && (data.Length % 16 == 0)) == true)
            {
                // If the size of the input message block is equal to a positive multiple of the block size (namely, 128 bits),
                // the last block shall be exclusive-OR'ed with K1 before processing
                for (int j = 0; j < FirstSubkey.Length; j++)
                    data[data.Length - 16 + j] ^= FirstSubkey[j];
            }
            else
            {
                // Otherwise, the last block shall be padded with 10^i
                byte[] padding = new byte[16 - data.Length % 16];
                padding[0] = 0x80;

                data = data.Concat<byte>(padding.AsEnumerable()).ToArray();

                // and exclusive-OR'ed with K2
                for (int j = 0; j < SecondSubkey.Length; j++)
                    data[data.Length - 16 + j] ^= SecondSubkey[j];
            }

            // The result of the previous process will be the input of the last encryption.
            byte[] encResult = AesEncrypt(key, new byte[16], data);
            byte[] HashValue = new byte[16];
            Array.Copy(encResult, encResult.Length - HashValue.Length, HashValue, 0, HashValue.Length);

            return HashValue;
        }

        private static byte[] Rol(byte[] b)
        {
            byte[] r = new byte[b.Length];
            byte carry = 0;

            for (int i = b.Length - 1; i >= 0; i--)
            {
                ushort u = (ushort)(b[i] << 1);
                r[i] = (byte)((u & 0xff) + carry);
                carry = (byte)((u & 0xff00) >> 8);
            }
            return r;
        }

        /// <summary>
        /// AES Encrypt
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        private static byte[] AesEncrypt(byte[] keys, byte[] iv, byte[] data)
        {
            using (RijndaelManaged cipher = new RijndaelManaged())
            {
                cipher.Mode = CipherMode.CBC;
                cipher.Padding = PaddingMode.None;
                cipher.Key = keys;
                cipher.IV = iv;

                using (ICryptoTransform encryptor = cipher.CreateEncryptor())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream writer = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            writer.Write(data, 0, data.Length);
                            writer.FlushFinalBlock();
                            return ms.ToArray();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// C# cmac verification is written according to C-LORAWAN protocol
        /// </summary>
        /// <param name="mixBxBuffer">The data payload checks B0, otherwise null</param>
        /// <param name="buffer">Content to be verified</param>
        /// <param name="key">The secret key to be encrypted</param>
        public static byte[] ComputeCmac(byte[] mixBxBuffer, byte[] buffer, byte[] key)
        {
            AesCmacCtx ctx = new AesCmacCtx();
            AES_CMAC_Init(ref ctx);
            //*-*micBxBuffer
            int bufferSize;
            if (mixBxBuffer != null && mixBxBuffer.Length > 0)
            {
                AES_CMAC_Update(ref ctx, mixBxBuffer, key, 16);
            }
            bufferSize = buffer.Length;
            AES_CMAC_Update(ref ctx, buffer, key, bufferSize);

            AES_CMAC_Final(ref ctx, key, out byte[] Cmac);

            return Cmac;
            //string cmac;
            //foreach (var item in Cmac)
            //{
            //    Console.WriteLine(item.ToString("X2"));
            //}
            //cmac = (Cmac[0] << 24 | Cmac[1] << 16 | Cmac[2] << 8 | Cmac[3]).ToString("X8");

        }

        private static void AES_CMAC_Init(ref AesCmacCtx ctx)
        {
            ctx.X = new byte[16];
            ctx.M_n = 0;
            ctx.M_last = new byte[16];
        }

        private static void LSHIFT(byte[] v, ref byte[] r)
        {
            for (int i = 0; i < 15; i++)
            {
                r[i] = (byte)((v[i] << 1) | (v[i + 1] >> 7));
            }
            r[15] = (byte)(v[15] << 1);
        }

        private static void XOR(byte[] v, ref byte[] r)
        {
            for (int i = 0; i < 16; i++)
            {
                r[i] = (byte)(r[i] ^ v[i]);
            }
        }

        private static void AES_CMAC_Update(ref AesCmacCtx ctx, byte[] data, byte[] appKey, int len)
        {
            int mlen = 0;
            byte[] tempIn = new byte[16];
            byte[] tempData;

            if (ctx.M_n > 0)
            {
                mlen = Math.Min(16 - ctx.M_n, len);
                for (int i = 0; i < mlen; i++)
                {
                    ctx.M_last[ctx.M_n + i] = data[i];
                }
                ctx.M_n += mlen;
                if (ctx.M_n < 16 || len == mlen)
                    return;
                XOR(ctx.M_last, ref ctx.X);

                Array.Copy(ctx.X, tempIn, 16);
                tempIn = AesEncrypt(appKey, new byte[16], tempIn);
                Array.Copy(tempIn, ctx.X, 16);

                tempData = new byte[data.Length - mlen];
                Array.Copy(data, mlen, tempData, 0, data.Length - mlen);
                data = new byte[data.Length - mlen];
                Array.Copy(tempData, data, tempData.Length);

                len -= mlen;
            }
            while (len > 16)
            {
                XOR(data, ref ctx.X);

                Array.Copy(ctx.X, tempIn, 16);
                tempIn = AesEncrypt(appKey, new byte[16], tempIn);
                Array.Copy(tempIn, ctx.X, 16);

                tempData = new byte[data.Length - 16];
                Array.Copy(data, 16, tempData, 0, data.Length - 16);
                data = new byte[data.Length - 16];
                Array.Copy(tempData, data, tempData.Length);

                len -= 16;
            }
            Array.Copy(data, ctx.M_last, len);
            ctx.M_n = len;
        }

        private static void AES_CMAC_Final(ref AesCmacCtx ctx, byte[] appKey, out byte[] Cmac)
        {
            Cmac = new byte[16];
            byte[] tempK = new byte[16];
            byte[] tempIn = new byte[16];

            tempK = AesEncrypt(appKey, new byte[16], tempK);

            if ((int)(tempK[0] & 0x80) > 0)
            {
                LSHIFT(tempK, ref tempK);
                tempK[15] ^= 0x87;
            }
            else
                LSHIFT(tempK, ref tempK);

            if (ctx.M_n == 16)
            {
                XOR(tempK, ref ctx.M_last);
            }
            else
            {
                if ((int)(tempK[0] & 0x80) > 0)
                {
                    LSHIFT(tempK, ref tempK);
                    tempK[15] ^= 0x87;
                }
                else
                    LSHIFT(tempK, ref tempK);

                ctx.M_last[ctx.M_n] = 0x80;
                while (++ctx.M_n < 16)
                {
                    ctx.M_last[ctx.M_n] = 0x00;
                }

                XOR(tempK, ref ctx.M_last);
            }
            XOR(ctx.M_last, ref ctx.X);

            Array.Copy(ctx.X, tempIn, 16);
            tempIn = AesEncrypt(appKey, new byte[16], tempIn);
            tempK = new byte[16];
            Array.Copy(tempIn, Cmac, 16);
        }


        #endregion

        #region read / write INI file

        /// <summary>
        /// 根据section，key取值,并设置默认值
        /// </summary>
        /// <param name="section">节点/段落名称</param>
        /// <param name="key">项/Key名称</param>
        /// <param name="def">默认值</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>返回指定内容，若不存在则返回默认值def</returns>
        public static string ReadIniKeys(string section, string key, string def, string filePath)
        {
            StringBuilder temp = new StringBuilder(50);
            GetPrivateProfileString(section, key, def, temp, 50, filePath);
            return temp.ToString();
        }

        /// <summary>
        /// 保存ini
        /// </summary>
        /// <param name="section">节点/段落名称</param>
        /// <param name="key">项/Key名称</param>
        /// <param name="value">值</param>
        /// <param name="filePath">ini文件路径</param>
        public static void WriteIniKeys(string section, string key, string value, string filePath)
        {
            WritePrivateProfileString(section, key, value, filePath);
        }

        /*
         * 若value为null则会删除配置文件中对应的key
         * 若key value为null则会删除对应的section
        */

        #endregion

        #region DataType convert

        public static void u32tostr(long data, char* str)
        {
            str[0] = ASIItab[(data >> 28) & 0x0f];
            str[1] = ASIItab[(data >> 24) & 0x0f];
            str[2] = ASIItab[(data >> 20) & 0x0f];
            str[3] = ASIItab[(data >> 16) & 0x0f];
            str[4] = ASIItab[(data >> 12) & 0x0f];
            str[5] = ASIItab[(data >> 8) & 0x0f];
            str[6] = ASIItab[(data >> 4) & 0x0f];
            str[7] = ASIItab[(data >> 0) & 0x0f];
        }

        public static void u16tostr(short data, char* str)
        {
            str[0] = ASIItab[(data >> 12) & 0x0f];
            str[1] = ASIItab[(data >> 8) & 0x0f];
            str[2] = ASIItab[(data >> 4) & 0x0f];
            str[3] = ASIItab[(data >> 0) & 0x0f];
        }

        public static void u8tostr(char data, char* str)
        {
            str[0] = ASIItab[(data >> 4) & 0x0f];
            str[1] = ASIItab[(data >> 0) & 0x0f];
        }

        public static String u8tostr(char* data, short len)
        {
            String str = "";
            String temp = "";
            for (int i = 0; i < len; i++)
            {
                temp = Convert.ToString(data[i], 16);
                //temp.Format(("%02X "), data[i]);
                str += temp;
            }
            return str;
        }

        public static void strtou16(char* str, ushort* data)
        {
            char[] tm = new char[8];
            if (String2Bytes(str, tm, 4) == 0)
                return;
            u8tou16(tm, data);
        }

        public static void strtou32(char* str, ulong* data)
        {
            char[] tm = new char[16];
            if (String2Bytes(str, tm, 8) == 0)
                return;
            u8tou32(tm, data);
        }

        public static void u8tou16(char[] tm, ushort* data)
        {
            *data = (ushort)tm[0];
            *data <<= 8;
            *data |= (ushort)tm[1];
        }

        public static void u16tou8(char* tm, ushort data)
        {
            tm[0] = (char)((data >> 8) & 0xff);
            tm[1] = (char)(data & 0xff);
        }

        public static void u8tou32(char[] tm, ulong* data)
        {
            *data = tm[0]; *data <<= 8;
            *data |= tm[1]; *data <<= 8;
            *data |= tm[2]; *data <<= 8;
            *data |= tm[3];
        }

        public static void u32tou8(char* tm, ulong data)
        {
            tm[0] = (char)((data >> 24) & 0xff);
            tm[1] = (char)((data >> 16) & 0xff);
            tm[2] = (char)((data >> 8) & 0xff);
            tm[3] = (char)(data & 0xff);
        }

        public static int autocpy(char* dst, char* src, int srclen, int dstlen, char ascii)
        {
            if (srclen == 0) srclen = strlen_1(src);
            if (dstlen == 0) dstlen = srclen;

            short i;
            for (i = 0; i < dstlen && i < srclen; i++)
                dst[i] = src[i];
            for (; i < dstlen; i++)
                dst[i] = ascii;

            dst[i] = (char)0;
            return dstlen;
        }

        public static short strlen_1(char* src, short maxstrlen = 4096)
        {
            short n;
            for (n = 0; (*src != '\0') && (n < maxstrlen); src++) ++n;
            return (n);
        }

        public static short FindASCII(char* src, char ascii, short srclen)
        {
            short i;
            if (srclen <= 0)
            {
                srclen = strlen_1(src);
            }
            if (ascii == 0)
            {
                return (srclen);
            }
            for (i = 0; i < srclen; i++)
            {
                if (src[i] == ascii) return (i);
            }
            return (-1);
        }

        public static short String2Bytes(char* pSrc, char[] pDst, short nSrcLength)
        {
            short i;
            for (i = 0; i < nSrcLength; i += 2)
            {
                if ((*pSrc >= '0') && (*pSrc <= '9'))
                {
                    pDst[i] = (char)(*pSrc - '0');
                }
                else if ((*pSrc >= 'A') && (*pSrc <= 'F'))  /* A....F  */
                {
                    pDst[i] = (char)(*pSrc - 'A' + 0x0A);
                }
                else if ((*pSrc >= 'a') && (*pSrc <= 'f'))  /* a....f  */
                {
                    pDst[i] = (char)(*pSrc - 'a' + 0x0a);
                }
                else
                    return 0;

                pDst[i] <<= 4;

                pSrc++;

                if ((*pSrc >= '0') && (*pSrc <= '9'))
                {
                    pDst[i] |= (char)(*pSrc - '0');
                }
                else if ((*pSrc >= 'A') && (*pSrc <= 'F'))  /* A....F  */
                {
                    pDst[i] |= (char)(*pSrc - 'A' + 0x0A);
                }
                else if ((*pSrc >= 'a') && (*pSrc <= 'f'))  /* a....f  */
                {
                    pDst[i] |= (char)(*pSrc - 'a' + 0x0a);
                }
                else
                    return 0;

                pSrc++;
                pDst[i]++;
            }
            pDst[i] = '0';
            return (short)(nSrcLength / 2);
        }

        public static short Bytes2String(char* pSrc, char* pDst, short nSrcLength)
        {
            short i;
            for (i = 0; i < nSrcLength; i++)
            {
                *pDst++ = ASIItab[*pSrc >> 4];      // 输出高4位
                *pDst++ = ASIItab[*pSrc & 0x0f];    // 输出低4位
                pSrc++;
            }
            // 输出字符串加个结束符
            *pDst = '\0';
            // 返回目标字符串长度
            return (short)(nSrcLength * 2);
        }

        public static void StrSort(char* StrList, int Comnum)
        {
            int i = 0, j = 0;
            char StrTemp;

            for (i = 0; i < Comnum; i++)
            {
                for (j = Comnum - 2; j >= i; j--)
                {
                    if (StrList[j] > StrList[j + 1])
                    {
                        StrTemp = StrList[j];
                        StrList[j] = StrList[j + 1];
                        StrList[j + 1] = StrTemp;
                    }
                }
            }
        }

        public static int DectoBCD(int Dec, byte[] Bcd, int length)
        {
            int i;
            int temp;
            for (i = length - 1; i >= 0; i--)
            {
                temp = Dec % 100;
                Bcd[i] = Convert.ToByte(((temp / 10) << 4) + ((temp % 10) & 0x0F));
                Dec /= 100;
            }
            return 0;
        }

        public static long BCDtoDec(char* bcd, int length)
        {

            int i, tmp;
            long dec = 0;
            for (i = 0; i < length; i++)
            {
                tmp = ((bcd[i] >> 4) & 0x0F) * 10 + (bcd[i] & 0x0F);
                dec += tmp * power(100, length - 1 - i);
            }
            return dec;
        }

        public static long power(int baseI, int times)
        {
            int i;
            long rslt = 1;
            for (i = 0; i < times; i++)
                rslt *= baseI;
            return rslt;
        }

        #endregion



    }
 }
