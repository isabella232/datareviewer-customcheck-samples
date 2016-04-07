#Topic : Create Custom Check (ArcGIS 10.4)

##Introduction
Custom checks are programs that can be incorporated into the ArcGIS Data Reviewer framework. These programs can be executed as individual checks or as part of a batch job. They can validate a specific set of features, an entire feature class or multiple feature classes/tables within a workspace.

----------
     Note: This sample was created using ArcGIS 10.4 Data Reviewer and may be freely used and redistributed "AS-IS". 
     No technical support is provided.

[ArcGIS Data Reviewer](http://www.esri.com/software/arcgis/extensions/arcgis-data-reviewer/index.html) provides over 42 out-of-the-box [checks](http://desktop.arcgis.com/en/arcmap/latest/extensions/data-reviewer/checks-in-data-reviewer.htm) that can be run individually or grouped into [batch jobs](http://desktop.arcgis.com/en/arcmap/latest/extensions/data-reviewer/batch-jobs-and-data-reviewer.htm) (.rbj files). If these included checks do not meet your specific requirements, these samples can help you write a custom check to meet your specific organization requirements.

## Contents 

This folder contains following Data Reviewer Custom Check Samples.

* [CustomCheckFeatureNotOnFeature (c#)](https://github.com/ArcGIS/DataReviewerCustomCheckSamples/tree/master/CustomCheckFeatureNotOnFeature)
* [CustomCheckIsNumeric (c#)](https://github.com/ArcGIS/DataReviewerCustomCheckSamples/tree/master/CustomCheckIsNumeric)
* [CustomCheckValidateDomainBasedAttributes (c#)](https://github.com/ArcGIS/DataReviewerCustomCheckSamples/tree/master/CustomCheckValidateDomainBasedAttributes)

##Technical Environment
The requirements for the machine on which you develop your ArcGIS Data Reviewer Custom Check are listed here.

####ArcGIS for Desktop or Server

* ArcGIS 10.4 for Desktop Basic, Standard, or Advanced or ArcGIS 10.4 for Server

####ArcGIS Data Reviewer for Desktop or Server

* ArcGIS 10.4 Data Reviewer for Desktop or Esri Production Mapping 10.4 or ArcGIS 10.4 Data Reviewer for Server

Note: If you currently do not have a licensed copy of ArcGIS Data Reviewer, you can request a free 60-day trial [here](http://www.esri.com/apps/products/offers/mapping/index.cfm?prd=reviewer).

####Supported .NET framework

* 4.5.2 
* 4.5.1 
* 4.5 

####Supported IDEs

* Visual Studio 2013 (Professional, Premium, Ultimate, and Community Editions) 

##Additional Resources
* Click [here](http://desktop.arcgis.com/en/arcmap/latest/extensions/data-reviewer/what-is-data-reviewer.htm) to access web help for ArcGIS 10.4 Data Reviewer.
* Click [here](http://desktop.arcgis.com/en/arcmap/latest/extensions/data-reviewer/pdf/Reviewer_check_poster.pdf) to view all ArcGIS Data Reviewer checks available.
* Click [here](http://www.esri.com/services/professional-services/request-services) to request help from Esri Professional Services.

##Configure Create Custom Check

You can use the Microsoft Visual Studio 2013 project files provided and in doing so you will learn how to develop a custom check for performing data validation according to the specific business needs of your organization. The steps below will guide you through developing and configuring custom checks.

##Implementation Steps

In this section, you will learn to develop the custom checks that are provided. This will familiarize you with the objects and interfaces used to implement a custom check and how to create validation results that will be used by the Data Reviewer framework.
     1. Create a new CSharp Class Library Project.
     2. Add a reference to ESRI.Reviewer.Public.Engine.Interop.dll. If you are building the custom check for ArcGIS Data Reviewer for Desktop use the 32 bit version of this dll which is located in the ArcGIS Data Reviewer for Desktop install directory. If you are building the custom check for ArcGIS Data Reviewer for Server use the 64 bit version of this dll which is located in the ArcGIS Data Reviewer for Server install directory.
     3. Create a class that implements appropriate interfaces.
     4. Make class COM visible and assign a GUID to the class.
     5. In project settings, check the “Register for COM interop” checkbox.
     6. In project settings, set target framework to .NET Framework 4.5.

####Interfaces

There are three interfaces to choose from when creating a custom check: IPLTSCNTSelectionSetValExtension, IPLTSCNTObjectClassValExtension and IPLTSCNTWorkspaceValExtension. Choose an interface to implement based on the type of data you want to validate. If validating a selected set of features in a map, then implement IPLTSCNTSelectionSetValExtension interface. If validating the entire contents of a feature class or table, then implement IPLTSCNTObjectClassValExtension. If validating multiple datasets within a workspace, then implement IPLTSCNTWorkspaceValExtension. Below is an explanation of the properties and functions on the interfaces.

|Property/Function Name       |Description               |
|:--------------------------- |:--------------|
|Bitmap Property              |Return a handle to a bitmap for the custom check. (Not in use)|
|Name Property                |Return a string representing the name of the custom check.|
|OnCreate Function            |Called when Data Reviewer instantiates the custom check. This method serves the purpose of initialization by hooking on to the ArcMap application. Any setup that needs to happen prior to the call of Execute can happen in OnCreate. If you are planning to run this check on ArcGIS Data Reviewer for Server, ensure you don’t use this.|
|Long Description Property    |Return a string representing a detailed description of the custom check.|
|Short Description Property   |Return a string representing a short description of the custom check.|
|Execute Function             |Main function where custom validation will be handled. Depending on the interface implemented either an ISelectionSet, IObjectClass, or IWorkspace will be passed in. A comma delimited string of arguments is also passed in.|

####Create Validation Results

When a violation of a custom validation rule is found, use the PLTSError and IPLTSError2 interface to report the problem. All the validation results should be added into a collection using the PLTSErrorCollection and IPLTSErrorCollection interface.

#####IPLTSError2
|Property Name      |Description        |
|:------------------|:------------------|
|ErrorGeometry      |Get/Set geometry of validation result.|
|ErrorID            |Get/Set unique identifier for validation result.|
|ErrorKind          |Get/Set type of validation result (value from enum pltsValErrorKind)|
|ErrorNumber        |Get/Set a numeric number representing the validation result (deprecated )|
|ShortDescription   |Get/Set string representing short description of validation result.|
|LongDescription    |Get/Set string representing detailed description of validation result.|
|OID                |Get/Set object id of row/feature associated with validation result.|
|QualifiedTableName |Get/Set qualified dataset name of row/feature associated with validation result.|

#####IPLTSErrorCollection
|Property Name      |Description        |
|:------------------|:------------------|
|Count              |Get count of items in collection.|
|AddError           |Add item to collection.|
|get_Errors         |Get an item in collection at specified index.|
|RemoveError        |Remove item in collection at specified index.|
|set_Errors         |Add item in collection at specified index.|

----------
     Note: ErrorNumber was used with Condition tables (CNTs), but CNTs are no longer supported in the product.

####Map to the Reviewer Table Fields
Values returned by certain properties on the objects and interfaces explained above are stored in specific fields within the Reviewer table. Below is a map of the properties and fields. IPLTSCNTSelectionSetValExtension is used when referring to an interface property but it also applies to IPLTSCNTObjectClassValExtension and IPLTSCNTWorkspaceValExtension as well.

|Property Name                                         |Reviewer Table Field        |
|:-----------------------------------------------------|:------------------|
|IPLTSCNTSelectionSetValExtension.Name                 |OriginCheck|
|IPLTSCNTSelectionSetValExtension.ShortDescription     |Notes (appended to value from Custom Check dialog)|
|IPLTSError2.LongDescription                           |ReviewStatus|
|IPLTSError2.ShortDescription                          |ReviewStatus (only if LongDescription is empty)|
|IPLTSError2.OID                                       |ObjectID|

####Deploy a Custom Check
To deploy a custom check, the assembly must be copied to each machine it will be executed on and registered using the regasm.exe tool (e.g. regasm.exe C:\MyCustomCode\MyCustomCheck.dll \codebase). The location the assembly is copied to should not matter as long as all dependent assemblies can be resolved at runtime (ArcGIS and Data Reviewer assemblies are stored in the GAC). If you are building the custom check for ArcGIS Data Reviewer for Desktop use the 32 bit version of regasm.exe and if you are building the custom check for ArcGIS Data Reviewer for Server use the 64 bit version of regasm.exe. 

[](Esri Tags: Data Reviewer Custom Check)
[](Esri Language: C#)
