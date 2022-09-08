using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.JsonClasses
{
    public class Fields_Mapping
    {
        public string source_field_name { get; set; }
        public string target_field_name { get; set; }
        public string expression { get; set; }

    }
}
