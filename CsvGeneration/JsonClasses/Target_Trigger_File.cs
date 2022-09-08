using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.JsonClasses
{
    public class Target_Trigger_File
    {
        public string target_trigger_project { get; set; }
        public string target_trigger_bucket { get; set; }
        public string target_trigger_path { get; set; }
    }
}
