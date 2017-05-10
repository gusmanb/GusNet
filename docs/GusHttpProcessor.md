# **GusHttpProcessor**

Definition: _public class GusHttpProcessor_

This class is the responsible of processing an HTTP/1.1|1.0 incomming request.

It holds the information about the request (GET variables, POST data, headers...) and calls to the HandleRequest function of the [GusHttpServer](GusHttpServer) when the processing is done.

## Public properties

_+public byte[]() OriginalRequest+_

Contains the original unprocessed request, as it was received.

+_public string PostDataFile+_

Contains the temporal file name of the POST data.

_+public bool IsSecure+_

Boolean which indicates if it's an HTTP or HTTPS request.

_+public Socket Socket+_

The original socket of the request.
If you want to control to a more low level the communications, then you can use this socket instead of the Input and Output streams.

_+public [GusHttpServer](GusHttpServer) Server+_

The server which created this GusHttpProcessor.

_+public string RemoteIP+_

The remote addres of the incomming connection.
_Warning!_ the RemoteIP address, may not be the one who originated the request, because a proxy can be in the middle.

_+public Stream InputStream+_

The stream used to read data from the request.

_+public [GusOutputStream](GusOutputStream) OutputStream+_

The stream used to write response data.

_+public string Method+_

The HTTP Method used in the request.

_+public string SourceUrl+_

Source URL of the request. It contains just the path of the URL, to obtain a full URI you must join the Host property to this path.

_+public Dictionary<string, string> RequestHeaders+_

Received headers with the request. All the headers are stored as is, even the cookies. If you need a server with specialized cookies support use or copy the functionality from the GusMainServer.

_+public int MaxPostSize+_

Maximum length of POST data, by default 2Mb, but can handle up to 2Gb files. It's set by the GusHttpServer who creates the processor.

## Public methods

_+public void WriteSuccess(string ContentType = "text/html", List<KeyValuePair<string, string>> Headers = null)+_

_Parameters_

* ContentType: Content type of the response.
* Headers: Additional headers to send with the response.

Writes an standard 200 response to the request attaching the desired headers and content type.
It uses a list of KeyValuePair<string,string> instead of a Dictionary<string,string> because the Set-Cookie header can appear more than once in a response, so it's not possible to use a dictionary.

_+public void WriteNotFound(Dictionary<string, string> Headers = null)+_

_Parameters_

* Headers: Dictionary of headers to send with the response

Writes an standard 404 response to the request attaching the desired headers passed through the Headers parameter.
If Headers is null no aditional header is sent. Not intended to send cookies as it uses a Dictionary to hold the headers.

_+public void WriteError(Dictionary<string, string> Headers = null)+_

_Parameters_

* Headers: Dictionary of headers to send with the response.

Writes an standard 500 response to the request attaching the desired headers passed through the Headers parameter.
If Headers is null no aditional header is sent. Not intended to send cookies as it uses a Dictionary to hold the headers.

_+public void WriteResponseType(string Code, Dictionary<string, string> Headers = null)+_

_Parameters_

* Code: the response code to send.
* Headers: Dictionary of headers to send with the response.

Writes a response with a custom code to the request attaching the desired headers passed through the Headers parameter.
If Headers is null no aditional header is sent. Not intended to send cookies as it uses a Dictionary to hold the headers.
Code cannot be null.