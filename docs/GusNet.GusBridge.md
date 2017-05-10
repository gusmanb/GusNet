# **GusNet.GusBridge**

This namespace contains two classes to create an HTTP/1.1 proxy (fully compliant (ok, nearly fully compliant, only needed to implement trails for chunked encoding), including persistent connections) and an HTTP to Socks5 proxy bridge using as core a [GusMainServer](GusMainServer).

The first class is named GusProxyPath, and has no parameters nor properties. Just instantiate it and add to a ready [GusMainServer](GusMainServer) and you are done. _**The proxy path is a catch-all, so be ware that mixing this path with others can lead to errors**_.

The second class is named GusBridgePath which inherits GusProxyPath and has some parameters for it's constructor:

* _+Socks5Address+_: the SOCKS5 proxy IP address.
* _+Port+_: the SOCKS5 server port.
* _+User+_: the SOCKS5 user name, can be null if the server allows unauthenticated connections.
* _+Password+_: the SOCKS5 user password, can be null if the server allows unauthenticated connections.

And that's all, once instantiated and added to the paths of your [GusMainServer](GusMainServer) you are ready to go.

One extra utility of those paths are that they have a debug mode (enabled through a define in GusProxypath) which will debug information abut requests, so you can analize HTTP traffic using the proxy or the bridge.

In the namespace also is a SOCKS5 socket which you can use to transparently replace a socket and have the capability of connecting through a SOCKS5 proxy.

Next week I will add full documentation, but not much has been left.

Happy Coding!
