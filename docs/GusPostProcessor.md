# **GusPostProcessor**

Definition: _+public class GusPostProcessor+_

This class process the Multipart data of a POST request. If the POST request is not multipart, (beside it's form encoded or not), the the processor will parse nothing because the data is just a GET string which can be parsed with HttpUtility.ParseQueryString.

_**Warning!** HttpUtility.ParseQueryStrings has some bugs which prevents to parse some queries. Instead of it you can use [GusMainServer](GusMainServer).ParseQueryString which works with any request string._

## Public properties

_+public bool Success+_

Indicates if the processing of the POST data was successful.

_+public Dictionary<string, string> Variables+_

Holds the variables found in the POST data.

_+public Dictionary<string, [GusPostFile](GusPostFile)> Files+_

Holds the Files found in the POST data. All the files are stored in temporal files, remember to thelete them if you use the processor alone.

## Public methods

_+public GusPostProcessor(Stream Stream, string ContentType)+_
_+public GusPostProcessor(Stream Stream, Encoding Encoding, string ContentType)+_

_Parameters_

* Stream: the stream from where to read the POST data.
* Encoding: encoding used to process the raw strings in the file. If not passed, the the Default encoding is used.
* ContentType: the Content-Type header received in the request. Mandatory to pass to the function as it contains the delimiter of the POST data blocks.

This is the default constructor. When the constructor is executed, automatically the processing begins.