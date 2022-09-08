/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/
using CommandLine;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
   
    class Program
    {

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                  .ReadFrom.AppSettings() // Other configuration here
                  .CreateLogger();

            var parseRes = CommandLine.Parser.Default.ParseArguments<CsvFileOptions, CsvFolderOptions, CsvQueryOptions>(args)
                                               .WithNotParsed(HandleParseError);
            var isOk = parseRes.MapResult(
                                  (CsvFileOptions opts) => CsvFileGeneration(opts),
                                  (CsvFolderOptions opts) => CsvFolderGeneration(opts),
                                  (CsvQueryOptions opts) => CsvQueryGeneration(opts),
                                  errs => 1);
            if(300 != isOk && 1 != isOk)
                Log.Information("Return code {0}", isOk);
#if DEBUG
            Console.WriteLine("Press any key");
            Console.ReadKey();
#endif 
            if (300 == isOk)
                System.Environment.Exit(0);
            System.Environment.Exit(isOk);
        }
    
        static void HandleParseError(IEnumerable<Error> errs)
        {
            //Handle errors
            Console.WriteLine("Simple Usage: DynamicCsvGeneration.exe file -S ServerName -d DatabaseName -o OutputFolder -i InputJsonFile");
        }

        private static int CsvFileGeneration(CsvFileOptions options)
        {
            CsvHelper tm = new CsvHelper();
            bool res = tm.DynamicCsvGeneration(options);
            if(res)
            {
                if (!String.IsNullOrWhiteSpace(options.TypeOfFilesList)) // disable log output
                    return 300;
                return 0;
            }
            return -1;
        }
        private static int CsvQueryGeneration(CsvQueryOptions options)
        {
            CsvHelper tm = new CsvHelper();
            bool res = tm.CsvQueryGeneration(options);
            if (res)
            {
                if (!String.IsNullOrWhiteSpace(options.TypeOfFilesList)) // disable log output
                    return 300;
                return 0;
            }
            return -1;
        }
        private static int CsvFolderGeneration(CsvFolderOptions options)
        {
            CsvHelper tm = new CsvHelper();
            bool res = tm.CsvFromFolderGeneration(options);

            if (res)
            {
                if (!String.IsNullOrWhiteSpace(options.TypeOfFilesList)) // disable log output
                    return 300;
                return 0;
            }
            else
                return -1;
        }
    }
}
