using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.esriSystem;
using ESRI.Reviewer.Public.Engine;
using System.Collections.Generic;

////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2008-2016 Esri
//
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
//
// You may freely redistribute and use this software, with or
// without modification, provided you include the original copyright
// and use restrictions.  See use restrictions in the file use_restrictions.txt.
//
////////////////////////////////////////////////////////////////////////////////

namespace CustomCheckValidateDomainBasedAttributes
{
    [Guid("10F6A0EB-5415-4837-B50F-62C26DB3F4E7")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CustomCheckValidateDomainBasedAttributes.ValidateDomainBasedAttributes")]
    public class ValidateDomainBasedAttributes : IPLTSCNTWorkspaceValExtension
    {
        private IWorkspace m_ipWorkspace = null;
        
        /// <summary>
        /// Enumeration for Workspace Type
        /// </summary>
        private enum DatabaseType
        {
            SHAPEFILE,
            MDB,
            FGDB,
            SQLSERVER,
            POSTGRESQL,
            ORACLE
        }

        #region IPLTSCNTSelectionSetValExtension Members
        /// <summary>
        /// Returns handle to Bitmap.  Not currently used.
        /// </summary>
        public int Bitmap
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Long description of custom check.
        /// </summary>
        public string LongDescription
        {
            get
            {
                return "Attribute value violates domain constraints";
            }
        }
        
        /// <summary>
        /// Name of the Custom Check.
        /// </summary>
        public string Name
        {
            get
            {
                return "Validate Domain Based Attributes";
            }
        }

        /// <summary>
        /// Used to get a handle to the application.
        /// </summary>
        /// <param name="hook"></param>
        public void OnCreate(object hook)
        {
           
        }
        
        /// <summary>
        /// Short description of custom check.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return "Domain constraints violated";
            }
        }
        
        /// <summary>
        /// Execute is called by Data Reviewer engine and passed the appropriate parameters.
        /// </summary>
        /// <param name="validateMe">IWorkspace object that contains the data being validated</param>
        /// <param name="arguments">comma delimited string of arguments</param>
        /// <returns>
        /// Collection of validation results.
        /// </returns>
        public IPLTSErrorCollection Execute(IWorkspace validateMe, string arguments)
        {
            m_ipWorkspace = validateMe;
            IPLTSErrorCollection ipRevResultCollection = new PLTSErrorCollectionClass();

            //Split comma delimited string into array
            //This is the array of fully qualified feature class/table names
            string[] arrayOfArguments = arguments.Split(new char[] { ',' }, StringSplitOptions.None);

            //loop through the feature classes/tables and check for any attributes that violate domain constraints
            for (int i = 0; i < arrayOfArguments.Length; i++)
            {
                string strTableName = arrayOfArguments[i];
                if (DatasetExists(validateMe, strTableName))
                {
                    ITable ipTable = (validateMe as IFeatureWorkspace).OpenTable(strTableName);
                    
                    bool bHasSubtype = false;
                    ISubtypes ipSubtypes = ipTable as ISubtypes;
                    if (null != ipSubtypes)
                        bHasSubtype = ipSubtypes.HasSubtype;
                    
                    if(bHasSubtype)
                    {
                        IEnumSubtype ipEnumSubtype = ipSubtypes.Subtypes;
                        if(null != ipEnumSubtype)
                        {
                            ipEnumSubtype.Reset();
                            int iSubtypeCode;
                            string strSubtypeName;
                            strSubtypeName = ipEnumSubtype.Next(out iSubtypeCode);
                            while(null != strSubtypeName)
                            {
                                ValidateSubtype(ipRevResultCollection, ipSubtypes, iSubtypeCode);
                                strSubtypeName = ipEnumSubtype.Next(out iSubtypeCode);
                            }
                        }
                    }
                    else
                    {
                        ValidateTable(ipRevResultCollection, ipTable);
                    }
                }
            }
            m_ipWorkspace = null;
            return ipRevResultCollection;
        }
        #endregion

