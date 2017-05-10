# **Package Structuration**

The package is structurated in four different libraries:


# _+GusServer.dll+_, includes the main core GusNet's classes. All classes are under the namespace GusNet.GusServer
# _+GusScript.dll+_, includes the classes to serve pages using the GusScript language. All classes are under the namespace GusNet.GusScript
# _+GusBridge.dll+_, include the classes to act as an HTTP 1.1 proxy or an HTTP to SOCKS5 bridge. All classes are under the namespace GusNet.GusBridge
# _+GusTor.dll+_, includes a class to control The Onion Router. All classes are under the namespace GusNet.GusTor

As you can see, all the classes are grouped on the GusNet namespace, and each functionality has its own namespace.

I decided to split the project in different libraries to add the minimum weight to my programs, but if you prefer to have only one dll, or even none, you can just take the code you need and add it to your program, the classes names are always prefixed with "Gus", enough to avoid conflicts with other classes.

