using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.SsisWrapper
{
    public enum UsageType
    {
        UT_READONLY = 0,
        UT_READWRITE = 1,
        UT_IGNORED = 2
    }
    public enum AdoNetSourceAccessMode
    {
        TableOrViewName = 0,
        SqlCommand = 2
    }
    /// <summary>
    /// Stand-in for DataType
    /// </summary>
    public enum SSISDataType
    {
        DT_EMPTY = 0,
        DT_NULL = 1,
        DT_I2 = 2,
        DT_I4 = 3,
        DT_R4 = 4,
        DT_R8 = 5,
        DT_CY = 6,
        DT_DATE = 7,
        DT_BOOL = 11,
        DT_DECIMAL = 14,
        DT_I1 = 16,
        DT_UI1 = 17,
        DT_UI2 = 18,
        DT_UI4 = 19,
        DT_I8 = 20,
        DT_UI8 = 21,
        DT_FILETIME = 64,
        DT_GUID = 72,
        DT_BYTES = 128,
        DT_STR = 129,
        DT_WSTR = 130,
        DT_NUMERIC = 131,
        DT_DBDATE = 133,
        DT_DBTIME = 134,
        DT_DBTIMESTAMP = 135,
        DT_DBTIME2 = 145,
        DT_DBTIMESTAMPOFFSET = 146,
        DT_IMAGE = 301,
        DT_TEXT = 302,
        DT_NTEXT = 303,
        DT_DBTIMESTAMP2 = 304,
        DT_BYREF_I2 = 16386,
        DT_BYREF_I4 = 16387,
        DT_BYREF_R4 = 16388,
        DT_BYREF_R8 = 16389,
        DT_BYREF_CY = 16390,
        DT_BYREF_DATE = 16391,
        DT_BYREF_BOOL = 16395,
        DT_BYREF_DECIMAL = 16398,
        DT_BYREF_I1 = 16400,
        DT_BYREF_UI1 = 16401,
        DT_BYREF_UI2 = 16402,
        DT_BYREF_UI4 = 16403,
        DT_BYREF_I8 = 16404,
        DT_BYREF_UI8 = 16405,
        DT_BYREF_FILETIME = 16448,
        DT_BYREF_GUID = 16456,
        DT_BYREF_NUMERIC = 16515,
        DT_BYREF_DBDATE = 16517,
        DT_BYREF_DBTIME = 16518,
        DT_BYREF_DBTIMESTAMP = 16519,
        DT_BYREF_DBTIME2 = 16520,
        DT_BYREF_DBTIMESTAMPOFFSET = 16521,
        DT_BYREF_DBTIMESTAMP2 = 16522
    }

    public struct SSISDataTypeWithProperty
    {
        public SSISDataType DataType { get; set; }
        public int CodePage { get; set; }
        public int Length { get; set; }
        public int Scale { get; set; }
        public int Precision { get; set; }
    }
}
