# **GusNet.GusServer Namespace**

This namespace contains the main core of the GusNet package. It contains the Http server classes which are:

_+[GusHttpServer](GusHttpServer)+_: a bare-metal but fully featured HTTP server. It cames in the form of an abstract class so you can implement the underliying logic to the Request processing.

_+[GusHttpProcessor](GusHttpProcessor)+_: the class responsible of processing an Http request. It holds the original request socket, streams, and a bunch of info about the original request.

_+[GusOutputStream](GusOutputStream)+_: just an encapsulated Stream with functions to write text and binary data.

_+[GusCertificate](GusCertificate)+_: a very little class for creating self-signed certificates. Very useful.

_+[GusMainServer](GusMainServer)+_: a more elaborated server, including POST data parsing. It works with the concept of paths, each path in the server is a class, so you can serve different codes with no modifications to the server.

_+[GusPostProcessor](GusPostProcessor)+_: this class parses the multipart POST requests. It splits the files and variables into file buffers and string dictionaries to easily manipulate the request data. It can be useful if you need to parse a multipart POST request in your program but don't want to use the GusHttpServer nor the GusMainServer (but I can't think a reason why you would not want to use them :D).

_+[GusPostFile](GusPostFile)+_: a data class which holds information about a POST file received in a request.

_+[GusServerRequest](GusServerRequest)+_: a data class which holds all the information parsed from a request and classes used to read/write a response. Also defines shortcuts to functions to write responses.

_+[GusServerPath](GusServerPath)+_: the base class used to create a path for the GusMainServer.

As a continuation, I recommend to read the full description of all the classes, but if you are in a hurry here is a little table about what to read according to what you whant to do:

* Process HTTP 1.1 requests with full control: Create a [GusHttpServer](GusHttpServer) implementation
* Serve HTML pages and files: Create a [GusPath](GusPath) implementation and use the [GusMainServer](GusMainServer)
* Serve custom content to HTTP requests: Create a [GusPath](GusPath) implementation and use the [GusMainServer](GusMainServer)
* Generate an X509 certificate: Call to CreateSelfSignedCertificate in [GusCertificate](GusCertificate)
