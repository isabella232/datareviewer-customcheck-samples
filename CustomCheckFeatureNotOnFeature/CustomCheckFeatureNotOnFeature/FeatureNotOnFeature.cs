using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.Reviewer.Public.Engine;

////////////////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2004-2012 Esri
//
// All rights reserved under the copyright laws of the United States
// and applicable international laws, treaties, and conventions.
//
// You may freely redistribute and use this software, with or
// without modification, provided you include the original copyright
// and use restrictions.  See use restrictions in the file use_restrictions.txt.
//
////////////////////////////////////////////////////////////////////////////////

namespace CustomCheckFeatureOnFeature
{
    [Guid("c2779add-7603-4fd4-bf68-4e5e3021a67c")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("CustomCheckFeatureOnFeature.FeatureNotOnFeature")]
    public class FeatureNotOnFeature : IPLTSCNTSelectionSetValExtension
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
        /// Name of the Custom Check.
        /// </summary>
        public string Name
        {
            get
            {
                return "FeatureNotOnFeature_CS";
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
                return "Feature does not intersect other feature";
            }
        }
        /// <summary>
        /// Short description of custom check.
        /// </summary>
        public string ShortDescription
        {
            get
            {
                return "Feature Not On Feature";
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
            
            //Exit if there is nothing to check
            if (0 == ipSelectionToValidate.Count)
            {
                return new PLTSErrorCollectionClass();
            }

            //Arguments and default values
            string strTargetFeatureClassName = "";
            string strTargetSubtypeNumber = "";
            string strSourceWhereClause = "";
            string strTargetWhereClause = "";
            esriSpatialRelEnum eSpatialOperation = esriSpatialRelEnum.esriSpatialRelIntersects;
            string strSpatialRelDescription = "";
            string strUserMessage = "";

            //Split comma delimited string into array
            string[] arrayOfArguments = arguments.Split(new char[] { ',' }, StringSplitOptions.None);

            //Parse arguments
            for (int i = 0; i < arrayOfArguments.Length; i++)
            {
                if (0 == i)
                {
                    strTargetFeatureClassName = arrayOfArguments[i];
                }
                else if (1 == i)
                {
                    strTargetSubtypeNumber = arrayOfArguments[i];
                }
                else if (2 == i)
                {
                    try
                    {
                        eSpatialOperation = (esriSpatialRelEnum)Convert.ToInt32(arrayOfArguments[i]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(String.Format("Error converting spatial operation parameter to esriSpatialRelEnum. Parameter value {0}", arrayOfArguments[i]), ex);
                    }
                }
                else if (3 == i)
                {
                    strTargetWhereClause = arrayOfArguments[i];
                }
                else if (4 == i)
                {
                    strSourceWhereClause = arrayOfArguments[i];
                }
                else if (5 == i)
                {
                    strSpatialRelDescription = arrayOfArguments[i];
                }
                else if (6 == i)
                {
                    strUserMessage = arrayOfArguments[i];
                }
                else
                {
                    throw new Exception("Invalid number of arguments. Only seven arguments are allowed. Arguments: (" + arguments + ")");
                }
            }

            //Get handle to workspace
            IDataset ipSourceDataset = ipSelectionToValidate.Target as IDataset;
            IFeatureWorkspace ipFeatureWorkspace = ipSourceDataset.Workspace as IFeatureWorkspace;

            //Open the target feature class. Feature class name passed in should be fully qualified.
            IFeatureClass ipTargetFeatureClass = ipFeatureWorkspace.OpenFeatureClass(strTargetFeatureClassName);
            if (null == ipTargetFeatureClass)
            {
                throw new Exception(String.Format("Unable to open feature class {0} from workspace {1}", strTargetFeatureClassName, (ipFeatureWorkspace as IWorkspace).PathName));
            }

            string strTargetSubtypeFieldName = (ipTargetFeatureClass as ISubtypes).SubtypeFieldName;

            //Setup spatial filter to apply to target feature class
            ISpatialFilter ipTargetSF = new SpatialFilterClass();
            ipTargetSF.SpatialRel = eSpatialOperation;

            if ("*" == strTargetSubtypeNumber || String.IsNullOrEmpty(strTargetSubtypeNumber))
            {
                if (strTargetWhereClause.Length > 0)
                {
                    ipTargetSF.WhereClause = strTargetWhereClause;
                }

            }
            else
            {
                if (strTargetWhereClause.Length > 0)
                {
                    ipTargetSF.WhereClause = strTargetSubtypeFieldName + " = " + strTargetSubtypeNumber + " AND " + strTargetWhereClause;
                }
                else
                {
                    ipTargetSF.WhereClause = strTargetSubtypeFieldName + " = " + strTargetSubtypeNumber;
                }
            }

            if (eSpatialOperation == esriSpatialRelEnum.esriSpatialRelRelation)
            {
                ipTargetSF.SpatialRelDescription = strSpatialRelDescription;
            }

            //Prepare source where clause
            IQueryFilter ipSourceQF = new QueryFilterClass();

            if (strSourceWhereClause.Length > 0)
            {
                ipSourceQF.WhereClause = strSourceWhereClause;
            }

            IPLTSErrorCollection ipRevResultCollection = new PLTSErrorCollectionClass();
            
            //Loop through source geometries and perform a spatial query againts the target Feature Class.
            //For each geometry that does not satisfy the spatial relationship add a Reviewer result.
            ICursor ipCursor = null;
            ipSelectionToValidate.Search(ipSourceQF, false, out ipCursor);

            IFeatureCursor ipFeatureCursor = ipCursor as IFeatureCursor;
            IFeature  ipSourceFeature = ipFeatureCursor.NextFeature();

            while (null != ipSourceFeature)
            {
                Application.DoEvents();
                    
                IGeometry ipSourceGeometry = ipSourceFeature.ShapeCopy;
                ipTargetSF.Geometry = ipSourceGeometry;

                //If spatial filter returns zero records create a Reviewer result.
                if (ipTargetFeatureClass.FeatureCount(ipTargetSF) == 0)
                {
                    //Create a Reviewer result
                    IPLTSError2 ipReviewerResult = new PLTSErrorClass() as IPLTSError2;
                    ipReviewerResult.ErrorKind = pltsValErrorKind.pltsValErrorKindStandard;
                    ipReviewerResult.OID = ipSourceFeature.OID;
                    ipReviewerResult.LongDescription = strUserMessage;
                    ipReviewerResult.QualifiedTableName = ipSourceDataset.Name;
                    ipReviewerResult.ErrorGeometry = ipSourceGeometry;
                    ipRevResultCollection.AddError(ipReviewerResult);
                }

                ipSourceFeature = ipFeatureCursor.NextFeature();
            }//end while loop

            //Release cursor
            Marshal.ReleaseComObject(ipFeatureCursor);
            ipFeatureCursor = null;
            Marshal.ReleaseComObject(ipCursor);
            ipCursor = null;

            //Return the collection of results
            return ipRevResultCollection;
        }

        #endregion
    }
}
