using Microsoft.SqlServer.Dts.Runtime;
using runTimeWrapper = Microsoft.SqlServer.Dts.Runtime.Wrapper;
using System;

namespace DynamicCsvGeneration.SsisWrapper
{
    public class ISFlatFileColumn
    {
        #region Properties

        /// <summary>
        /// Gets or sets the character that is the column delimiter
        /// </summary>
        public string ColumnDelimiter { get { return FlatFileColumn.ColumnDelimiter; } set { FlatFileColumn.ColumnDelimiter = value; } }

        /// <summary>
        /// Gets or sets the column type, whether "Delimited" or "Fixed Width".
        /// </summary>
        public string ColumnType { get { return FlatFileColumn.ColumnType; } set { FlatFileColumn.ColumnType = value; } }

        /// <summary>
        /// Gets or sets the value that indicates how wide a column is when the ColumnType is fixed width.
        /// </summary>
        public int ColumnWidth { get { return FlatFileColumn.ColumnWidth; } set { FlatFileColumn.ColumnWidth = value; } }

        /// <summary>
        /// Gets or sets the total number of digits in a column that is defined as a numeric data type.
        /// </summary>
        public int DataPrecision { get { return FlatFileColumn.DataPrecision; } set { FlatFileColumn.DataPrecision = value; } }

        /// <summary>
        /// Gets and sets the scale.
        /// </summary>
        public int DataScale { get { return FlatFileColumn.DataScale; } set { FlatFileColumn.DataScale = value; } }

        /// <summary>
        /// Gets or sets the DataType of the value stored in the flat file column.
        /// </summary>
        public SSISDataType DataType
        {
            get { return DtsUtility.EnumAToEnumB<Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType, SSISDataType>(FlatFileColumn.DataType); }
            set { FlatFileColumn.DataType = DtsUtility.EnumAToEnumB<SSISDataType, runTimeWrapper.DataType>(value); }
        }

        /// <summary>
        /// Gets or sets the maximum number of bytes to be read from the buffer
        /// </summary>
        public int MaximumWidth { get { return FlatFileColumn.MaximumWidth; } set { FlatFileColumn.MaximumWidth = value; } }

        /// <summary>
        /// Gets or sets a Boolean that indicates whether the column is text qualified.
        /// </summary>
        public bool TextQualified { get { return FlatFileColumn.TextQualified; } set { FlatFileColumn.TextQualified = value; } }

        /// <summary>
        /// Gets or sets a name for the column
        /// </summary>
        public string Name
        {
            get { return ((runTimeWrapper.IDTSName100)FlatFileColumn).Name; }
            set
            {
                ((runTimeWrapper.IDTSName100)FlatFileColumn).Name = value;
            }
        }

        /// <summary>
        /// Use this to indicate if the Column is the Last Column in the list of Columns. This is not a Dts property; rather a helper property for simplifying certain actions.
        /// </summary>
        public bool IsLastColumnInCollection { get; set; }

        /// <summary>
        /// The flat file connection manager that this flat file column uses.
        /// </summary>
        private ConnectionManager _parentConnectionManager;

        /// <summary>
        /// Dts Object that is Wrapped.
        /// </summary>
        internal runTimeWrapper.IDTSConnectionManagerFlatFileColumn100 FlatFileColumn { get; set; }

        #endregion

        #region ctor

    
        /// <summary>
        /// Internal ctor that accepts a IDTSConnectionManagerFlatFileColumn100
        /// </summary>
        /// <param name="inputColumn"></param>
        internal ISFlatFileColumn(runTimeWrapper.IDTSConnectionManagerFlatFileColumn100 inputColumn, ConnectionManager connMgr)
        {
            FlatFileColumn = inputColumn;
            ConnectionManager = connMgr;
        }
        

        #endregion

        #region Fields and Properties

        /// <summary>
        /// The ConnectionManager object which we want to create/manipulate.
        /// </summary>
        internal ConnectionManager ConnectionManager { get; set; }

