using System;
using System.IO;
using System.Windows.Forms;

namespace Diag_BUS
{
    public partial class FlashAdressSet : Form
    {
        #region private members      
        static string m_strIniFile;
        static uint m_uAppEndAddr;
        static uint m_uCalStartAddr;

        static bool m_bAppAddrEnable;
        static bool m_bCalAddrEnable;
        
        #endregion

        #region public address info

        public uint AppEndAddress
        {
            get { return m_uAppEndAddr; }
            set { value = m_uAppEndAddr; }
        }
        public uint CalStartAddress
        {
            get { return m_uCalStartAddr; }
            set { value = m_uCalStartAddr; }
        }

        public bool Enable_AppAddr
        {
            get { return m_bAppAddrEnable; }
            set { value = m_bAppAddrEnable; }
        }
        public bool Enable_CalAddr
        {
            get { return m_bCalAddrEnable; }
            set { value = m_bCalAddrEnable; }
        }
        #endregion

        public FlashAdressSet()
        {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            //uint app_residue_bytes, cal_residue_bytes;

            uint uAppEndAddr, uCalStartAddr/*, uCalEndLength*/;            
            string strAppAddr, strCalAddr;

            strAppAddr = Convert.ToInt32(nuAppEndAddr.Value).ToString();
            strCalAddr = Convert.ToInt32(nuCalStart.Value).ToString();

            if (m_bAppAddrEnable)
                fConvert.WriteIniKeys("FlashAddr", "AppEnable", "true", m_strIniFile);
            else
                fConvert.WriteIniKeys("FlashAddr", "AppEnable", "false", m_strIniFile);
            if(m_bCalAddrEnable)
                fConvert.WriteIniKeys("FlashAddr", "CalEnable", "true", m_strIniFile);
            else
                fConvert.WriteIniKeys("FlashAddr", "CalEnable", "false", m_strIniFile);

            //fix residue bytes if address not be whole divided with CHIP_PAGESIZE
            uAppEndAddr = (uint)Convert.ToInt32(strAppAddr);
            uCalStartAddr = (uint)Convert.ToInt32(strCalAddr);
            /*
            notice: do these assignment if embedded UDS program not support earse on 0x800 integer multiple address 
             */
            m_uAppEndAddr = uAppEndAddr;
            m_uCalStartAddr = uCalStartAddr;
            //_

            //write caculate result to .ini file
            fConvert.WriteIniKeys("FlashAddr", "App", uAppEndAddr.ToString(), m_strIniFile);
            fConvert.WriteIniKeys("FlashAddr", "Cal", uCalStartAddr.ToString(), m_strIniFile);

            Close();
        }

        private void FlashAdressSet_Load(object sender, EventArgs e)
        {
            m_strIniFile = Directory.GetCurrentDirectory() + @"\FlashAddressInfo.ini";
            FileInfo fi = new FileInfo(m_strIniFile);
            if (!fi.Exists)
            {
                FileStream fs = File.Create(m_strIniFile, 50, FileOptions.None);
                fs.Close();
            }

            string strAppAddr, strCalAddr, strAppEnable, strCalEnable;
            strAppAddr = fConvert.ReadIniKeys("FlashAddr", "App", "0", m_strIniFile);
            strCalAddr = fConvert.ReadIniKeys("FlashAddr", "Cal", "0", m_strIniFile);
            strAppEnable = fConvert.ReadIniKeys("FlashAddr", "AppEnable", "true", m_strIniFile);
            strCalEnable = fConvert.ReadIniKeys("FlashAddr", "CalEnable", "true", m_strIniFile);

            if (strAppEnable.Equals("true"))
            {
                cbDataPart.Checked = true;
                nuAppEndAddr.Enabled = true;
            }
            else
            {
                cbDataPart.Checked = false;
                nuAppEndAddr.Enabled = false;
            }
            if (strCalEnable.Equals("true"))
            {
                cbCalPart.Checked = true;
                nuCalStart.Enabled = true;
            }
            else
            { 
                cbCalPart.Checked = false;
                nuCalStart.Enabled = false;
            }

            nuAppEndAddr.Value = (uint)Convert.ToInt32(strAppAddr);
            nuCalStart.Value = (uint)Convert.ToInt32(strCalAddr);
        }
        /// <summary>
        /// for init flash info on main frame lanuch
        /// </summary>
        public static void GetFlashInfo()
        {
            string strAppAddr, strCalAddr, strAppEnable, strCalEnable;

            m_strIniFile = Directory.GetCurrentDirectory() + @"\FlashAddressInfo.ini";
            strAppAddr = fConvert.ReadIniKeys("FlashAddr", "App", "0", m_strIniFile);
            strCalAddr = fConvert.ReadIniKeys("FlashAddr", "Cal", "0", m_strIniFile);
            strAppEnable = fConvert.ReadIniKeys("FlashAddr", "AppEnable", "true", m_strIniFile);
            strCalEnable = fConvert.ReadIniKeys("FlashAddr", "CalEnable", "true", m_strIniFile);

            m_uAppEndAddr = (uint)Convert.ToInt32(strAppAddr);
            m_uCalStartAddr = (uint)Convert.ToInt32(strCalAddr);

            if (strAppEnable.Equals("true"))
                m_bAppAddrEnable = true;
            else
                m_bAppAddrEnable = false;

            if (strCalEnable.Equals("true"))
                m_bCalAddrEnable = true;
            else
                m_bCalAddrEnable = false;
        }

        private void cbDataPart_CheckedChanged(object sender, EventArgs e)
        {
            if (cbDataPart.Checked)
            {
                m_bAppAddrEnable = true;
                nuAppEndAddr.Enabled = true;
            }
            else
            {
                m_bAppAddrEnable = false;
                nuAppEndAddr.Enabled = false;
            }                
        }

        private void cbCalPart_CheckedChanged(object sender, EventArgs e)
        {
            if (cbCalPart.Checked)
            {
                m_bCalAddrEnable = true;
                nuCalStart.Enabled = true;
            }
            else
            {
                m_bCalAddrEnable = false;
                nuCalStart.Enabled = false;
            }
        }
    }
}
