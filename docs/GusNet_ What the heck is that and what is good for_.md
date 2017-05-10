# GusNet? What the heck is that and what is good for? 

So you are here, then you must be a programmer looking for info or you are bored and don't know what to do, or may be both _(nothing more dangerous than a bored programmer at leisure!)_.

Anyway, GusNet is a suite of four different libraries with a bunch of classes which allows to integrate Http services with a minimum coding.  It can be used to serve HTTP requests, has it's own scripting language (just c# with some handful shortcuts), can be used to create a proxy server or a bridge from HTTP to SOCKS5 proxy and finally can be used to control The Onion Router. 

It has been coded in four days. Four of those programming-fever days. Four days of nearly no sleeping. Four days struggling my brains. But four days with a very satisfactory ending.

The origin of the project became from two sources, first the need of a lightweight but fully featured Http server for my own projects, and the second, my curiosity about the TOR network.

The features of the package can be splitted in four different parts

_+The Http server+_ using GusScript pages allows to serve nearly a thousand pages per second with not much CPU usage, so it will fulfill the needs of any programmer in the need of serving pages in it's own program, per example to use an HTML interface in a desktop program.

_+The proxy server+_ is an anonymous server with a very high throughput. With a 100/10 internet connection the bandwith loss is less than a 0.5%, and handles very well more than a thousand concurrent requests.

_+The SOCKS5 bridge+_ allows to easily add support for those proxy type in programs or controls which allow only the use of HTTP proxies, per example a program using HttpWebRequests.

_+The TOR controller+_ makes to establish and control a link to the TOR network very easy, as to write a line of code.

The four libraries are:


# [GusServer](GusServer), the GusNet's main core. It contains a bare-metal Http server ([GusHttpServer](GusHttpServer)) with HTTP 1.1 compliance (nearly), chunked encoding support, SSL, self-signed certificates and so on, intended to be inherited and create your own server, and a more developed server ([GusMainServer](GusMainServer)(GusMainServer)) with integrated system to add your own code throug [GusServerPath](GusServerPath)s. It also contains a POST request parser ([GusPostProcessor](GusPostProcessor)) which can be used alone to process received requests and is also used in [GusMainServer](GusMainServer)(GusMainServer) to process the POST data.
# [GusScript](GusScript)(GusScript), a set of classes which include a c# variation for creating HTML pages used in [GusScriptPath](GusScriptPath), an implementation of [GusServerPath](GusServerPath) which uses compiled assemblies as pages (similar to how ASP .Net works) for efficiency. Also includes a path ([GusScriptRemotePath](GusScriptRemotePath)) to execute remote [GusScript](GusScript)(GusScript) pages.
# [GusBridge](GusBridge), with this library you can integrate an Http proxy ([GusProxyPath](GusProxyPath)) or a Http to SOCKS5 bridge([GusBridgePath](GusBridgePath)) using [GusMainServer](GusMainServer) as the base core for the services. Also contains a SOCKS5 proxy client implementation encapsulated into a socket, so you can use it transparently in your code as a normal socket.
# [GusTor](GusTor), which holds a TOR controller ([GusTorController](GusTorController))

So, if you got till here that means you are interested in GusNet :D.

If you use GusNet in a project you can make me happy leaving me a message in the [discussion](https://gusnet.codeplex.com/discussions) section with a link to see the program, and if you give me credits for my work I will add you to the [List of projects using GusNet](List-of-projects-using-GusNet).

_Happy Coding!_
