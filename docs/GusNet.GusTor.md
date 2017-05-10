# **GusNet.GusTor**

So, you are interested in using the TOR network in your applications? Well, then with GusTor it's really easy.

If you don't know what TOR is, it's the acronim for The Onion Router, [chek it's homepage](https://www.torproject.org/) to get more info.

In this namespace you will find just a class, the GusTorController.

It's a basic Tor controller, it can start, stop and take ownership of a tor instance, can authenticate, ask for a new identity and create hidden services.

To start, you must first create an instance and start() it. 

The parameters to start are:

* _+TorPort+_: the port in which TOR will listen for SOCKS5 connections.
* _+ControlPort+_: the port in which TOR will listen for control connections.
* _+HiddenServices+_: a list of hidden services created at startup. The services must be in the format "serveraddress:serverport serviceport@servicepath".

Once started, you must first Authenticate() and then you are ready to issue more commands. If you don't need to control how TOR behaves, just start it and use it.

So, now you have TOR running, how can you use it in your program?

Well, you have all the tools in GusNet. First create a [GusMainServer](GusMainServer) listening ond some port. Add a [GusBridge](GusBridge)Path using as SOCKS5 proxy 127.0.0.1:(TOR SOCKS5 port).

Now you are ready to use it in your programs or your prefered application, just use 127.0.0.1:(your server port) as HTTP proxy and you are done. 

Also, you want a hidden service? No problem!! Create a [GusMainServer](GusMainServer), add your prefered [GusServerPath](GusServerPath) implementation and add a hidden service using RegisterHiddenService (or pass it through the GusTorController constructor).

As you can see, using the TOR network has never been so easy. With very little code you can add really anonymous connections through your programs.

Next week I promise I will add full documentation, for now this is enough to start playing with it.

Have fun!
