# **GusMainServer**

Definition: _+public class GusMainServer : [GusHttpServer](GusHttpServer)+_

This is an implementation of [GusHttpServer](GusHttpServer) which does a deeper process of the request and parses the cookies and POST data received so it can be consumed easily by code.

The server works with the concepts of paths. Each GusPath executed by the server is tied to a virtual path in the server. In this way you can create different classes for each path and have a mantainable code structure.

## Public methods

_+public GusMainServer(int Port, bool UseSsl, string CertificateFile = null)+_

_Parameters_

* Port: listening port of the instance.
* UseSsl: listen for HTTPS requests. Only one type of request can be served, HTTP or HTTPS.
* CertificateFile: File for a X509 certificate. If null and UseSsl is used, then a self-signed certificate is issued.

This is the main constructor of the class.

_+public void AddPath([GusServerPath](GusServerPath) Path)+_

_Parameters_

* Path: the new path to add to the server.

This function adds a new path to this server and boots it up immediately.

_+public void RemovePath([GusServerPath](GusServerPath) Path)+_

_Parameters_

* Path: the ath to remove from the server.

This function removes a path from the server. **If the path is processing a request it's not cancelled**.

_+public static Dictionary<string, string> ParseQueryString(string Query)+_

_Parameters_

* Query: the query string to parse.

This function takes a GET string and splits it into a dictionary of key/value pair.
