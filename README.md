###Supported attributes:
 
* **[RequireToStringAttribute]** Put the attribute ahead of a class/struct, then the class/struct will have an overwritten 'ToString()' method, the 'ToString()' method returns the class/struct's fields' value, like 'TestClass:[n=0]'
* **[InsertLogAttribute]** Put the attribute ahead of a method, an entrance log and an exit log will be inserted, like 'Enter TestClass.Test: [arg=0]' and 'Exit TestClass.Test: 0'