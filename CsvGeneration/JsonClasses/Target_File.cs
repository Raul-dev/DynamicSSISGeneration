using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.JsonClasses
{

    public class Target_File
    {
        public string target_type { get; set; }
        public string target_dataset { get; set; }
        public string target_system { get; set; }
        public string target_project { get; set; }
        public string target_bucket { get; set; }
        public string target_path { get; set; }
        public string code_page { get; set; }
        
        public string text_qualifier { get; set; }
        public string csv_delimiter { get; set; }
    }
}