        #region private methods
        /// <summary>
        /// Validates the attributes in a Feature Class/Table for domain constraints
        /// </summary>
        /// <param name="ipErrorCollection">Collection of validation results</param>
        /// <param name="ipTable">Feature class/Table whose attributes will be validated for domain constraints</param>
        private void ValidateTable(IPLTSErrorCollection ipErrorCollection, ITable ipTable)
        {
            if (null == ipTable || null == ipErrorCollection)
                return;

            //Get a list of fields that has domain applied
            List<IField> domainFields = GetDomainFields(ipTable);

            //loop through all the fields that has domain and check if there are any records that violate domain constraints.
            for (int i = 0; i < domainFields.Count; i++)
            {
                string strFieldName = domainFields[i].Name;
                int intFieldIndex = ipTable.Fields.FindField(strFieldName);
                if (intFieldIndex >= 0)
                {
                    IDomain ipDomain = domainFields[i].Domain;
                    ValidateAttributes(ipErrorCollection, ipTable, ipDomain, strFieldName, false, "", -1);
                }
            }
        }

        /// <summary>
        /// Validates the attributes in a the subtype of a Feature Class/Table for domain constraints
        /// </summary>
        /// <param name="ipErrorCollection">Collection of validation results</param>
        /// <param name="ipSubtypes">Subtypes in the feature class/table</param>
        /// <param name="iSubtypeCode">SubtypeCode of the subtype that needs to be validated</param>
        private void ValidateSubtype(IPLTSErrorCollection ipErrorCollection, ISubtypes ipSubtypes, int iSubtypeCode)
        {
            if (null == ipSubtypes || null == ipErrorCollection)
                return;
            ITable ipTable = (ipSubtypes as ITable);
            if (null == ipTable)
                return;

            string strSubtypeFieldName = ipSubtypes.SubtypeFieldName;

            IFields ipFields = ipTable.Fields;
            if(null != ipFields)
            {
                long lFieldCount = ipFields.FieldCount;
                for (int i = 0; i < ipFields.FieldCount; i++)
                {
                    IField ipField = ipFields.get_Field(i);
                    if (!(ipField.Type == esriFieldType.esriFieldTypeBlob ||
                        ipField.Type == esriFieldType.esriFieldTypeGeometry ||
                        ipField.Type == esriFieldType.esriFieldTypeOID ||
                        ipField.Type == esriFieldType.esriFieldTypeRaster ||
                        ipField.Type == esriFieldType.esriFieldTypeGlobalID ||
                        ipField.Type == esriFieldType.esriFieldTypeGUID ||
                        ipField.Type == esriFieldType.esriFieldTypeXML))
                    {
                        IDomain ipDomain = ipSubtypes.get_Domain(iSubtypeCode, ipField.Name);
                        if(null != ipDomain)
                            ValidateAttributes(ipErrorCollection, ipTable, ipDomain, ipField.Name, true, strSubtypeFieldName, iSubtypeCode);
                    }
                }
            }
        }

        
        /// <summary>
        /// Validates the attributes in a Feature Class/Table or its subtype for domain constraints
        /// </summary>
        /// <param name="ipErrorCollection">Collection of validation results</param>
        /// <param name="ipTable">Feature class/Table whose attributes will be validated for domain constraints</param>
        /// <param name="ipDomain">Domain for which attributes need to be validated</param>
        /// <param name="strDomainFieldName">Name of the domain field</param>
        /// <param name="bHasSubtype">input true if the feature class/table has subype else false</param>
        /// <param name="strSubtypeFieldName">Name of the subtype field</param>
        /// <param name="iSubtypeCode">Subtype code for the subtype that need to be validated for the domain constraints</param>
        private void ValidateAttributes(IPLTSErrorCollection ipErrorCollection, ITable ipTable, IDomain ipDomain, string strDomainFieldName, bool bHasSubtype, string strSubtypeFieldName, int iSubtypeCode)
        {
            string strErrorConditionQueryString = "";

            //Get the query string for searching records that violate domain constraints
            if (ipDomain.Type == esriDomainType.esriDTRange)
            {
                strErrorConditionQueryString = GetQueryStringForRangeDomain(ipDomain as IRangeDomain, strDomainFieldName);
            }
            else if (ipDomain.Type == esriDomainType.esriDTCodedValue)
            {
                strErrorConditionQueryString = GetQueryStringForCodedDomain(ipDomain as ICodedValueDomain, strDomainFieldName);
            }

            if (!string.IsNullOrEmpty(strErrorConditionQueryString))
            {
                //Apply subtype filter if needed
                if(bHasSubtype && !string.IsNullOrEmpty(strSubtypeFieldName))
                    strErrorConditionQueryString += " AND " + strSubtypeFieldName + " = " + iSubtypeCode;

                //Use the query string to search records that violate domain constraints
                IQueryFilter ipQF = new QueryFilter();
                ipQF.WhereClause = strErrorConditionQueryString;
                ICursor ipCursor = ipTable.Search(ipQF, true);
                if (null != ipCursor)
                {
                    IRow ipRow = ipCursor.NextRow();
                    while (null != ipRow)
                    {
                        //Create a Reviewer result
                        IPLTSError2 ipReviewerResult = new PLTSErrorClass() as IPLTSError2;
                        ipReviewerResult.ErrorKind = pltsValErrorKind.pltsValErrorKindStandard;
                        ipReviewerResult.OID = ipRow.OID;
                        ipReviewerResult.LongDescription = "Domain constraints violated for " + strDomainFieldName + " field";
                        ipReviewerResult.QualifiedTableName = (ipTable as IDataset).Name;

                        //If the record is a feature then use the feature geometry as Result's error geometry
                        IFeature ipFeature = ipRow as IFeature;
                        if (null != ipFeature)
                        {
                            IGeometry ipErrorGeometry = ipFeature.ShapeCopy;
                            if (!ipErrorGeometry.IsEmpty)
                                ipReviewerResult.ErrorGeometry = ipErrorGeometry;
                        }

                        //Add the result to the collection of results
                        ipErrorCollection.AddError(ipReviewerResult);

                        ipRow = ipCursor.NextRow();
                    }
                    //release cursor
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(ipCursor);
                    ipCursor = null;
                }
            }
            return;
        }
        
