/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2021 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using DynamicCsvGeneration.JsonClasses;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
    public class SqlServerTable
    {

        public string Schema { get; set; }
        public string Table { get; set; }
        public SqlServerTable(string schema, string table)
        {
            Schema = schema;
            Table = table;
        }

    }
    public class CsvBase
    {
        public TableEvent JsonSetup;
        //public QueryEvent QuerySetup;
        public string SqlCmdText;
        public Dictionary<string, string> SqlColumnType;
        public string[] SqlTypeArray;
        public string ColumnDelimiter;
        public int CodePage;
        public SqlServerTable SqlTable;
        public List<string> SourceColumns;
        public string ListOfColumns;
        string TargetFilePath;
        double unixdatetime;

        string target_bucket;

        public CsvBase(TableEvent jsonSetup)
        {
            unixdatetime = 0;
            JsonSetup = jsonSetup;
            SqlCmdText = "";
            ColumnDelimiter = "";
            ListOfColumns = "";
            List<string> sqlTypeArray = new List<string>();
            SourceColumns = new List<string>();
            SqlColumnType = new Dictionary<string, string>();
            ColumnDelimiter = jsonSetup.target_file.csv_delimiter;

            CodePage = (String.IsNullOrEmpty(JsonSetup.target_file.code_page) ? 65001 : Int32.Parse(JsonSetup.target_file.code_page));

            TargetFilePath = JsonSetup.target_file.target_path;
            //source_schema_name = jsonSetup.source_schema_name;
            //source_dataset = jsonSetup.source_dataset;
            target_bucket = JsonSetup.target_file.target_bucket;
            if (jsonSetup.source_query != null)
            {
                SqlCmdText = jsonSetup.source_query;
                return;
            }
            for (int i = 0; i < jsonSetup.fields_mapping.Length; i++)
            {
                if (SqlCmdText.Length > 0) {
                    SqlCmdText += ", ";
                    ListOfColumns += ", ";
                }
                if(jsonSetup.schema[i].type != "text")
                    SqlCmdText += jsonSetup.fields_mapping[i].target_field_name + " = " + (String.IsNullOrEmpty(jsonSetup.fields_mapping[i].expression) ? jsonSetup.fields_mapping[i].source_field_name : jsonSetup.fields_mapping[i].expression) ;
                else
                    SqlCmdText += jsonSetup.fields_mapping[i].target_field_name + " = CAST(" + (String.IsNullOrEmpty(jsonSetup.fields_mapping[i].expression) ? jsonSetup.fields_mapping[i].source_field_name : jsonSetup.fields_mapping[i].expression) + " as varchar(max))";

                SqlColumnType.Add(jsonSetup.fields_mapping[i].target_field_name, ParseSqlType(jsonSetup.schema[i].type));
                sqlTypeArray.Add(ParseSqlType(jsonSetup.schema[i].type));
                SourceColumns.Add(jsonSetup.fields_mapping[i].source_field_name);
                ListOfColumns += "'" + jsonSetup.fields_mapping[i].source_field_name + "'";
            }
            SqlTable = new SqlServerTable(jsonSetup.source_schema_name, jsonSetup.source_dataset);
            SqlCmdText += @"
            FROM ["+ SqlTable.Schema + "].[" + jsonSetup.source_dataset + "]";
            SqlCmdText = "SELECT " + SqlCmdText;
            SqlCmdText = SqlCmdText + @"
            " + (String.IsNullOrWhiteSpace(jsonSetup.source_query_where_clause) ? "" : " WHERE " + jsonSetup.source_query_where_clause);
            SqlTypeArray = sqlTypeArray.ToArray();

        }
        
        public string GetOutputGcsCommand(string zipfile, DateTime datetime)
        {
            string outputfile = GetOutputFileName(datetime);
            string cmd = "gsutil cp " + zipfile + " " + outputfile;
            return cmd;
        }
        public string GetOutputFileName(DateTime datetime, bool Isgs = true)
        {
            string strRes;
            
            string timestmp = GetTimeStamp(datetime);
            if (datetime == DateTime.MinValue)
                datetime = DateTime.Now;

            strRes = TargetFilePath;
            strRes = strRes.Replace("{YYYY}", datetime.ToString("yyyy"));
            strRes = strRes.Replace("{MM}", datetime.ToString("MM"));
            strRes = strRes.Replace("{DD}", datetime.ToString("dd"));
            
            strRes = strRes.Replace("{SCHEMA_NAME}", SqlTable.Schema);
            strRes = strRes.Replace("{DATASET}", SqlTable.Table);
            
            strRes = strRes.Replace("{TIMESTAMP}", timestmp);
            strRes = strRes.Replace("{FILE_FORMAT}", "gz");
            
            strRes = target_bucket + @"/" + strRes;
            if (Isgs)
                strRes = "gs://" + strRes;
            return strRes; 
        }
        public string GetOutputTriggerName(DateTime datetime, bool Isgs = true)
        {
            string strRes = "";
            string timestmp = GetTimeStamp(datetime);
            if (JsonSetup.target_trigger_file != null)
            {
                if (JsonSetup.target_trigger_file.target_trigger_bucket != null && JsonSetup.target_trigger_file.target_trigger_path != null)
                {
                    strRes = @"gs://" + JsonSetup.target_trigger_file.target_trigger_bucket + "/" + JsonSetup.target_trigger_file.target_trigger_path;
                    strRes = strRes.Replace("{DATASET}", JsonSetup.source_dataset.ToUpper());
                    strRes = strRes.Replace("{TIMESTAMP}", timestmp);
                }
            }
            return strRes;
        }
        public string GetTimeStamp(DateTime datetime)
        {
            string timestmp = "";
            if (datetime != DateTime.MinValue)
            {
                if (unixdatetime == 0)
                {
                    var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    unixdatetime = (datetime.ToUniversalTime() - epoch).TotalSeconds;
                }
                timestmp = ((int)unixdatetime).ToString();
            }
            else
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                unixdatetime = (DateTime.Now.ToUniversalTime() - epoch).TotalSeconds;
                timestmp = ((int)unixdatetime).ToString();
                timestmp = timestmp.Substring(0, timestmp.Length - 3) + "***";

            }
            return timestmp;
        }
        public string GenerateTrigger(string triggerfile)
        {
            triggerfile = triggerfile.Replace("<dataset name>", SqlTable.Schema);
            triggerfile = triggerfile.Replace("<datasetUPPER>", SqlTable.Table.ToUpper());
            triggerfile = triggerfile.Replace("<target schema name>",  "RESCENTRAL");
            triggerfile = triggerfile.Replace("<outputfile>", GetOutputFileName(DateTime.Now, false));
            triggerfile = triggerfile.Replace("<source_to_landing dag id>", JsonSetup.dag_id);
            triggerfile = triggerfile.Replace("<source_to_landing dag run>", JsonSetup.dag_run_id);

            return triggerfile;

        }
        string ParseSqlType(string stype)
        {
            string[] words = stype.Split('(');
            return words[0];
        }
        public virtual void GenerateCSV(string sqlserver, string databasename, string outputfile)
        {
            string connString =string.Format("Data Source =[0]; Initial Catalog =[0]; Connect Timeout = 300; Integrated Security = True;",sqlserver,databasename); 
            ToCSV(connString, SqlCmdText, outputfile);
        }
        public string ConvertEsc(string str)
        {
            if (str.Contains(','))
            {
                str = String.Format("\"{0}\"", str);
            }
            return str;
        }
        public virtual void ToCSV(string connString, string query, string strFile)
        {
        }
        public DataSet GetDataSet(string connString, string query)
        {
            DataSet ds = new DataSet();
            SqlConnection conn = new SqlConnection(connString);
            try
            {

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                
                da.Fill(ds);
                da.Dispose();
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
            finally
            {
                conn.Close();
            }
            return ds;
        }
       
    }
}