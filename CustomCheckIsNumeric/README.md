#Data Reviewer Custom Check Samples
##IsNumeric

A custom check used to identify where a character value - rather than a numeric value - may have inadvertently been entered into a field. For example, if you have numeric values containing leading zeros, this would cause the value to be stored in a text field. 

```
Language:               C#
Subject:                Data Reviewer Custom Step Sample
Contributor:            ArcGIS Data Reviewer Team <DataReviewer_Team@esri.com>
Organization:           Esri, http://www.esri.com
Date:                   02/09/2016
ArcGIS Data Reviewer:   10.4
Visual Studio:          2013
```
#How to use the sample
In this section you will configure and run the IsNumeric custom check by using the _Custom Check_ option on the Data Reviewer toolbar. You can use any dataset in which you need to validate the contents of a field to find values that are not all numeric.

1. Open Visual Studio and compile the CustomCheckIsNumeric solution.
2. Run the command prompt as an administrator and navigate to C:\\Windows\Microsoft.NET\Framework\v4.030319 folder.
3. Enter the command RegAsm.exe <path to your .dll> /codebase.
4. After the .dll is registered, open ArcMap.
5. Check the _Data Reviewer_ extension check box from __Customize > Extensions__.
6. Add the __Data Reviewer__ toolbar from __Customize > Toolbars__.
7. Click the __Select Data Check__ drop-down on the __Data Reviewer__ toolbar, expand __Advanced Check__ and click __Custom Check__.
8. Enter IsNumeric Check as the check name in the __Check Title__ text box.
9. Click the __Type of Check__ drop-down and choose the _Input Feature_ class which contains the field which needs to be validated.
10. Enter the GUID for the DLL associated with the custom check in the __GUID__ text box.

    ```Note: The GUID of the CSharp IsNumeric custom check is {34af6bc6-fd0a-4bdc-9763-29c2e04d398a}.```

11. Type an argument in the __Argument__ cell. 

    ```Note: For IsNumeric, enter the field name that needs to be validated for numeric values.```
12. If necessary, enter descriptive text for the check results in the Notes text box in the Reviewer Remarks area.
13. If necessary, click the __Severity__ drop-down and choose a value that indicates the priority of the checks results in the Reviewer Remarks area.
14. If necessary, enter a description of what the check is validating in the __Description of Custom Check__ text box.

   ```Note: The _Description of Custom Check_ is used to describe the custom check. This will be preserved if this check is   configured inside a batch job and will not be used while writing results to Data Reviewer.```
   
   ![UI](../Screenshots/ValidateDomainBasedAttributes.png) 
   
15. Click __OK__.
16. Click the __Run__ button on the __Data Reviewer__ toolbar.
