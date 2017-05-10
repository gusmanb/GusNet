# **GusServerRequest**

Definition: _+public class GusServerRequest+_

This class is passed from the [GusMainServer](GusMainServer) which received a request to the corresponding [GusPath](GusPath).

It holds all the information abut the query (Request headers, GET and POST variables, POST files, etc) and the holders for creating a response (like Response headers, cookies, etc).

It also has references to the processor and server who received the request.

## Public properties

_+public [GusHttpProcessor](GusHttpProcessor) Processor+_

The processor used to process this request.

_+public [GusHttpServer](GusHttpServer) Server+_

The server who received this request.

_+public Stream RequestStream+_

The stream used to read from the request.

_+public [GusOutputStream](GusOutputStream) ResponseStream+_

The stream used to write a response to the request. Anything the path must send as a response must be written here. Remember that this stream is not buffered, so when you write, a synchronous write operation is done. If you want to buffer the response, the you can encapsulate this stream inside a BufferedStream.

_+public Dictionary<string, string> RequestHeaders+_

Collection of received headers in this request.

_+public Dictionary<string, string> ResponseHeaders+_

Holder for haders to be sent with the response. When a WriteXXXResponse is executed, the headers in this dictionary are sent with the response.

_+public Dictionary<string, string> GetVariables+_

Collection of variables received through the query string of this request.

_+public Dictionary<string, string> PostVariables+_ Collection of variables received through the POST data received with this request.

_+public Dictionary<string, [GusPostFile](GusPostFile)> PostFiles+_

Collection of files found in the POST data of this request.

_+public string SourceUrl+_

Source URL with the query string. It only contains the path, doesn't contains the protocol nor the host.

_+public string ProtocolVersion+_

This contains the protocol version string (HTTP/1.0 or HTTP/1.1)

_+public bool IsSsl+_

This indicates if the request was done through an SSL connection.

_+public string Method+_

Method of the request.

_+public string Path+_

Path for the request, without the query string.

_+public string QueryString+_

The query string sent with this request.

_+public string RemoteIP+_

Remote IP of the incomming connection.

_+public Dictionary<string, string> Cookies+_

This is the list of received cookies with the request. Also, any cookie added to this collection will be sent when a WriteXXXResponse is executed.

## Public methods

_+public void WriteOkResponse()+_

_Parameters_

* None.

This method sends the begining of a Response with a 200 code, headers and cookies.

 _**Warning!**_ if you will not write manually the Response header to the response stream and want to use the WriteXXXResponse methods, the selected method must be called BEFORE any data is sent through the response stream.

_+public void WriteNotFoundResponse()+_

_Parameters_

* None.

This method sends the begining of a Response with a 404 code, without headers nor cookies.

 _**Warning!**_ if you will not write manually the Response header to the response stream and want to use the WriteXXXResponse methods, the selected method must be called BEFORE any data is sent through the response stream.

_+public void WriteCustomResponse(string Code)+_

_Parameters_

* Code: the code of the response.

This method sends the begining of a Response with a custom code, without headers nor cookies.

 _**Warning!**_ if you will not write manually the Response header to the response stream and want to use the WriteXXXResponse methods, the selected method must be called BEFORE any data is sent through the response stream.
