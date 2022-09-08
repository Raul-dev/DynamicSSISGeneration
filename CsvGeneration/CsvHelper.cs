/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using DynamicCsvGeneration.JsonClasses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
    public class CsvHelper
    {
        List<Tuple<string, TableEvent>> TableEventList;
        string GetCommandLine(BaseOptions option)
        {
            string strRes = "-S " + option.ServerName + " -d " + option.DatabaseName + " -o " + option.OutputFolder + (String.IsNullOrWhiteSpace(option.TypeOfFilesList) ? "" : " -a " + option.TypeOfFilesList) ;
            strRes = strRes + ((option.IsArchive == true) ? " -z " : "");
            return strRes;
        }
        public bool CsvQueryGeneration(CsvQueryOptions option)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(option.TypeOfFilesList))
                {
                    string cmd = " query " + GetCommandLine((BaseOptions)option);
                    Log.Debug("Command line: {0}", cmd);
                    Log.Debug("Get setup from {0}.", option.InputFile);
                }
                option = ParseCsvQueryFileOptions(option);
                
                string ljson = File.ReadAllText(option.InputFile);
                TableEvent jsonDeserializedClass = JsonConvert.DeserializeObject<TableEvent>(ljson);

                //1 SSIS
                CsvFromSsis cv = new CsvFromSsis(Path.GetFileNameWithoutExtension(option.InputFile), jsonDeserializedClass, option.IsSaveProject, option.IsCreateLogEvent, option.OutputFolder);
                if (!String.IsNullOrWhiteSpace(option.TypeOfFilesList))
                {
                    if (option.TypeOfFilesList == "target")
                    {
                        string outfile = cv.GetOutputFileName(DateTime.Now);
                        string trigger = File.ReadAllText(option.TriggerFile);
                        Console.WriteLine(outfile);
                        return true;
                    }
                    if (option.TypeOfFilesList == "gcscommand")
                    {
                        string outfile = cv.GetOutputGcsCommand(option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".gz", DateTime.Now);
                        string trigger = File.ReadAllText(option.TriggerFile);
                        trigger = cv.GenerateTrigger(trigger);
                        FileHelper.SaveText(trigger, Path.GetFileNameWithoutExtension(option.InputFile) + ".json", option.OutputFolder + "OutputQueryCsv\\");
                        Console.WriteLine(outfile);
                        return true;
                    }
                    if (option.TypeOfFilesList == "trigger")
                    {
                        string outfile = cv.GetOutputTriggerName(DateTime.Now);
                        Console.WriteLine(outfile);
                        return true;
                    }
                    return false;
                }

                cv.GenerateCSV(option.ServerName, option.DatabaseName, option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv");
                //Fixed convertation
                //CopyUTF16ToUTF8(option.OutputFolder + "OutputQueryCsv\\", Path.GetFileNameWithoutExtension(option.InputFile) + ".csv");
                string logoutfile = option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv";
                Log.Information("Source file: " + logoutfile);
                /* gs destination
                logoutfile = cv.GetOutputFileName(DateTime.MinValue);
                Log.Information("Target file: " + logoutfile);
                */
                if (option.IsArchive)
                {
                    string outputFile = Path.GetFileNameWithoutExtension(option.InputFile) + ".zip";
                    logoutfile = option.OutputFolder + "OutputQueryCsv\\" + outputFile;
                    Log.Information("Local archive file: " + logoutfile);
                    FileHelper.CompressFile(option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv", logoutfile);
                    //FileHelper.CompressGzipFile(option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv", option.OutputFolder + "OutputQueryCsv\\" + outputFile);

                    //File.Delete(option.OutputFolder + "OutputQueryCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv");
                    //need to add nuget Google.Cloud.Storage.v1 version 2.40
                    //outputFile = cv.GetOutputFileName(DateTime.Now);
                    //FileHelper.UploadFile("dbprj-landing-dev", option.OutputFolder + "OutputQueryCsv\\" + outputFile, outputFile);

                }

                //2 Other variant for small files
                //CsvFromDataTable cv = new CsvFromDataTable();
                //cv.GenerateCSV(jsonDeserializedClass, option.ServerName, option.DatabaseName, option.OutputFolder + "GeneratedFile.csv");
                //cv.ToCSV(cv.GetDataTable(jsonDeserializedClass), );
                //GetDataTable(jsonDeserializedClass,)
            }
            catch (Exception ex)
            {
                Log.Error("DynamicCsvGeneration: " + ex.Message);
                return false;
            }
            return true;
        }
        public bool DynamicCsvGeneration(CsvFileOptions option)
        {
            try
            {

                if (String.IsNullOrWhiteSpace(option.TypeOfFilesList))
                {
                    string cmd = " file " + GetCommandLine((BaseOptions)option) + " -i " + option.InputFile;
                    Log.Debug("Command line: {0}", cmd);
                    Log.Debug("Get setup from {0}.", option.InputFile);
                }
                option = ParseCsvFileOptions(option);
                string ljson = File.ReadAllText(option.InputFile);
                TableEvent jsonDeserializedClass = JsonConvert.DeserializeObject<TableEvent>(ljson);
                
                //1 SSIS
                CsvFromSsis cv = new CsvFromSsis(Path.GetFileNameWithoutExtension(option.InputFile), jsonDeserializedClass, option.IsSaveProject, option.IsCreateLogEvent, option.OutputFolder);
                if (!String.IsNullOrWhiteSpace(option.TypeOfFilesList)) {
                    if (option.TypeOfFilesList == "target")
                    {
                        string outfile = cv.GetOutputFileName(DateTime.Now);
                        string trigger = File.ReadAllText(option.TriggerFile);
                        trigger = cv.GenerateTrigger(trigger);
                        FileHelper.SaveText(trigger, Path.GetFileNameWithoutExtension(option.InputFile) + ".json", option.OutputFolder + "OutputCsv\\");
                        Console.WriteLine(outfile);
                        return true;
                    }
                    if (option.TypeOfFilesList == "gcscommand")
                    {
                        string outfile = cv.GetOutputGcsCommand(option.OutputFolder + "OutputCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".gz", DateTime.Now);
                        string trigger = File.ReadAllText(option.TriggerFile);
                        trigger = cv.GenerateTrigger(trigger);
                        FileHelper.SaveText(trigger, Path.GetFileNameWithoutExtension(option.InputFile) + ".json", option.OutputFolder + "OutputCsv\\");
                        Console.WriteLine(outfile);
                        return true;
                    }
                    if (option.TypeOfFilesList == "trigger")
                    {
                        string outfile = cv.GetOutputTriggerName(DateTime.Now);
                        Console.WriteLine(outfile);
                        return true;
                    }
                    return false;
                }
                
                cv.GenerateCSV(option.ServerName, option.DatabaseName, option.OutputFolder + "OutputCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv");
                string logoutfile = option.OutputFolder + "OutputCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv";
                Log.Information("Local output csv file: " + logoutfile);

                if (option.IsArchive)
                {
                    /*
                    logoutfile = cv.GetOutputFileName(DateTime.MinValue);
                    Log.Information("Target file: " + logoutfile);
                    // Modified trigger file
                    logoutfile = cv.GetOutputTriggerName(DateTime.MinValue);
                    Log.Information("Trigger file: " + logoutfile);
                    */

                    
                    string outputFile = Path.GetFileNameWithoutExtension(option.InputFile) +".zip";
                    logoutfile = option.OutputFolder + "OutputCsv\\" + outputFile;
                    Log.Information("Local archive file: " + logoutfile);
                    FileHelper.CompressFile(option.OutputFolder + "OutputCsv\\" + Path.GetFileNameWithoutExtension(option.InputFile) + ".csv", logoutfile);
                }
            }
            catch (Exception ex)
            {
                Log.Error("DynamicCsvGeneration: " + ex.Message);
                return false;
            }
            return true;
        }
        protected void CopyUTF16ToUTF8(string folder, string filename)
        {
            string source = folder + filename;
            string destination = folder + Path.GetFileNameWithoutExtension(filename) +"U8." + Path.GetExtension(filename);
            FileHelper.CopyUTF16ToUTF8(source, destination);
            File.Delete(source);
            File.Move(destination, source);
        }
        protected CsvFolderOptions ParseCsvFolderOptions(CsvFolderOptions option)
        {
            ParseOutputFolder((BaseOptions)option);
            option.InputFolder = ParseFolderPath(option.InputFolder);
            option.OutputFolder = ParseFolderPath(option.OutputFolder);
            return option;
        }
        protected CsvFileOptions ParseCsvFileOptions(CsvFileOptions option)
        {
            option.OutputFolder = ParseOutputFolder((BaseOptions)option);
            return option;
        }
        protected CsvQueryOptions ParseCsvQueryFileOptions(CsvQueryOptions option)
        {
            option.OutputFolder = ParseOutputFolder((BaseOptions)option);
            return option;
        }

        protected string ParseOutputFolder(BaseOptions option)
        {
            
            option.OutputFolder = ParseFolderPath(option.OutputFolder);
            //Check folder
            if (!Directory.Exists(option.OutputFolder))
            {
                Directory.CreateDirectory(option.OutputFolder);
            }
            if (!Directory.Exists(option.OutputFolder + "OutputCsv\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "OutputCsv\\");
            }
            if (!Directory.Exists(option.OutputFolder + "InputJson\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "InputJson\\");
            }
            if (!Directory.Exists(option.OutputFolder + "OutputCsv\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "OutputCsv\\");
            }
            if (!Directory.Exists(option.OutputFolder + "InputQueryJson\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "InputQueryJson\\");
            }
            if (!Directory.Exists(option.OutputFolder + "Log\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "Log\\");
            }
            if (!Directory.Exists(option.OutputFolder + "OutputQueryCsv\\"))
            {
                Directory.CreateDirectory(option.OutputFolder + "OutputQueryCsv\\");
            }
            
            return option.OutputFolder;
        }
        protected string ParseFolderPath(string path)
        {
            var q1 = path.Substring(path.Length-1, 1);
            if (q1 != @"\")
                path += @"\";
            return path;
        }
        public bool CsvFromFolderGeneration(CsvFolderOptions ioption)
        {
            try
            {
                string ljson;
                CsvFolderOptions option; // = new CsvFolderOptions();
                option = ParseCsvFolderOptions(ioption);
                TableEvent jsonDeserializedClass = null;
                
                TableEventList = new List<Tuple<string, TableEvent>>();
                FileHelper fh = new FileHelper();
                string[] files = fh.GetFilesFromDir(option.InputFolder);
                if (!String.IsNullOrWhiteSpace(option.TypeOfFilesList))
                    if (option.TypeOfFilesList == "source")
                    {
                        for (int i = 0; i < files.Length; i++)
                        {
                            Console.WriteLine(Path.GetFileNameWithoutExtension(files[i]));
                        }
                        return true;
                    }

                for (int i = 0; i < files.Length; i++)
                {
                    Log.Debug("Read json {0}.", files[i]);
                    ljson = File.ReadAllText(files[i]);
                    jsonDeserializedClass = JsonConvert.DeserializeObject<TableEvent>(ljson);
                    
                    TableEventList.Add(new Tuple<string, TableEvent>(Path.GetFileNameWithoutExtension(files[i]), jsonDeserializedClass));


                }
                CsvFromSsis cv = null;
                foreach (Tuple<string, TableEvent> lfe in TableEventList)
                {
                    
                    cv = new CsvFromSsis(lfe.Item1, lfe.Item2, option.IsSaveProject, option.IsCreateLogEvent, option.OutputFolder);
                    cv.GenerateCSV(option.ServerName, option.DatabaseName, option.OutputFolder + "OutputCsv\\" + lfe.Item1 + ".csv");
                    if (option.IsArchive) {
            
                        string outputFile = Path.GetFileNameWithoutExtension(lfe.Item1) + ".zip";
                        string logoutfile = option.OutputFolder + "OutputCsv\\" + outputFile;
                        FileHelper.CompressFile(option.OutputFolder + "OutputCsv\\" + lfe.Item1 + ".csv", logoutfile);
                        Log.Information("Local archive file: " + logoutfile);
                        //File.Delete(option.OutputFolder + "OutputCsv\\" + lfe.Item1 + ".csv");
                        //need to add nuget Google.Cloud.Storage.v1 version 2.40
                        //FileHelper.UploadFile("dbprj-landing-dev", option.OutputFolder + "OutputCsv\\" + outputFile, outputFile);
                    }
                }

            }
            catch (Exception ex)
            {
                Log.Error("DynamicCsvGeneration: " + ex.Message);
                return false;
            }
            return true;
        }

    }
}

