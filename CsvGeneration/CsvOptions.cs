/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicCsvGeneration
{
    //Command line parameters
    public class BaseOptions
    {
        [Option('S', "DB Server name.", Required = false, Default = "localhost", HelpText = "DB Server name.")]
        public string ServerName { get; set; }

        [Option('d', "Database name.", Required = false, Default = "AdventureWorks2014", HelpText = "Database name.")]
        public string DatabaseName { get; set; }

        [Option('o', "Output folder.", Required = false, Default = @".\", HelpText = "Output folder")]
        public string OutputFolder { get; set; }

        [Option('p', "Save project file.", Required = false, Default = false, HelpText = "Save project file.")]
        public bool IsSaveProject { get; set; }

        [Option('l', "Create Log Event", Required = false, Default = false, HelpText = "Create DB Log Event.")]
        public bool IsCreateLogEvent { get; set; }
        
        [Option('z', "Archive csv files.", Required = false, Default = true, HelpText = "Archive csv files.")]
        public bool IsArchive { get; set; }
        [Option('a', "Get name of files.", Required = false, Default = @"", HelpText = "Get list of source or target files.")]
        public string TypeOfFilesList { get; set; } // enum source,target,gcscommand,trigger

        [Option('t', "Input json template.", Required = false, Default = @".\JsonTemplate\Trigger_Template.json", HelpText = "Input json template")]
        public string TriggerFile { get; set; }

    }

    [Verb("file", isDefault: true, HelpText = "Generate csv from sql table.")]
    public class CsvFileOptions: BaseOptions
    {
        [Option('i', "Input json file.", Required = false, Default = @".\JsonTemplate\Production_Document.json", HelpText = "Input json file")]
        public string InputFile { get; set; }

    }
    [Verb("query", isDefault: false, HelpText = "Generate csv from custom sql query.")]
    public class CsvQueryOptions : BaseOptions
    {
        [Option('i', "Input json file.", Required = false, Default = @".\JsonTemplate\Query.json", HelpText = "Input json file")]
        public string InputFile { get; set; }

    }
    [Verb("folder", isDefault: false, HelpText = "Generate csv from json folder.")]
    public class CsvFolderOptions: BaseOptions
    {
        [Option('f', "Input folder.", Required = false, Default = @".\JsonTemplate\", HelpText = "Input folder.")]
        public string InputFolder { get; set; }

    }
}