        internal DtsProperties Properties
        {
            get { return ConnectionManager.Properties; }
        }
        public object GetConnectionManagerPropertyValue(string propertyName)
        {
            return Properties[propertyName].GetValue(ConnectionManager);
        }
        internal runTimeWrapper.IDTSConnectionManagerFlatFileColumns100 Columns_m
        {
            get { return (runTimeWrapper.IDTSConnectionManagerFlatFileColumns100)GetConnectionManagerPropertyValue("Columns"); }
        }
        /// <summary>
        /// ctor that accepts the parent flat file connection manager and a name for the flat file column
        /// </summary>
        /// <param name="fileConnection"></param>
        /// <param name="name"></param>
        public ISFlatFileColumn(ConnectionManager fileConnection, string name)
        {
            bool exists = false;
            ConnectionManager = fileConnection;
            _parentConnectionManager = fileConnection;
            var r = (runTimeWrapper.IDTSConnectionManagerFlatFileColumns100)GetConnectionManagerPropertyValue("Columns");
            //_parentConnectionManager = fileConnection;
            // runTimeWrapper.IDTSConnectionManagerFlatFileColumns100 columns = fileConnection.Properties["Columns"].GetValue(fileConnection);

            foreach (runTimeWrapper.IDTSConnectionManagerFlatFileColumn100 column in Columns_m)
            {
                
                if (((runTimeWrapper.IDTSName100)column).Name == name)
                {
                    exists = true;
                    FlatFileColumn = column;
                }
            }
            if (!(exists))
            {
                FlatFileColumn = Columns_m.Add();
                Name = name;
            }
            
        }
        /*
                 public ISFlatFileColumn(ISBaseFlatFileConnectionManager fileConnection, string name)
                {
                    bool exists = false;
                    _parentConnectionManager = fileConnection;
                    foreach (runTimeWrapper.IDTSConnectionManagerFlatFileColumn100 column in fileConnection.Columns_m)
                    {
                        if (((runTimeWrapper.IDTSName100)column).Name == name)
                        {
                            exists = true;
                            FlatFileColumn = column;
                        }
                    }
                    if (!(exists))
                    {
                        FlatFileColumn = fileConnection.Columns_m.Add();
                        Name = name;
                    }
                }
         * */
        string RowDelimiter;
        /// <summary>
        /// Extended ctor that also accepts whether the column is hte last column or not.
        /// </summary>
        /// <param name="fileConnection"></param>
        /// <param name="name"></param>
        /// <param name="isLastColumnInFile"></param>
        public ISFlatFileColumn(ConnectionManager fileConnection, string name, bool isLastColumnInFile, string rowDelimiter) :
            this(fileConnection, name)
        {
            IsLastColumnInCollection = isLastColumnInFile;
            RowDelimiter = rowDelimiter;
        }



        #endregion

        #region Methods
        public string TextQualifier
        {
            get { return (string)GetConnectionManagerPropertyValue("TextQualifier"); }
            set { SetConnectionManagerPropertyValue("TextQualifier", value); }
        }
        public void SetConnectionManagerPropertyValue(string propertyName, object value)
        {
            Properties[propertyName].SetValue(ConnectionManager, value);
        }
        public void SetColumnProperties(SSISDataType dataType, string columnType, string columnDelimiter, int columnWidth, int maximumWidth, int dataPrecision, int dataScale)
        {
            if (dataType == SSISDataType.DT_STR || dataType == SSISDataType.DT_WSTR)
            {
                DataType = dataType;
                ColumnDelimiter = (IsLastColumnInCollection ? RowDelimiter : columnDelimiter);
                ColumnType = columnType;
                ColumnWidth = columnWidth;
                MaximumWidth = maximumWidth;
                DataPrecision = dataPrecision;
                DataScale = dataScale;
                //TextQualified = (String.IsNullOrEmpty(_parentConnectionManager.TextQualifier) ? false : true);
                //OutputColumnWidth = outputColumnWidth;
            }
            else
            {
                DataType = dataType;
               // ColumnDelimiter = (IsLastColumnInCollection ? _parentConnectionManager.RowDelimiter : columnDelimiter);
                ColumnType = columnType;
                ColumnWidth = 0;
                MaximumWidth = 0;
                DataPrecision = dataPrecision;
                DataScale = dataScale;
            }
        }

        public void SetColumnDataType(SSISDataType dataType, string columnType, string columnDelimiter, int columnWidth, int maximumWidth, int dataPrecision, int dataScale)
        {
            if (dataType == SSISDataType.DT_STR)
            {
                DataType = dataType;
                ColumnDelimiter = columnDelimiter;
                ColumnType = columnType;
                ColumnWidth = columnWidth;
                MaximumWidth = maximumWidth;
                DataPrecision = dataPrecision;
                DataScale = dataScale;
                //TextQualified = (String.IsNullOrEmpty(_parentConnectionManager.TextQualifier) ? false : true);
            }
            else
            {
                DataType = dataType;
                ColumnDelimiter = columnDelimiter;
                ColumnType = columnType;
                ColumnWidth = 0;
                MaximumWidth = 0;
                DataPrecision = dataPrecision;
                DataScale = dataScale;
            }
        }

        #endregion


    }

}
