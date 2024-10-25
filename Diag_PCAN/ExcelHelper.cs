/*
 * Xi'An ManHui Info. Science LLC
 * Created on: Nov 23, 2023
 * Author: He Jingchi
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data;
using System.Text;
using System.Collections;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.HSSF.UserModel;

namespace Diag_BUS
{
    public class ExcelHelper : IDisposable
    {
        private string fileName = null; //文件名
        private IWorkbook workbook = null;
        private FileStream fs = null;
        private bool disposed;

        private int m_nColumn, m_nRow;
        private string[,] m_ExcelValues;

        public ExcelHelper(string fileName)//构造函数，读入文件名
        {
            this.fileName = fileName;
            disposed = false;

            m_nRow = 0;
            m_nColumn = 0;
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
                if (m_ExcelValues != new string[0, 0])
                    m_ExcelValues = value;
            }
            get { return m_ExcelValues; }
        }

        /// 将excel中的数据导入到DataTable中
        /// <param name="sheetName">excel工作薄sheet的名称</param>
        /// <param name="isFirstRowColumn">第一行是否是DataTable的列名</param>
        /// <returns>返回的DataTable</returns>
        public bool ExcelToDataTable(ref DataTable data, string sheetName, bool isFirstRowColumn)
        {
            ISheet sheet = null;
            bool bResult = false;
            int startRow = 0;
            try
            {
                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read); 
                workbook = WorkbookFactory.Create(fs);
                if (sheetName != null)
                {
                    sheet = workbook.GetSheet(sheetName);
                    //如果没有找到指定的sheetName对应的sheet，则尝试获取第一个sheet
                    if (sheet == null)
                    {
                        sheet = workbook.GetSheetAt(0);
                    }
                }
                else
                {
                    sheet = workbook.GetSheetAt(0);
                }
                if (sheet != null)
                {
                    IRow firstRow = sheet.GetRow(0);
                    int cellCount = firstRow.LastCellNum; //一行最后一个cell的编号，即总的列数                   
                    int rowCount = sheet.LastRowNum; //最后一行的标号

                    //根据得到的行,列数创建二维数组
                    m_nRow = rowCount;
                    m_nColumn = cellCount;

                    if (isFirstRowColumn)
                    {
                        for (int i = firstRow.FirstCellNum; i < cellCount; ++i)
                        {
                            ICell cell = firstRow.GetCell(i);
                            if (cell != null)
                            {
                                string cellValue = cell.StringCellValue;
                                if (cellValue != null)
                                {
                                    DataColumn column = new DataColumn(cellValue);
                                    data.Columns.Add(column);
                                }
                            }
                        }
                        startRow = sheet.FirstRowNum + 1;//得到项标题后
                    }
                    else
                    {
                        startRow = sheet.FirstRowNum;
                    }
                    
                    for (int i = startRow; i <= rowCount; ++i)
                    {
                        IRow row = sheet.GetRow(i);
                        if (row == null) continue; //没有数据的行默认是null　　　　　　　

                        DataRow dataRow = data.NewRow();
                        for (int j = row.FirstCellNum; j < cellCount; ++j)
                        {
                            if (row.GetCell(j) != null) //同理，没有数据的单元格都默认是null
                            {
                                dataRow[j] = row.GetCell(j).ToString();
                            }
                        }
                        data.Rows.Add(dataRow);
                    }

                    //fs.Close();
                    bResult = true;
                }
                return bResult;
            }
            catch (Exception ex)//打印错误信息
            {
                Console.Write("Exception: " + ex.Message);          
                return bResult;
            }
        }

        //将DataTable数据导入到excel中
        //<param name="data">要导入的数据</param>
        //<param name="sheetName">要导入的excel的sheet的名称</param>
        //<param name="isColumnWritten">DataTable的列名是否要导入</param>
        //<returns>导入数据行数(包含列名那一行)</returns>
        public int DataTableToExcel(DataTable data, string sheetName, bool isColumnWritten)
        {
            int i = 0;
            int j = 0;
            int count = 0;
            ISheet sheet = null;
            String strOutFileName, strCreateTime, strFilePath;
            strFilePath = Directory.GetCurrentDirectory() + "\\UDS_ServiceCheck";
            strOutFileName = strFilePath;
            strCreateTime = String.Format("{0:d}_{0:t}", DateTime.Now);
            strCreateTime = strCreateTime.Replace('/', '_');
            strCreateTime = strCreateTime.Replace(':', '_');
            strOutFileName += strCreateTime + ".xls";

            fs = new FileStream(strOutFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            if (strOutFileName.IndexOf(".xlsx") > 0) // 2007版本(常写入失败)
                workbook = new XSSFWorkbook();
            else if (fileName.IndexOf(".xls") > 0) // 2003版本
                workbook = new HSSFWorkbook();

            try
            {
                if (workbook != null)
                {
                    sheet = workbook.CreateSheet(sheetName);
                }
                else
                {
                    return -1;
                }

                if (isColumnWritten == true) //写入DataTable的列名
                {
                    IRow row = sheet.CreateRow(0);
                    for (j = 0; j < data.Columns.Count; ++j)
                    {
                        row.CreateCell(j).SetCellValue(data.Columns[j].ColumnName);
                    }
                    count = 1;
                }
                else
                {
                    count = 0;
                }

                for (i = 0; i < data.Rows.Count; ++i)
                {
                    IRow row = sheet.CreateRow(count);
                    for (j = 0; j < data.Columns.Count; ++j)
                    {                      
                        row.CreateCell(j).SetCellValue(data.Rows[i][j].ToString());
                    }
                    ++count;
                }
                lock (fs.SafeFileHandle)
                {
                    workbook.Write(fs); //写入到excel
                }
                
                //fs.Close();
                return count;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return -1;
            }
        }

        public string WriteTace(DataTable data)
        {
            string strResult = string.Empty;

            int i = 0;
            int j = 0;
            int count = 0;
            IRow row = null;
            ISheet sheet = null;
            try
            {
                fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                workbook = new HSSFWorkbook();

                if (workbook != null)
                    sheet = workbook.CreateSheet("Trace");
                else
                    return strResult;
                //write excel first row title
                row = sheet.CreateRow(0);
                for (j = 0; j < data.Columns.Count; ++j)
                {
                    row.CreateCell(j).SetCellValue(data.Columns[j].ColumnName);
                }
                count = 1;

                //write trace data
                for (i = 0; i < data.Rows.Count; ++i)
                {
                    row = sheet.CreateRow(count);
                    for (j = 0; j < data.Columns.Count; ++j)
                    {
                        row.CreateCell(j).SetCellValue(data.Rows[i][j].ToString());
                    }
                    ++count;
                }
                lock (fs.SafeFileHandle)
                {
                    workbook.Write(fs); //写入到excel
                }

                strResult = "OK";
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                return ex.Message;
            }

            return strResult;
        }

        public void Dispose()//IDisposable为垃圾回收相关的东西，用来显式释放非托管资源
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (fs != null)
                        fs.Close();
                }
                fs = null;
                disposed = true;
            }
        }
    }
}