        /// <summary>
        /// Used for RangeDomain
        /// This function is used to construct a query string which when executed on the source Feature Class/Table will return the records that violate domain constraints.
        /// </summary>
        /// <param name="ipRangeDomain">Range domain applied to the field whose attributes are getting validated</param>
        /// <param name="strFieldName">Name of the field</param>
        /// <returns></returns>
        private string GetQueryStringForRangeDomain(IRangeDomain ipRangeDomain, string strFieldName)
        {
            if (null == ipRangeDomain || string.IsNullOrEmpty(strFieldName))
                return "";

            string strQueryString = strFieldName + " < {0} OR " + strFieldName + " > {1}";

            IDomain ipDomain = ipRangeDomain as IDomain;
            if(ipDomain.FieldType == esriFieldType.esriFieldTypeDate)
            {
                strQueryString = GetDateQueryStringForRangeDomain(ipRangeDomain, strFieldName);
            }
            else if(ipDomain.FieldType == esriFieldType.esriFieldTypeDouble ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeInteger ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeSingle ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeSmallInteger
                )
            {
                double dblMinValue = Convert.ToDouble(ipRangeDomain.MinValue);
                double dblMaxValue = Convert.ToDouble(ipRangeDomain.MaxValue);

                strQueryString = string.Format(strQueryString, dblMinValue.ToString(), dblMaxValue.ToString());
            }
            else
            {
                return "";
            }

            return strQueryString;
        }
        /// <summary>
        /// Used for CodedValueDomain
        /// This function is used to construct a query string which when executed on the source Feature Class/Table will return the records that violate domain constraints.
        /// </summary>
        /// <param name="ipCodedDomain">Coded value domain applied to the field whose attributes are getting validated</param>
        /// <param name="strFieldName">Name of the field</param>
        /// <returns></returns>
        private string GetQueryStringForCodedDomain(ICodedValueDomain ipCodedDomain, string strFieldName)
        {
            if (null == ipCodedDomain || string.IsNullOrEmpty(strFieldName))
                return "";

            string strQueryString = "";
            IDomain ipDomain = ipCodedDomain as IDomain;
            if (ipDomain.FieldType == esriFieldType.esriFieldTypeDate)
            {
                strQueryString = GetDateQueryStringForCodedDomain(ipCodedDomain, strFieldName);
            }
            else if (ipDomain.FieldType == esriFieldType.esriFieldTypeDouble ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeInteger ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeSingle ||
                ipDomain.FieldType == esriFieldType.esriFieldTypeSmallInteger
                )
            {
                if(ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = "";
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        if(i==0)
                            strQueryString += Convert.ToString(ipCodedDomain.get_Value(i));
                        else
                            strQueryString += "," + Convert.ToString(ipCodedDomain.get_Value(i));
                    }
                    strQueryString += ")";
                }
            }
            else if (ipDomain.FieldType == esriFieldType.esriFieldTypeString)
            {
                if (ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = "";
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        if (i == 0)
                            strQueryString += "'" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                        else
                            strQueryString += ",'" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                    }
                    strQueryString += ")";
                }
            }
            
            return strQueryString;
        }
        /// <summary>
        /// Used when the CodedValueDomain field is of Date type
        /// This function is used to construct a query string which when executed on the source Feature Class/Table will return the records that violate domain constraints.
        /// </summary>
        /// <param name="ipCodedDomain">Coded value domain applied to the Date type field whose attributes are getting validated</param>
        /// <param name="strFieldName">Name of the Date type field</param>
        /// <returns></returns>
        private string GetDateQueryStringForCodedDomain(ICodedValueDomain ipCodedDomain, string strFieldName)
        {
            if (null == ipCodedDomain || string.IsNullOrEmpty(strFieldName))
                return "";

            if (null == m_ipWorkspace)
                return "";

            string strQueryString = "";

            DatabaseType eDBType = GetDatabaseType(m_ipWorkspace);
            if (eDBType == DatabaseType.FGDB || eDBType == DatabaseType.SHAPEFILE)
            {
                if (ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        if (i == 0)
                            strQueryString += "date '" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                        else
                            strQueryString += ", date '" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                    }
                    strQueryString += ")";
                }
            }
            else if (eDBType == DatabaseType.MDB)
            {
                if (ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        if (i == 0)
                            strQueryString += "#" + Convert.ToString(ipCodedDomain.get_Value(i)) + "#";
                        else
                            strQueryString += ", #" + Convert.ToString(ipCodedDomain.get_Value(i)) + "#";
                    }
                    strQueryString += ")";
                }
            }
            else if (eDBType == DatabaseType.ORACLE)
            {
                if (ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        DateTime dateTimeValue =  Convert.ToDateTime(ipCodedDomain.get_Value(i));
                        string strYear = dateTimeValue.Year.ToString();

                        string strMonth = dateTimeValue.Month.ToString();
                        if (dateTimeValue.Month < 10)
                            strMonth = "0" + strMonth;
                        
                        string strDay = dateTimeValue.Day.ToString();
                        if (dateTimeValue.Day < 10)
                            strDay = "0" + strDay;

                        string strHour = dateTimeValue.Hour.ToString();
                        if (dateTimeValue.Hour < 10)
                            strHour = "0" + strHour;

                        string strMin = dateTimeValue.Minute.ToString();
                        if (dateTimeValue.Minute < 10)
                            strMin = "0" + strMin;

                        string strSec = dateTimeValue.Second.ToString();
                        if (dateTimeValue.Second < 10)
                            strSec = "0" + strSec;

                        string strFormattedDate = string.Format("to_date('{0}-{1}-{2} {3}:{4}:{5}', 'dd-mm-yyyy hh24:mi:ss')",strDay,strMonth,strYear,strHour,strMin,strSec);

                        if (i == 0)
                            strQueryString += strFormattedDate;
                        else
                            strQueryString += "," + strFormattedDate;
                    }
                    strQueryString += ")";
                }
            }
            else if (eDBType == DatabaseType.POSTGRESQL || eDBType == DatabaseType.SQLSERVER)
            {
                if (ipCodedDomain.CodeCount > 0)
                {
                    strQueryString = strFieldName + " NOT IN (";
                    for (int i = 0; i < ipCodedDomain.CodeCount; i++)
                    {
                        if (i == 0)
                            strQueryString += "'" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                        else
                            strQueryString += ", '" + Convert.ToString(ipCodedDomain.get_Value(i)) + "'";
                    }
                    strQueryString += ")";
                }
            }
            else
            {
                strQueryString = "";
            }
            return strQueryString;
        }
        /// <summary>
        /// Used when the RangeDomain field is of Date type
        /// This function is used to construct a query string which when executed on the source Feature Class/Table will return the records that violate domain constraints.
        /// </summary>
        /// <param name="ipRangeDomain">Range domain applied to the Date type field whose attributes are getting validated</param>
        /// <param name="strFieldName">Name of the Date type field</param>
        /// <returns></returns>
        private string GetDateQueryStringForRangeDomain(IRangeDomain ipRangeDomain, string strFieldName)
        {
            if (null == ipRangeDomain || string.IsNullOrEmpty(strFieldName))
                return "";

            if (null == m_ipWorkspace)
                return "";

            string strQueryString = strFieldName + " < {0} OR " + strFieldName + " > {1}";

            string strMinValue = Convert.ToString(ipRangeDomain.MinValue);
            string strMaxValue = Convert.ToString(ipRangeDomain.MaxValue);

            DateTime dateMinValue = Convert.ToDateTime(ipRangeDomain.MinValue);
            DateTime dateMaxValue = Convert.ToDateTime(ipRangeDomain.MaxValue);

            string strYearMin = dateMinValue.Year.ToString();
            string strMonthMin = dateMinValue.Month.ToString();
            if (dateMinValue.Month < 10)
                strMonthMin = "0" + strMonthMin;

            string strDayMin = dateMinValue.Day.ToString();
            if (dateMinValue.Day < 10)
                strDayMin = "0" + strDayMin;

            string strHourMin = dateMinValue.Hour.ToString();
            if (dateMinValue.Hour < 10)
                strHourMin = "0" + strHourMin;

            string strMinMin = dateMinValue.Minute.ToString();
            if (dateMinValue.Minute < 10)
                strMinMin = "0" + strMinMin;

            string strSecMin = dateMinValue.Second.ToString();
            if (dateMinValue.Second < 10)
                strSecMin = "0" + strSecMin;

            string strYearMax = dateMaxValue.Year.ToString();
            string strMonthMax = dateMaxValue.Month.ToString();
            if (dateMaxValue.Month < 10)
                strYearMax = "0" + strYearMax;

            string strDayMax = dateMaxValue.Day.ToString();
            if (dateMaxValue.Day < 10)
                strDayMax = "0" + strDayMax;

            string strHourMax = dateMaxValue.Hour.ToString();
            if (dateMaxValue.Hour < 10)
                strHourMax = "0" + strHourMax;

            string strMinMax = dateMaxValue.Minute.ToString();
            if (dateMaxValue.Minute < 10)
                strMinMax = "0" + strMinMax;

            string strSecMax = dateMaxValue.Second.ToString();
            if (dateMaxValue.Second < 10)
                strSecMax = "0" + strSecMax;


            string strFormattedDateMin = string.Format("to_date('{0}-{1}-{2} {3}:{4}:{5}', 'dd-mm-yyyy hh24:mi:ss')", strDayMin, strMonthMin, strYearMin, strHourMin, strMinMin, strSecMin);
            string strFormattedDateMax = string.Format("to_date('{0}-{1}-{2} {3}:{4}:{5}', 'dd-mm-yyyy hh24:mi:ss')", strDayMax, strMonthMax, strYearMax, strHourMax, strMinMax, strSecMax);


            DatabaseType eDBType = GetDatabaseType(m_ipWorkspace);
            if(eDBType == DatabaseType.FGDB || eDBType == DatabaseType.SHAPEFILE)
            {
                string strNewMinValue = "date '" + strMinValue + "'";
                string strNewMaxValue = "date '" + strMaxValue + "'";
                strQueryString = string.Format(strQueryString, strNewMinValue, strNewMaxValue);
            }
            else if (eDBType == DatabaseType.MDB)
            {
                string strNewMinValue = "#" + strMinValue + "#";
                string strNewMaxValue = "#" + strMaxValue + "#";
                strQueryString = string.Format(strQueryString, strNewMinValue, strNewMaxValue);
            }
            else if (eDBType == DatabaseType.ORACLE)
            {
                string strNewMinValue = strFormattedDateMin;
                string strNewMaxValue = strFormattedDateMax;
                strQueryString = string.Format(strQueryString, strNewMinValue, strNewMaxValue);
            }
            else if (eDBType == DatabaseType.POSTGRESQL || eDBType == DatabaseType.SQLSERVER)
            {
                string strNewMinValue = "'" + strMinValue + "'";
                string strNewMaxValue = "'" + strMaxValue + "'";
                strQueryString = string.Format(strQueryString, strNewMinValue, strNewMaxValue);
            }
            else
            {
                strQueryString = "";
            }
            return strQueryString;
        }
        /// <summary>
        /// Used to get the type of the Database
        /// </summary>
        /// <param name="ipWorkspace">Input database</param>
        /// <returns>The type of the input database</returns>
        private DatabaseType GetDatabaseType(IWorkspace ipWorkspace)
        {
            DatabaseType eDBType = DatabaseType.FGDB;
            if (null != ipWorkspace)
            {
                if (ipWorkspace.Type == esriWorkspaceType.esriFileSystemWorkspace)
                    eDBType = DatabaseType.SHAPEFILE;
                else if (ipWorkspace.Type == esriWorkspaceType.esriLocalDatabaseWorkspace)
                {
                    IWorkspaceFactory ipWSFact = ((ipWorkspace as IDataset).FullName as IWorkspaceName).WorkspaceFactory;
                    UID temp = ipWSFact.GetClassID();
                    string strWorkspaceCLSID = Convert.ToString((ipWSFact.GetClassID().Value));
                    if (strWorkspaceCLSID.ToUpper() == "{DD48C96A-D92A-11D1-AA81-00C04FA33A15}".ToUpper())
                        eDBType = DatabaseType.MDB;
                    else if (strWorkspaceCLSID.ToUpper() == "{71FE75F0-EA0C-4406-873E-B7D53748AE7E}".ToUpper())
                        eDBType = DatabaseType.FGDB;
                    else if (strWorkspaceCLSID.ToUpper() == "{A06ADB96-D95C-11D1-AA81-00C04FA33A15}".ToUpper())
                        eDBType = DatabaseType.SHAPEFILE;
                }
                else if (ipWorkspace.Type == esriWorkspaceType.esriRemoteDatabaseWorkspace)
                {
                    IDatabaseConnectionInfo2 ipDBInfo = ipWorkspace as IDatabaseConnectionInfo2;
                    esriConnectionDBMS eConnDBMS = ipDBInfo.ConnectionDBMS;
                    if (eConnDBMS == esriConnectionDBMS.esriDBMS_Oracle)
                        eDBType = DatabaseType.ORACLE;
                    else if (eConnDBMS == esriConnectionDBMS.esriDBMS_PostgreSQL)
                        eDBType = DatabaseType.POSTGRESQL;
                    else if (eConnDBMS == esriConnectionDBMS.esriDBMS_SQLServer)
                        eDBType = DatabaseType.SQLSERVER;
                }
            }
            return eDBType;
        }
        /// <summary>
        /// Used for getting a list of fields that has domain applied
        /// </summary>
        /// <param name="ipTable">Input table for which the fields are evaluated</param>
        /// <returns>list of fields that has domain applied</returns>
        private List<IField> GetDomainFields(ITable ipTable)
        {
            List<IField> domainFields = new List<IField>();

            if (null == ipTable)
                return domainFields;

            IFields ipFields = ipTable.Fields;
            
            long lFieldCount = ipFields.FieldCount;
            for (int i = 0; i < ipFields.FieldCount;i++ )
            {
                IField ipField = ipFields.get_Field(i);
                if(!(ipField.Type == esriFieldType.esriFieldTypeBlob ||
                    ipField.Type == esriFieldType.esriFieldTypeGeometry ||
                    ipField.Type == esriFieldType.esriFieldTypeOID ||
                    ipField.Type == esriFieldType.esriFieldTypeRaster ||
                    ipField.Type == esriFieldType.esriFieldTypeGlobalID||
                    ipField.Type == esriFieldType.esriFieldTypeGUID ||
                    ipField.Type == esriFieldType.esriFieldTypeXML))
                  
                {
                    IDomain ipDomain = ipField.Domain;
                    if (null != ipDomain)
                        domainFields.Add(ipField);
                }
            }
            return domainFields;
        }

        /// <summary>
        /// Used for checking if a dataset exists in a workspace
        /// </summary>
        /// <param name="ipWorkspace">Workspace to search the dataset</param>
        /// <param name="strDatasetName">Name of the dataset</param>
        /// <returns>Returns true if the dataset exists in the workspace else returns false</returns>
        private bool DatasetExists(IWorkspace ipWorkspace, string strDatasetName)
        {
            if (null == ipWorkspace || string.IsNullOrEmpty(strDatasetName))
                return false;
            try
            {
                ITable ipTable = (ipWorkspace as IFeatureWorkspace).OpenTable(strDatasetName);
                if (null != ipTable)
                {
                    ipTable = null;
                    return true;
                }
            }
            catch { }

            return false;
        }
        #endregion
    }
}
