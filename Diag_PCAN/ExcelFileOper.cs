using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CSharp;
using Microsoft.Office.Interop.Excel;

namespace Diag_TomossLIN
{
    class ExcelFileOper : IDisposable
    {
        Application m_ExcelObj;
        int m_nColumn, m_nRow;
        string[,] m_ExcelValues;
        bool disposed;

        public ExcelFileOper()
        {
            disposed = false;
            m_ExcelObj = new Application();
        }

        public void Dispose()
        {
            Dispose(true);
            //throw new NotImplementedException();
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (m_ExcelObj != null)
                        m_ExcelObj.Quit();
                }
                disposed = true;
            }
        }

        public int Column
        { get { return m_nColumn; } }

        public int Row
        { get { return m_nRow; } }

        /// <summary>
        /// process data from excel cells
        /// </summary>
        public string[,] ExcelValue
        {
            set 
            {
                if(m_ExcelValues != new string[0,0])
                    m_ExcelValues = value; 
            }
            get {  return m_ExcelValues; }
        }
        /// <summary>
        /// read data from excel file
        /// </summary>
        public void ReadExcelFile(string strFilePath)
        {
            try
            {
                // 创建Excel对象          
                Workbook workbook = m_ExcelObj.Workbooks.Open(strFilePath);

                // 获取第一个工作表
                Worksheet worksheet = (Worksheet)workbook.Sheets[1]; //  workbook.Sheets[1];

                // 获取单元格值
                Range range = worksheet.UsedRange;
                m_nRow = range.Rows.Count;
                m_nColumn = range.Columns.Count;
                m_ExcelValues = new string[m_nRow, m_nColumn];

                for (int i = 1; i <= m_nRow; i++)
                {
                    for (int j = 1; j <= m_nColumn; j++)
                    {
                        string cellValue = (range.Cells[i, j] as Range).Value.ToString();
                        m_ExcelValues[i, j] = cellValue;

                        Console.Write(cellValue + "\t");
                    }

                    Console.WriteLine();
                }

                // 关闭Excel对象
                workbook.Close();
            }
            catch { }
          
        }
        /// <summary>
        /// write data into excel file
        /// </summary>
        public void WriteExcelFile(string strFilePath)
        {
            try
            {
                // 创建Excel对象
                Workbook workbook = m_ExcelObj.Workbooks.Add();
                Worksheet worksheet = (Worksheet)workbook.Sheets[1];

                for (int i = 0; i < m_ExcelValues.GetLength(0); i++)  //0指的是一维
                {
                    for (int j = 0; j < m_ExcelValues.GetLength(1); j++)//1指的是二维
                    {
                        worksheet.Cells[i, j] = m_ExcelValues[i, j]; // 写入数据
                        Console.WriteLine("i is {0};j is {1}", i, j);
                    }
                }

                // 保存Excel文件
                workbook.SaveAs(strFilePath);

                // 关闭Excel对象
                workbook.Close();
            }
            catch { }        
        }
    }
}