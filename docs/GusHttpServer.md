# **GusHttpServer**

Definifion: _+public abstract class GusHttpServer+_

The GusHttpServer plus the GusHttpProcessor constitute the core of the package. 

The GusHttpServer accepts incomming connections and creates new GusHttpProcessors for each one. Also includes an abstract function named HandleRequest which is called by each [GusHttpProcessor](GusHttpProcessor) created. In this way you can create your own server just inheriting the GusHttpServer and handling the request in the HandleRequest function.

The corresponding [GusHttpProcessor](GusHttpProcessor) is passed to each call to the HandleRequest, so the concrete request is identifyed.

The resulting HTTP server of the combination of GusHttpServer and GusHttpProcessor creates an HTTP server conformant to the HTTP/1.1 specification with the lack of keep alive support.

## Public properties

_+public int MaxPostSize+_

Maximum POST size allowed. By default is set to 2Mb, but the server can handle up to 2Gb post files.

## Public methods

_+public GusHttpServer(int Port, bool UseSsl, string CertificateFile)+_

_Parameters_

* Port: listening port of the instance.
* UseSsl: listen for HTTPS requests. Only one type of request can be served, HTTP or HTTPS.
* CertificateFile: File for a X509 certificate. If null and UseSsl is used, then a self-signed certificate is issued.

This is the main constructor of the GusHttpServer.

_+public bool Start()+_

_Parameters_

* None.

Starts accepting HTTP Requests.

_+public bool Stop()+_

_Parameters_

* None.

Stops serving new requests and stops the running ones.

