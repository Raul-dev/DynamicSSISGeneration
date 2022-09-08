/*# SPDX-license-identifier: Apache-2.0
##############################################################################
# Copyright (c) 2022 Raul
# All rights reserved. This program and the accompanying materials
# are made available under the terms of the Apache License, Version 2.0
# which accompanies this distribution, and is available at
# http://www.apache.org/licenses/LICENSE-2.0
##############################################################################*/

using DynamicCsvGeneration.JsonClasses;
using DynamicCsvGeneration.SsisWrapper;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using System;
using Microsoft.SqlServer.Dts.Runtime;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;
namespace DynamicCsvGeneration
{
    class CsvFromSsis : CsvBase
    {
        bool IsSaveProject;
        bool IsCreateLogEvent;
        string ProjectName;
        Project _project;
        Package _mainPackage;
        string FileName;
        string _ispacFileName;
        string _ispacFolderPath;
        string[] ColumnNames;
        ConnectionManager _DbPrjConnection;
        static string baseMetadataQuery = @"select c.name as column_name, c.column_id as column_position,
		data_type = ty.name, --CASE WHEN ty.name = 'text' THEN 'varchar' ELSE ty.name END , 
		case 
			when ty.name in ('decimal', 'numeric') then '(' + convert(varchar(10), c.precision) + ', ' + convert(varchar(10), c.scale) + ')' 
			when ty.name in ('varchar', 'char') then '(' + case c.max_length when -1 then 'max' else convert(varchar(10), c.max_length) end + ')'
			when ty.name in ('nvarchar', 'nchar') then '(' + case c.max_length when -1 then 'max' else convert(varchar(10), c.max_length/2) end + ')'
            --when ty.name in ('ntext', 'text') then '(max)' 
			else ''
		end as data_type_length,
        c.precision,
        c.scale
	from sys.objects o
		inner join sys.columns c on o.object_id = c.object_id
		inner join sys.types ty on c.user_type_id = ty.user_type_id
	where o.type ='U'";

        static protected string tempMetadataQuery = @"
        select TOP 0 *
        into #temp_table
        from(
        <Query>
        ) t1
        select c.name as column_name, c.column_id as column_position,
		    data_type = ty.name, --CASE WHEN ty.name = 'text' THEN 'varchar' ELSE ty.name END ,
		    case 
                when ty.name in ('decimal', 'numeric') then '(' + convert(varchar(10), c.precision) + ', ' + convert(varchar(10), c.scale) + ')' 
			    when ty.name in ('varchar', 'char') then '(' + case c.max_length when -1 then 'max' else convert(varchar(10), c.max_length) end + ')'
			    when ty.name in ('nvarchar', 'nchar') then '(' + case c.max_length when -1 then 'max' else convert(varchar(10), c.max_length/2) end + ')'
                --when ty.name in ('ntext', 'text') then '(max)' 
			    else ''
		    end as data_type_length,
            c.precision,
            c.scale
        from tempdb.sys.objects o
            inner join tempdb.sys.columns c on o.object_id = c.object_id
            inner join tempdb.sys.types ty on c.user_type_id = ty.user_type_id
        WHERE o.object_id = object_id(N'tempdb.dbo.#temp_table')
    ";


        public CsvFromSsis(string filename, TableEvent jsonSetup, bool isSaveProject, bool isCreateLogEvent, string outputfolder)
            : base(jsonSetup)
        {
            IsSaveProject = isSaveProject;
            IsCreateLogEvent = isCreateLogEvent;
            ProjectName = filename;
            _ispacFolderPath = outputfolder;
        }

        public override void GenerateCSV( string sqlserver, string databasename, string outputfile)
        {
            
            FileName = outputfile;
            _ispacFileName = ProjectName;
            
            bool res = GenerateSsis(sqlserver, databasename, outputfile);
            if (!res)
                throw new Exception("Unknown Exception. Csv file did not generate. Turn on event log on SSIS.");
        }
        public override void ToCSV(string connString, string query, string strFile)
        {

        }
        
