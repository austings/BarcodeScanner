# BarcodeScanner
ASP.net razorpages application to count inventory. Can use with or without a barcode scanner. Enter a UPC and if it is found in the master list of UPCs then it will create a csv on the host server with the count indicated. If its not found you can enter the item by searching SKU or title. You can undo bad entries. Once complete you can click 'Complete and Compile' to collect all the items you scanned into a single document. 

![alt tag](https://github.com/austings/BarcodeScanner/blob/master/preview.png) 

When I was working in the library in college, I hated scanning books and waiting a long time for a response from the server. For my approach with this tool, I wanted to be able to scan several bar codes very quickly. Saving the scanned items to the file on the complete button accomplishes this.  This way the worker can scan each item in rapid succession if need be. 
