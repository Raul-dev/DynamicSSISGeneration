/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
    public class CsvFromDataTable
    {
        private DataTable dataTable = new DataTable();

        public CsvFromDataTable()
        {

        }

        public void PullData(string connString, string query)
        {
            
            SqlConnection conn = new SqlConnection(connString);
            SqlCommand cmd = new SqlCommand(query, conn);
            conn.Open();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dataTable);
            conn.Close();
            da.Dispose();
        }
        public void GenerateCSV(TableEvent jsonDeserialized, string sqlserver, string databasename, string outputfile)
        {
            string connString = string.Format("Data Source =[0]; Initial Catalog =[0]; Connect Timeout = 300; Integrated Security = True;",sqlserver,databasename);
            string sqlCmd ="";
            DataTable dt = new DataTable();
            foreach (Fields_Mapping fm in jsonDeserialized.fields_mapping)
            {
                if(sqlCmd.Length > 0)
                    sqlCmd += ", ";
                sqlCmd += fm.target_field_name + " = " + fm.source_field_name;
            }
            sqlCmd += @"
            FROM " + jsonDeserialized.source_dataset;
            sqlCmd = "SELECT " + sqlCmd;

            ToCSV( outputfile);

            //small
            //PullData(connString, sqlCmd);
            //ToCSV(outputfile);
        }
        public void ToCSV(string strFilePath)
        {
            Log.Debug("Started upload.");
            using (StreamWriter sw = new StreamWriter(strFilePath, false))
            {
                //headers
                for (int i = 0; i < dataTable.Columns.Count; i++)
                {
                    sw.Write(dataTable.Columns[i]);
                    if (i < dataTable.Columns.Count - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
                int iCount = 0;
                foreach (DataRow dr in dataTable.Rows)
                {
                    for (int i = 0; i < dataTable.Columns.Count; i++)
                    {
                        if (!Convert.IsDBNull(dr[i]))
                        {
                            string value = dr[i].ToString();
                            if (value.Contains(','))
                            {
                                value = String.Format("\"{0}\"", value);
                                sw.Write(value);
                            }
                            else
                            {
                                sw.Write(dr[i].ToString());
                            }
                        }
                        if (i < dataTable.Columns.Count - 1)
                        {
                            sw.Write(",");
                        }
                    }
                    sw.Write(sw.NewLine);
                    if (iCount % 1000 == 0)
                        Log.Debug("[0] rows were saved.", iCount);
                }

                sw.Close();
            }
            Log.Debug("Finished upload.");
        }
    }


}