        public bool GenerateSsis(string sqlserver, string databasename, string outputfile)
        {
            GenerateEmptyProject();
            DBConnection dwConn = new DBConnection(databasename, sqlserver );
            GenerateEmptyPackage(_ispacFileName, dwConn,  "DbPrjConnection");
            Log.Information("Server {0}, Database {1}.", sqlserver, databasename);
            TaskHost sqlflow = CreateCsvDataFlowTask( dwConn.GetSqlNetConnectionstring(), outputfile);
            DTSExecResult res = DTSExecResult.Failure;
            if (IsSaveProject)
            {
                Log.Information("Save project {0}{1}.ispac only.", _ispacFolderPath, _ispacFileName);
                _project.Save();
                _project.Dispose();
                return true;
            }
            else {
                Log.Information("Save project {0}{1}.ispac.", _ispacFolderPath, _ispacFileName);
                _project.Save();
                Log.Information("Started Execution.");
                /* Additional log File from generatad package
                ConnectionManager loggingConnection = _mainPackage.Connections.Add("FILE");
                loggingConnection.ConnectionString = _ispacFolderPath + @"Log\SSISPackageLog.txt";
                LogProvider provider = _mainPackage.LogProviders.Add("DTS.LogProviderTextFile");
                provider.ConfigString = loggingConnection.Name;
                _mainPackage.LoggingOptions.SelectedLogProviders.Add(provider);
                _mainPackage.LoggingOptions.EventFilterKind = DTSEventFilterKind.Inclusion;
                _mainPackage.LoggingOptions.EventFilter = new string[] { "OnPreExecute", "OnPostExecute", "OnError", "OnWarning", "OnInformation" };
                _mainPackage.LoggingMode = DTSLoggingMode.Enabled;
                */
                CsvEventHandler evnt = new CsvEventHandler();
                res = _mainPackage.Execute(null, null, evnt, null, null);
            }
            _project.Dispose();
            if (res == DTSExecResult.Success)
            {
                Log.Information("Finished Execution.");
                return true;
            }
            else
                return false;

        }
        private Project GenerateEmptyProject()
        {

            Log.Information("Creating Project {0}.", ProjectName);
            
            //  Delete the ispac if exists; otherwise the code will modify the existing ispac.
            if (File.Exists(_ispacFolderPath + @"\" + _ispacFileName + ".ispac"))
                File.Delete(_ispacFolderPath + @"\" + _ispacFileName + ".ispac");
            //  create a project
            _project = Project.CreateProject(_ispacFolderPath + @"\" + _ispacFileName + ".ispac");
            //  change some project properties
            _project.Name = ProjectName;
            _project.ProtectionLevel = DTSProtectionLevel.DontSaveSensitive;
            return _project;
        }
        protected void GenerateEmptyPackage(string packageName, DBConnection dwConn, string ssisConnectionName = "DbPrjConnection")
        {

            string ErrorExpression = @"""
INSERT INTO[audit].[DTS_LOGS]
([PACKAGE_NAME]
,[PACKAGE_LOG_ID]
,[PROCEDURE_NAME]
,[ERROR_CODE]
,[ERROR_MSG]
,[PACKAGE_DURATION]
,[CONTAINER_DURATION]
,[ERROR_DATE])
VALUES
(
'"" + @[System::PackageName] + ""'
, '"" +  @[System::PackageID] + ""', '"" + @[System::SourceName] + ""'
, "" + (DT_STR, 15, 1252) @[System::ErrorCode] + ""
, '"" + REPLACE( @[System::ErrorDescription],""'"",""''"") +""'
, "" + (DT_STR,6, 1252) DATEDIFF(""ss"", @[System::StartTime] ,GETDATE()) + ""
, "" + (DT_STR,6, 1252) DATEDIFF(""ss"", @[System::ContainerStartTime] ,GETDATE()) + ""
, GETDATE()
)""";


            Log.Information("Creating Package {0}.", packageName);
            //Create a package 
            _mainPackage = new Package(); 
            _mainPackage.Name = packageName;

            _mainPackage.TransactionOption = DTSTransactionOption.NotSupported; 
            _mainPackage.Description = "Dynamic Package";
            _mainPackage.CreatorName = "Raul";
            _mainPackage.VersionBuild = 1;
            _project.PackageItems.Add(_mainPackage, packageName + ".dtsx");

            Connections conns = _mainPackage.Connections;
            int myConns = conns.Count;
            dwConn.SSISPackageName = ssisConnectionName;
            _DbPrjConnection = _mainPackage.Connections.Add("ADO.NET");
            _DbPrjConnection.Name = ssisConnectionName;
            _DbPrjConnection.ConnectionString = dwConn.GetADONetConnectionstring();
            _DbPrjConnection.Qualifier = "System.Data.SqlClient.SqlConnection, System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
            //Error Handler            
            if (IsCreateLogEvent)
            {
                Executable eventExec = default(Executable);
                DtsEventHandler ehOnError = default(DtsEventHandler);

                ehOnError = (_mainPackage.EventHandlers.Add("OnError") as DtsEventHandler);
                eventExec = ehOnError.Executables.Add("STOCK:SQLTask");
                TaskHost stTaskHost = (TaskHost)eventExec;

                stTaskHost.Properties["Name"].SetValue(stTaskHost, "SSISPackageOnError");
                stTaskHost.Properties["Description"].SetValue(stTaskHost, "SSISPackageOnError");

                stTaskHost.Properties["Connection"].SetValue(stTaskHost, ssisConnectionName);

                stTaskHost.SetExpression("SqlStatementSource", ErrorExpression);

            }

        }
        internal CManagedComponentWrapper DesignTimeComponent { get; set; }
        internal IDTSComponentMetaData100 ComponentMetaData { get; set; }
        internal IDTSComponentMetaData100 ComponentMetaDataSource { get; set; }
        SSISDataTypeWithProperty[] GetColumnDataTypes(string connString)
        {
            SqlServerTable table = this.SqlTable;

            DataSet ds = null;
            
            if (JsonSetup.source_dataset == "query")
            {
                Log.Information("Getting metadata from JSON query.");
                // TODO: not supported CTE and subquery with ORDER BY statement. Needed advanced compilation here.
                bool isOrder = JsonSetup.source_query.Contains("ORDER BY ");
                if (isOrder)
                {
                    JsonSetup.source_query = JsonSetup.source_query.Replace("SELECT ", "SELECT TOP 100 PERCENT " );
                }
                string slquery = tempMetadataQuery.Replace("<Query>", JsonSetup.source_query);

                ds = GetDataSet(connString, slquery);
            }
            else
            {
                Log.Information("Getting metadata from SQL table [{0}].[{1}].", table.Schema, table.Table);
                string swhere = @" and o.name = '" + table.Table + @"' 
                    and schema_name(o.schema_id) = '" + table.Schema + @"' 
                    and  c.name in (" + ListOfColumns + ") ";

                ds = GetDataSet(connString, baseMetadataQuery + swhere);
            }
            

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count == 0)
                throw new Exception(@"Table " + table.Table + " not found.");
            int columnCount = dt.Rows.Count;
            ColumnNames = new string[columnCount];
            SSISDataTypeWithProperty[] columnDataTypes = new SSISDataTypeWithProperty[columnCount];
            string[] columnSqlNames = new string[columnCount];
            for (int i = 0; i < dt.Rows.Count; i++)
            {

                ColumnNames.SetValue(dt.Rows[i]["column_name"].ToString(), i);
                columnSqlNames.SetValue('[' + dt.Rows[i]["column_name"].ToString() + ']', i);
                columnDataTypes.SetValue(Converter.TranslateSqlServerDataTypeToSSISDataTypeWithProperty(dt.Rows[i]["data_type"].ToString(), dt.Rows[i]["data_type_length"].ToString(), this.CodePage), i);
            }
            return columnDataTypes;
        }
        public TaskHost CreateCsvDataFlowTask( string connString, string outputfile)
        {
            TaskHost dft = null;
            SqlServerTable table = this.SqlTable;
            SSISDataTypeWithProperty[] columnDataTypes = GetColumnDataTypes(connString);
            Executable eventExec = default(Executable);

            eventExec = _mainPackage.Executables.Add("STOCK:PipelineTask");
            dft = (TaskHost)eventExec;
            dft.Properties["Name"].SetValue(dft, "SSISPackageTaskFlow");
            dft.Properties["Description"].SetValue(dft, "SSISPackageTaskFlow");

            dft.DelayValidation = true;

            //Add a flat file source
            Log.Information("Creating AdoNet source component.");

            IDTSPipeline100 mainPipe;
            string componentMoniker;
#if SqlVersion15 
            componentMoniker = "Microsoft.SqlServer.Dts.Pipeline.DataReaderSourceAdapter, Microsoft.SqlServer.ADONETSrc, Version=15.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
#else
            componentMoniker = "Microsoft.SqlServer.Dts.Pipeline.DataReaderSourceAdapter, Microsoft.SqlServer.ADONETSrc, Version=12.0.0.0, Culture=neutral, PublicKeyToken=89845dcd8080cc91";
#endif
            mainPipe = dft.InnerObject as MainPipe;
            ComponentMetaData = mainPipe.ComponentMetaDataCollection.New(); // Adds a new "component" to the Data Flow Task's Pipeline
            ComponentMetaData.ComponentClassID = componentMoniker; 
            DesignTimeComponent = ComponentMetaData.Instantiate();
            DesignTimeComponent.ProvideComponentProperties();
            ComponentMetaData.Name = "SourceTable";
            ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(_DbPrjConnection);
            ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManagerID = _DbPrjConnection.ID;
            DesignTimeComponent.SetComponentProperty("AccessMode", (int)AdoNetSourceAccessMode.SqlCommand);
            
            string sqlCommand = SqlCmdText; 
            DesignTimeComponent.SetComponentProperty("SqlCommand", sqlCommand);
            RetrieveMetaData();
            ComponentMetaDataSource = ComponentMetaData;

            // A connection manager for each file; added to the main project
            Log.Information("Creating Flat File connection manager.");
            ConnectionManager flatFileConn = _mainPackage.Connections.Add("FLATFILE");
            if(table == null)
                flatFileConn.Name = "User_Query_Conn";
            else
                flatFileConn.Name = table.Schema + "__" + table.Table + "_Conn";
            flatFileConn.ConnectionString = outputfile;
            flatFileConn.Properties["ColumnNamesInFirstDataRow"].SetValue(flatFileConn, true);


            
            flatFileConn.Properties["Format"].SetValue(flatFileConn, "Delimited");
            flatFileConn.Properties["HeaderRowDelimiter"].SetValue(flatFileConn, "\r\n");
            flatFileConn.Properties["RowDelimiter"].SetValue(flatFileConn, "\r\n");
            string UniCodePage = CodePage.ToString();
            flatFileConn.Properties["CodePage"].SetValue(flatFileConn, UniCodePage);
            flatFileConn.Properties["TextQualifier"].SetValue(flatFileConn, "\"");
            //flatFileConn.Properties["Unicode"].SetValue(flatFileConn, true);

            //  create a FlatFile column for each column the in the source file
            for (int i = 0; i < ColumnNames.Length; i++)
            {
                ISFlatFileColumn fc = new ISFlatFileColumn(flatFileConn, ColumnNames[i], ((i == ColumnNames.Length - 1) ? true : false), "\r\n");
                fc.SetColumnProperties(columnDataTypes[i].DataType, "Delimited", ",", 0, columnDataTypes[i].Length, 0, 0);
                fc.TextQualified = true;

                if (i == ColumnNames.Length - 1)
                    fc.ColumnDelimiter = "\r\n";
                else
                    fc.ColumnDelimiter = ColumnDelimiter;

            }

#region Selected Conversion Columns
            // Microsoft.DataConvert
            Log.Information("Creating component Microsoft.DataConvert for UTF8 format.");
            CManagedComponentWrapper DesignTimeComponentConv;
            IDTSComponentMetaData100 ComponentMetaDataConv;

            componentMoniker = "Microsoft.DataConvert";
            mainPipe = dft.InnerObject as MainPipe;
            ComponentMetaDataConv = mainPipe.ComponentMetaDataCollection.New(); // Adds a new "component" to the Data Flow Task's Pipeline
            ComponentMetaDataConv.ComponentClassID = componentMoniker;
            DesignTimeComponentConv = ComponentMetaDataConv.Instantiate();
            DesignTimeComponentConv.ProvideComponentProperties();
            ComponentMetaDataConv.Name = "Convert nText";
            DesignTimeComponent = DesignTimeComponentConv;
            ComponentMetaData = ComponentMetaDataConv;
            ConnectToAnotherPipelineComponent(dft, ComponentMetaDataSource);
            RetrieveMetaData();
                       
            //Get the input collection
            //Programmatically Data Conversion component add external columns
            IDTSInput100 input = ComponentMetaData.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            IDTSOutput100 dataConvertOutput = ComponentMetaDataConv.OutputCollection[0];
       
            foreach (IDTSVirtualInputColumn100 vColumn in vInput.VirtualInputColumnCollection)
            {
                if (vColumn.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_NTEXT 
                    || vColumn.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_IMAGE
                    )
                {
                    
                    Log.Information("Convert column {0} from {1} to NTEXT ", vColumn.Name, vColumn.DataType);
                    int sourceColumnLineageId = vInput.VirtualInputColumnCollection[vColumn.Name].LineageID;
                    DesignTimeComponent.SetUsageType(ComponentMetaDataConv.InputCollection[0].ID, vInput, sourceColumnLineageId, DTSUsageType.UT_READONLY);

                    IDTSOutputColumn100 newOutputColumn = DesignTimeComponent.InsertOutputColumnAt(dataConvertOutput.ID, 0, "New"+vColumn.Name, string.Empty);

                    newOutputColumn.SetDataTypeProperties(Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_TEXT, 0, 0, 0, CodePage);

                    newOutputColumn.MappedColumnID = 0;

                    DesignTimeComponent.SetOutputColumnProperty(dataConvertOutput.ID, newOutputColumn.ID, "SourceInputColumnLineageID", sourceColumnLineageId);

                }

            }
                  
            ComponentMetaDataSource = ComponentMetaDataConv;
#endregion

            //Add a destination
            Log.Information("Creating Flat File destination component");
            componentMoniker = "Microsoft.FlatFileDestination";
            ComponentMetaData = mainPipe.ComponentMetaDataCollection.New(); // Adds a new "component" to the Data Flow Task's Pipeline
            ComponentMetaData.ComponentClassID = componentMoniker; //
            DesignTimeComponent = ComponentMetaData.Instantiate();
            DesignTimeComponent.ProvideComponentProperties();
            ComponentMetaData.Name = "DestinationCsv";
            
            DesignTimeComponent.SetComponentProperty("Overwrite", true);
            ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManager = DtsConvert.GetExtendedInterface(flatFileConn);
            ComponentMetaData.RuntimeConnectionCollection[0].ConnectionManagerID = flatFileConn.ID;
            List<ExternalColumnInputMap> externalInputMap = new List<ExternalColumnInputMap>();

            ConnectToAnotherPipelineComponent(dft, ComponentMetaDataSource);
            RetrieveMetaData(); //initiate connection
            for (int i = 0; i < ColumnNames.Length; i++)
            {
                SSISDataTypeWithProperty sdt = columnDataTypes[i];
                ExternalMetadataColumn externalColumn = new ExternalMetadataColumn();
                if (sdt.DataType == SSISDataType.DT_NTEXT
                    || sdt.DataType == SSISDataType.DT_IMAGE
                    )
                {
                    externalColumn.ExternalColumnName = "New"+ ColumnNames[i];
                    externalColumn.DataType = SSISDataType.DT_TEXT;
                }
                else
                {
                    externalColumn.ExternalColumnName = ColumnNames[i];
                    externalColumn.DataType = sdt.DataType;
                }
                externalColumn.ColumnName = ColumnNames[i];
                externalColumn.Length = sdt.Length;
                externalColumn.Precision = sdt.Precision;
                externalColumn.Scale = sdt.Scale;
                externalColumn.CodePage = sdt.CodePage;
                externalInputMap.Add(new ExternalColumnInputMap { ExternalColumn = externalColumn, InputColumnName = ColumnNames[i] }); // // Becuase we are creating the target table with the same column names, we will directly map on the name
            }

            ManualMapToTargetColumns(externalInputMap, ComponentMetaData);

            return dft;
        }
 
        private IDTSPath100 ConnectToAnotherPipelineComponent(TaskHost parentDataFlowTask ,IDTSComponentMetaData100 sourceComponent, int sourceComponentOutputIndex = 0, int inputCollectionIndex = 0)
        {
            IDTSPipeline100 mainPipe;
            mainPipe = parentDataFlowTask.InnerObject as MainPipe;
            bool pathHasStartPoint = false;
            IDTSPath100 path = null;
            foreach (IDTSPath100 dtsPath in mainPipe.PathCollection)
            {
                IDTSOutput100 output = dtsPath.StartPoint;
                if (dtsPath.StartPoint.Name == sourceComponent.OutputCollection[sourceComponentOutputIndex].Name
                    && dtsPath.StartPoint.Component.Name == sourceComponent.Name
                    )
                {
                    pathHasStartPoint = true;
                    path = dtsPath;
                }
            }
            if (pathHasStartPoint)
            mainPipe.PathCollection.RemoveObjectByID(path.ID);

            path = mainPipe.PathCollection.New();
            IDTSOutput100 pIDTSOutput = sourceComponent.OutputCollection[sourceComponentOutputIndex];
            IDTSInput100 pIDTSInput = ComponentMetaData.InputCollection[inputCollectionIndex];
            path.AttachPathAndPropagateNotifications(
                    sourceComponent.OutputCollection[sourceComponentOutputIndex],
                    ComponentMetaData.InputCollection[inputCollectionIndex]);
            IDTSInput100 pIDTSInput2 = ComponentMetaData.InputCollection[inputCollectionIndex];
            return path;
        }

        public void ManualMapToTargetColumns(List<ExternalColumnInputMap>  ExternalColumnInputColumnMap, IDTSComponentMetaData100 parentComponent)
        { 
            IDTSInput100 Input;
            Input = parentComponent.InputCollection[0];
        
            if (ExternalColumnInputColumnMap.Count > 0)
            {
                string[] viCols = new string[Input.GetVirtualInput().VirtualInputColumnCollection.Count];
                for (int i = 0; i < viCols.Length; i++)
                {
                    viCols.SetValue(Input.GetVirtualInput().VirtualInputColumnCollection[i].Name, i);
                }

                foreach (ExternalColumnInputMap map in ExternalColumnInputColumnMap)
                {
                    if (String.IsNullOrEmpty(map.InputColumnName))
                    {

                    }
                    else
                    {
                        IDTSInput100 DtsInput = parentComponent.InputCollection[0];
                        ISExternalMetadataColumn extCol = new ISExternalMetadataColumn(parentComponent, DtsInput.Name, map.ExternalColumn.ColumnName, true);
                        for (int vi = 0; vi < viCols.Length; vi++)
                        {
                            if (viCols[vi].ToLower() == map.InputColumnName.ToLower())
                            {
                                IDTSInputColumn100 ic = SetInputColumnDTSUsageType(DtsInput, map.ExternalColumn.ExternalColumnName, UsageType.UT_READONLY);
                                DesignTimeComponent.MapInputColumn(DtsInput.ID, ic.ID, extCol.ID);
                                extCol.DataType = DtsUtility.EnumAToEnumB<Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType, SSISDataType>(ic.DataType); ;
                                extCol.CodePage = ic.CodePage;
                                extCol.Length = ic.Length;
                                extCol.Precision = ic.Precision;
                                extCol.Scale = ic.Scale;
                            }
                        }
                    }
                }
            }
        }
        List<string>  _readWriteCols = new List<string>();
        private IDTSInputColumn100 SetInputColumnDTSUsageType(IDTSInput100 input, string columnName, UsageType dtsUsageType)
        {
            
            //  keep track of hte columns whose usage type is set to ut_readwrite...for these cols, we want to prevent a change to ut_readonly
            if (dtsUsageType == UsageType.UT_READWRITE)
                _readWriteCols.Add(columnName);

            IDTSVirtualInput100 virtualInput = input.GetVirtualInput();
            IDTSInputColumn100 inputColumn = DesignTimeComponent.SetUsageType(
                input.ID,
                virtualInput,
                virtualInput.VirtualInputColumnCollection[columnName].LineageID,
                DtsUtility.EnumAToEnumB<UsageType, DTSUsageType>(dtsUsageType)
                );

            return inputColumn;
        }

        public bool RetrieveMetaData()
        {
            bool success = false;

            try
            {
                DesignTimeComponent.AcquireConnections(null);
                success = true;
            }
            catch (System.Runtime.InteropServices.COMException e)
            {
                Log.Error("AcquireConn Error:: " + e.Message + " -- " + e.HResult.ToString());
                success = false;
            }

            if (success)
            {
                try
                {
                    DesignTimeComponent.ReinitializeMetaData();
                }
                catch (Exception e)
                {
                    Log.Error("ReinitializeMetaData Error:: " + e.Message);
                    success = false;
                }
            }

            if (success)
            {
                try
                {
                    DesignTimeComponent.ReleaseConnections();
                }
                catch (Exception e)
                {
                    Log.Error("ReleaseConnections Error:: " + e.Message);
                    success = false;
                }
            }
            return success;
        }
    }
}
