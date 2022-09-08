using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.SsisWrapper
{
    public struct ExternalMetadataColumn
    {
        public string ColumnName;
        public string ExternalColumnName;
        public SSISDataType DataType;
        public int Length;
        public int Precision;
        public int Scale;
        public int CodePage;
    }

    public struct ExternalColumnInputMap
    {
        public ExternalMetadataColumn ExternalColumn { get; set; }
        public string InputColumnName { get; set; }
    }
}
