using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration.JsonClasses
{

    public class TableEvent
    {
        public string source_type { get; set; }
        public string source_dataset { get; set; }
        public string source_system { get; set; }
        public string source_schema_name { get; set; }
        public string source_query { get; set; }
        public string source_query_where_clause { get; set; }
        public string output_kafka_server { get; set; }
        public string output_kafka_topic { get; set; }
        public string dag_id { get; set; }
        public string dag_run_id { get; set; }
        public Fields_Mapping[] fields_mapping { get; set; }
        public Schema[] schema { get; set; }
        public Target_File target_file { get; set; }
        public Target_Trigger_File target_trigger_file { get; set; }
    }
}
