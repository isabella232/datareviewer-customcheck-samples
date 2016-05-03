using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.Reviewer.Public.Engine;

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

namespace CustomCheckIsNumeric
{
    [Guid("34af6bc6-fd0a-4bdc-9763-29c2e04d398a")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CustomCheckIsNumeric.IsNumericCustomCheck")]
    public class CNTExtIsNumeric : IPLTSCNTSelectionSetValExtension
    {
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
        /// Name of the Custom Check
        /// </summary>
        public string Name
        {
            get
            {
                return "CustomCheckIsNumeric_CS";
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
        /// Long description of custom check.
        /// </summary>
        public string LongDescription
        {
            get
            {
                return "Check if a specified field is a number.";
            }
        }
        /// <summary>
        /// Short description of custom check.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return "Is Numeric";
            }
        }
        /// <summary>
        /// Excecute is called by Data Reviewer engine and passed the appropriate parameters.
        /// </summary>
        /// <param name="ipSelectionToValidate">ISelectionSet of features/rows to validate</param>
        /// <param name="arguments">comma delimited string of arguments</param>
        /// <returns>
        /// Collection of validation results.
        /// </returns>
        public IPLTSErrorCollection Execute(ISelectionSet ipSelectionToValidate, string arguments)
        {
            if (null == ipSelectionToValidate)
            {
                throw new ArgumentNullException("ISelectionSet parameter is null");
            }

            if (String.IsNullOrEmpty(arguments))
            {
                throw new ArgumentNullException("string parameter is null or empty");
            }

            //Get cursor of selected features/rows
            ICursor ipCursor = null;
            ipSelectionToValidate.Search(null, true, out ipCursor);

            IDataset ipSourceDataset = ipSelectionToValidate.Target as IDataset;
            IFeatureClass ipSourceFeatureClass = null;
            
            //Setup reference to feature class to be used when creating results
            if (ipSourceDataset.Type == esriDatasetType.esriDTFeatureClass)
            {
                ipSourceFeatureClass = ipSourceDataset as IFeatureClass;
            }

            //Get the index of the field we are checking
            int iIndexOfField = -1;
            iIndexOfField = ipCursor.FindField(arguments); //arguments is the name of the field we are checking

            if (-1 == iIndexOfField)
            {
                throw new Exception(String.Format("Field {0} was not found in Dataset {1}", arguments, ipSourceDataset.Name));
            }

            //Collection of results passed back to Data Reviewer
            IPLTSErrorCollection ipRevResultCollection = new PLTSErrorCollectionClass();

            //Loop through rows and check if field is numeric
            IRow ipRow = ipCursor.NextRow();
            while (null != ipRow)
            {
                object oValue = ipRow.get_Value(iIndexOfField);
                bool bIsNumeric = false;
                if (null != oValue)
                {
                    double dOutValue;
                    bIsNumeric = double.TryParse(oValue.ToString().Trim(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.CurrentCulture,
                        out dOutValue);
                }

                if (!bIsNumeric)
                {
                    //Create Reviewer result and add to collection
                    IPLTSError2 ipRevResult = new PLTSErrorClass() as IPLTSError2;
                    ipRevResult.ErrorKind = pltsValErrorKind.pltsValErrorKindStandard;
                    ipRevResult.OID = ipRow.OID;
                    ipRevResult.QualifiedTableName = ipSourceDataset.Name;
                    ipRevResult.ShortDescription = "Field does not contain a number";
                    ipRevResult.LongDescription = oValue.ToString() + " in " + arguments + " is not a number.";

                    if (null != ipSourceFeatureClass)
                    {
                        IFeature ipFeature = ipSourceFeatureClass.GetFeature(ipRow.OID);
                        if (null != ipFeature)
                        {
                            ipRevResult.ErrorGeometry = ipFeature.ShapeCopy;
                        }
                    }

                    ipRevResultCollection.AddError(ipRevResult);
                }
                
                ipRow = ipCursor.NextRow();
            }//end while

            //Release cursor
            Marshal.ReleaseComObject(ipCursor);
            ipCursor = null;

            //Return the collection of results
            return ipRevResultCollection;
        }

        #endregion
    }
}
