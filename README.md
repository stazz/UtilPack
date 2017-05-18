# UtilPack
Home of UtilPack - library with various useful and generic stuff for .NET.

## Portability
UtilPack is designed to be extremely portable.
Currently, it is target .NET 4 and .NET Standard 1.0.
The .NET 4 target is lacking any asynchronous code though.

## TODO
One of the most important thing to be done is adding proper unit tests for all the code.
Due to a bit too hasty development model, and the ancientness of the project (back when making unit tests in VS wasn't as simple as it is now, in 2017), there are no unit tests that would test just the code of UtilPack.
Thus the tests are something that need to be added very soon.


# Core - or just UtilPack
The UtilPack project is the core of other UtilPack-based projects residing in this repository.
It provides some of the most commonly used utilities, and also has some IL code to enable things are not possible with just C# code.

The UtilPack Core is located at http://www.nuget.org/packages/UtilPack

# UtilPack.JSON
This project uses StreamStreamReaderWithResizableBuffer, IEncodingInfo, and StreamWriterWithResizableBuffer types located in UtilPack in order to provide fully asynchronous functionality to serialize and deserialize JSON objects (the JToken and its derivatives in Newtonsoft.JSON package).
This functionality is available as extension methods to the types mentioned above just by including UtilPack.JSON as a reference to your project.

The UtilPack.JSON is located at http://www.nuget.org/packages/UtilPack.JSON