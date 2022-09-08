using System;
using System.Linq;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using Microsoft.SqlServer.Dts.Runtime.Wrapper;

namespace DynamicCsvGeneration.SsisWrapper
{
    public class ISExternalMetadataColumn
    {
        #region Properties

        #region Parent Component

        public IDTSComponentMetaData100 ParentComponent { get; set; }

        #endregion

        #region IDTSInput100 Input

        public IDTSInput100 Input
        {
            get; set;
        }
        #endregion

        #region IDTSOutput100 Output

        public IDTSOutput100 Output
        {
            get; set;
        }
        #endregion

        #region IDTSExternalMetadataColumn100 ExternalMetadataColumn

        internal IDTSExternalMetadataColumn100 ExternalMetadataColumn
        {
            get; set;
        }

        #endregion

        #region CodePage

        public int CodePage
        {
            get { return ExternalMetadataColumn.CodePage; }
            set { ExternalMetadataColumn.CodePage = value; }
        }

        #endregion

        #region DataType

        public SSISDataType DataType
        {
            get { return DtsUtility.EnumAToEnumB<Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType, SSISDataType>(ExternalMetadataColumn.DataType); }
            set { ExternalMetadataColumn.DataType = DtsUtility.EnumAToEnumB<SSISDataType, DataType>(value); }
        }

        #endregion

        #region Description

        public string Description
        {
            get { return ExternalMetadataColumn.Description; }
            set { ExternalMetadataColumn.Description = value; }
        }

        #endregion

        #region ID

        public int ID
        {
            get { return ExternalMetadataColumn.ID; }
        }

        #endregion

        #region IdentificationString

        public string IdentificationString
        {
            get { return ExternalMetadataColumn.IdentificationString; }
        }

        #endregion

        #region Length

        public int Length
        {
            get { return ExternalMetadataColumn.Length; }
            set { ExternalMetadataColumn.Length = value; }
        }

        #endregion

        #region MappedColumnID

        public int MappedColumnID
        {
            get { return ExternalMetadataColumn.MappedColumnID; }
            set { ExternalMetadataColumn.MappedColumnID = value; }
        }

        #endregion

        #region Name

        public string Name
        {
            get { return ExternalMetadataColumn.Name; }
            set { ExternalMetadataColumn.Name = value; }
        }

        #endregion

        #region Precision

        public int Precision
        {
            get { return ExternalMetadataColumn.Precision; }
            set { ExternalMetadataColumn.Precision = value; }
        }

        #endregion

        #region Scale

        public int Scale
        {
            get { return ExternalMetadataColumn.Scale; }
            set { ExternalMetadataColumn.Scale = value; }
        }

        #endregion

        #endregion

        #region ctor

        public ISExternalMetadataColumn(IDTSComponentMetaData100 parentComponent, string inputOrOutputName, string externalColumnName, bool relatedToInput)
        {
            ParentComponent = parentComponent;
            if (relatedToInput)
                InitForInput(inputOrOutputName, externalColumnName);
            else
                InitForOutput(inputOrOutputName, externalColumnName);
        }

        public ISExternalMetadataColumn(IDTSComponentMetaData100 parentComponent, string inputOrOutputName, string externalColumnName, bool relatedToInput, string associateWithColumnWithName)
            : this(parentComponent, inputOrOutputName, externalColumnName, relatedToInput)
        {
            if (relatedToInput)
                AssociateWithInputColumn(associateWithColumnWithName);
            else
                AssociateWithOutputColumn(associateWithColumnWithName);
        }

        internal ISExternalMetadataColumn(IDTSExternalMetadataColumn100 externalMetadataColumn)
        {
            ExternalMetadataColumn = externalMetadataColumn;
        }

        #endregion

        #region Associate with Input

        private void InitForInput(string inputName, string externalColumnName)
        {
            Input = ParentComponent.InputCollection[inputName];
            
            bool externalColumnExists = false;
            for (int m = 0; m < Input.ExternalMetadataColumnCollection.Count; m++)
            {
                if (Input.ExternalMetadataColumnCollection[m].Name == externalColumnName)
                {
                    externalColumnExists = true;
                    ExternalMetadataColumn = Input.ExternalMetadataColumnCollection[m];
                }
            }

            if (!(externalColumnExists))
            {
                ExternalMetadataColumn = Input.ExternalMetadataColumnCollection.New();
                ExternalMetadataColumn.Name = externalColumnName;
            }
        }

        public void AssociateWithInputColumn(string inputColumnName)
        {
            IDTSInput100 input = ParentComponent.InputCollection[0];
            IDTSVirtualInput100 vInput = input.GetVirtualInput();
            CManagedComponentWrapper DesignTimeComponent = ParentComponent.Instantiate();
            DesignTimeComponent.SetUsageType(
                    input.ID,
                    vInput,
                    vInput.VirtualInputColumnCollection[inputColumnName].LineageID,
                    DTSUsageType.UT_READONLY
                    );

            IDTSInputColumn100 inputColumn = Input.InputColumnCollection[inputColumnName];
            inputColumn.ExternalMetadataColumnID = ID;
        }

        #endregion

        #region Associate with Output

        private void InitForOutput(string outputName, string externalColumnName)
        {
            Output = ParentComponent.OutputCollection[outputName]; 

            bool externalColumnExists = false;
            for (int m = 0; m < Output.ExternalMetadataColumnCollection.Count; m++)
            {
                if (Output.ExternalMetadataColumnCollection[m].Name == externalColumnName)
                {
                    externalColumnExists = true;
                    ExternalMetadataColumn = Output.ExternalMetadataColumnCollection[m];
                }
            }

            if (!(externalColumnExists))
            {
                ExternalMetadataColumn = Output.ExternalMetadataColumnCollection.New();
                ExternalMetadataColumn.Name = externalColumnName;
            }
        }

        public void AssociateWithOutputColumn(string outputColumnName)
        {
            //IDTSOutput100 outCol = new ISOutputColumn(ParentComponent, Output.Name, outputColumnName, RowDisposition.RD_FailComponent, RowDisposition.RD_FailComponent);

            IDTSOutput100 Output = ParentComponent.OutputCollection[0];
            IDTSOutputColumn100 outCol = Output.OutputColumnCollection[0];
            //outCol.SetDataTypeProperties(DataType, Length, Precision, Scale, CodePage);
            outCol.SetDataTypeProperties(DtsUtility.EnumAToEnumB<SSISDataType, DataType>(DataType), Length, Precision, Scale, CodePage);
            outCol.ExternalMetadataColumnID = ID;

            IDTSOutput100 errorOutput = null;
            for (int o = 0; o < ParentComponent.OutputCollection.Count; o++)
            {
                if (ParentComponent.OutputCollection[o].IsErrorOut == true)
                    errorOutput = ParentComponent.OutputCollection[o];
            }

            bool errorColExists = false;
            int errorColCount = errorOutput.OutputColumnCollection.Count;
            for (int e = 0; e < errorColCount; e++)
            {
                if (errorOutput.OutputColumnCollection[e].Name == outputColumnName)
                    errorColExists = true;
            }

            if (!(errorColExists))
            {
                IDTSOutputColumn100 o = errorOutput.OutputColumnCollection.NewAt(errorColCount - 2);
                o.Name = outputColumnName;
            }
        }

        #endregion

        #region Set Data Type

        public void SetDataType(SSISDataType dataType, int length, int precision, int scale, int codePage)
        {
            DataType = dataType;
            Length = length;
            Precision = precision;
            Scale = scale;
            CodePage = codePage;
        }

        #endregion

    }

}
