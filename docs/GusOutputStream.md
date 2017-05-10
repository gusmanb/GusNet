# **GusOutputStream**

Definifion: _+public class GusOutputStream : Stream+_

This is an encapsulated stream which allows to write binary and text data. It's used by the GusHttpProcessor as the OutputStream.

## Public methods

_All the common methods to a base Stream are ommited_

_+public void WriteText(string Text)+_
_+public void WriteText(string Text, Encoding Encode)+_

_Parameters_

* Text: the text to write to the Stream.
* Encode: the encoding used to get the binary data of the text to write.

This method writes an string to the stream using the choosen encoding. If the encodingless overload is used, then the Default encoding is used.

_+public void WriteLine(string Text)+_
_+public void WriteLine(string Text, Encoding Encode)+_

_Parameters_

* Text: the text to write to the Stream.
* Encode: the encoding used to get the binary data of the text to write.

This method writes an string and a final _"\r\n"_ to the stream using the choosen encoding. If the encodingless overload is used, then the Default encoding is used.
