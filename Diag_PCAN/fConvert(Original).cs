using System;
using System.Collections.Generic;
using System.Text;

namespace Diag_PCAN
{
    public static class fConvert
    {
        static char[] ASIItab = new char[] { '0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F' };
        public static unsafe void u32tostr(long data, char* str)
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

        public static unsafe void u16tostr(short data, char* str)
        {
            str[0] = ASIItab[(data >> 12) & 0x0f];
            str[1] = ASIItab[(data >> 8) & 0x0f];
            str[2] = ASIItab[(data >> 4) & 0x0f];
            str[3] = ASIItab[(data >> 0) & 0x0f];
        }

        public static unsafe void u8tostr(char data, char* str)
        {
            str[0] = ASIItab[(data >> 4) & 0x0f];
            str[1] = ASIItab[(data >> 0) & 0x0f];
        }

        public static unsafe String u8tostr(char* data, short len)
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

        public static unsafe void strtou16(char* str, ushort* data)
        {
            char[] tm= new char[8];
            if (String2Bytes(str, tm, 4) == 0) 
                return;
            u8tou16(tm, data);
        }

        public static unsafe void strtou32(char* str, ulong* data)
        {
            char[] tm = new char[16];
            if (String2Bytes(str, tm, 8) == 0) 
                return;
            u8tou32(tm, data);
        }

        public static unsafe void u8tou16(char[] tm, ushort* data)
        {
            *data = (ushort)tm[0];
            *data <<= 8;
            *data |= (ushort)tm[1];
        }

        public static unsafe void u16tou8(char* tm, ushort data)
        {
            tm[0] = (char)((data >> 8) & 0xff);
            tm[1] = (char)(data & 0xff);
        }

        public static unsafe void u8tou32(char[] tm, ulong* data)
        {
            *data = tm[0]; *data <<= 8;
            *data |= tm[1]; *data <<= 8;
            *data |= tm[2]; *data <<= 8;
            *data |= tm[3];
        }

        public static unsafe void u32tou8(char* tm, ulong data)
        {
            tm[0] = (char)((data >> 24) & 0xff);
            tm[1] = (char)((data >> 16) & 0xff);
            tm[2] = (char)((data >> 8) & 0xff);
            tm[3] = (char)(data & 0xff);
        }

        public static unsafe int autocpy(char* dst, char* src, int srclen, int dstlen, char ascii)
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

        public static unsafe short strlen_1(char* src, short maxstrlen = 4096)
        {
            short n;
            for (n = 0; (*src != '\0') && (n < maxstrlen); src++) ++n;
            return (n);
        }

        public static unsafe short FindASCII(char* src, char ascii, short srclen)
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

        public static unsafe short String2Bytes(char* pSrc, char[] pDst, short nSrcLength)
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

        public static unsafe short Bytes2String(char* pSrc, char* pDst, short nSrcLength)
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

        public static unsafe void StrSort(char* StrList, int Comnum)
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



}
}
