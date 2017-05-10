# **GusServerPath**

Definition: _+public abstract class GusServerPath+_

This class is the base class used to create paths for the [GusMainServer](GusMainServer).

## Public properties

_+public abstract string Path+_

This property must return the virtual path in the server of this [GusPath](GusPath). You can return "*" for a catch-all path.

A catch-all path can convive with other paths and will only catch unhandled requests.

## Public methods

_+public abstract void ProcessRequest([GusServerRequest](GusServerRequest) Request)+_

This function is called from a GusMainServer to start the processing of a request. All the request go through only one instance of the class, so be ware with fields and properties in the class body.

