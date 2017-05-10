# **GusNet.GusScript Namespace**

This namespace contains the implementation of a nifty scripting language called GusScript and the path for using it with a [GusMainServer](GusMainServer)

As a programmer, I like to use a tool which adapts to a concrete work in the best way. So for programming a Web Application, the ASP .net model is what I prefer, but for creating web pages for Internet presence, which will contain very little code (a form to send data, and not much else), I prefer the PHP way.

It can be achieved also with ASP .net, but takes more code to produce the same results, as ASP .net is prepared for bigger projects. This can be just my subjective point of view, but that's how I feel it.

So, after doing the [GusMainServer](GusMainServer) I decided to create this little script language.

It's far from perfect (there are some known cases where the parser fail), but it's very easy and fast to use.

It's based on C# (it compiles as C#), so you can use anything you can do in C#, but it has some keywords (the more often used) which make the developing more agile than writting pure C# code.

Here is an example of a GusScript page

{code:c#}
<&@ import namespace="System.Threading" &> //Importing a namespace
<& 

    string oldCookie = cookie("GSCSSESID"); //Retrieve old cookie
    var str = Guid.NewGuid().ToString();
    cookie("GSCSSESID", str); //Store a new value for the cookie
    cookie("gimme", "cookies"); //Create a new cookie
    head("Expires", DateTime.Now.ToString()); //Adding a header to the response

    ok(); //Issue a 200 OK response

&>
<html>
    <body>
    <& 

	if(oldCookie != null)
        {
        
            &>Had the GSCSSESID cookie with a value of <&
            echo(oldCookie);

        }

        byte[]()() b = new byte[]()(){ 49, 50, 51, 52 };

        string cadena ="Hello!";

        forvar(cha, cadena) //Creates a foreach(var cha in cadena)
            echo(cha.ToString() + "<br />");

        forbuc(0,256) //Creates a for(int buc = 0, buc < 256; buc++)
            echo("Nothing! <br />");

        forbuc(1, 10)
        {
            flush(); //Flushes the response to the stream to ensure it's sent
            Thread.Sleep(1000);
            &>Loop value: <& echo(buc.ToString()); &><br /><&
        }
    &>
    <br />
	The time is: <& echo(DateTime.Now.ToString()); &>
    </body>
</html>
{code:c#}


As you can see it's like a mixture of both, PHP and C#, with the advantage over PHP that it executes compiled code and not parsed code.

Yes, the resulting C# code is compiled into an assembly the first time a page is executed or when it's modified, so the first run can take a bit longer (one second? XD) to execute, but the next ones will run at the light's speed ;)


As I have for now very little time and want to publish today this project, I'm just writting the basic instructions of use, and the next week I will add the full documentation.

## **GusScript basics**

* All the GusScript files must have the ".gsc" extension
* The GusScriptPath is tied to a virtual path in the server, so the file names are passed in a get variable named file (per example, http://somedomain.com/script?file=index)
* You open and close a piece of GusScript code with <& and &> respectively
* You can use loops to write html by opening and closing code, but they must have opening and closing brakets.
* You can debug the resulting code adding a breakpoint with bp() in your code if the debug is enabled in the path. _**Warning!**_ only use breakpoints if you are executning the GusMainServer from Visual Studio or a fatal exception will be generated!
* When you create a page, remember that you have access to the [GusServerRequest](GusServerRequest) object used by the GusScriptPath (the [GusPath](GusPath)'s implementation).
* To import an assembly add to the top of your page a <&@ assembly name="(assemblyname)" &> directive
* To import a namespace add to the top of your page a <&@ import namespace="(assemblyname)" &>
* Some assemblies are always imported and only needed to add the namespace, the complete list is:
	* System
	* System.Reflection
	* System.IO
	* GusScript
	* GusServer
* Also, the GusNet.GusServer and GusNet.GusScript namespaces are imported

## **GusScript keywords**

_+**echo(x)**+_: writes an string to the Response stream.
_+**binecho(x)**+_: writes binary data to the response stream. The input must be a byte[]() array.
_+**head(x)**+_: retrieves a header from the response, returns null if not found.
_+**head(x, y)**+_: adds a header to the response.
_+**cookie(x)**+_: retrieves the value of a cookie, returns null if not found.
_+**cookie(x,y)**+_: adds a cookie to the response.
_+**forvar(x,y)**+_: creates a foreach(var x in y) loop.
_+**forbuc(x,y)**+_: creates a for(int buc = x, buc < y; buc++) loop.
_+**flush()**+_: flushes the Response stream.
_+**bp()**+_: adds a breakpoint to the current line.
_+**ok()**+_: sends a 200 OK response with the headers and cookies stored previously.
_+**postfile(x)**+_: retrieves a [GusPostFile](GusPostFile) from the post files collection. Returns null if not found.
_+**postvar(x)**+_ retrieves a POST variable. Returns null if not found.
_+**getvar(x)**+_: retrieves a GET variable. Returns null if not found.

As you can see, nothing really special is done, BUT, it makes to write a little web very very easy and fast. 

## **Using GusScript**

So if you still are here, you will want to try or use it. It's very easy.

First, you must have an [GusMainServer](GusMainServer) ready to accept requests, to know how it works read the help at [GusNet.GusServer](GusNet.GusServer).

Create a new GusScriptPath indicating the virtual path (or Webpath :P), the physical path where your GusScript files are stored, the default file name to execute if none is sent, and if you want to debug your pages set Debug to true.

Add the path to the [GusMainServer](GusMainServer).

You are ready!

Whenever any request to the virtual path matches the virtual path of the GusScriptPath, it will seek for the file specified in the "file" variable. If it was found, or none where specified, then the corresponding page is executed. If no matching file is found, then a 404 response is issued.

As I stated, nex week full documentation will be uploaded, but for now, with this you can start playing with GusScript.

Also, a GusScriptRemotePath is there, it allows the remote execution of code, take a look if you are curious.

IF you use GusScript, I will be very happy if you leave a message in the [discussion](https://gusnet.codeplex.com/discussions) section.

**Update**

Added new tag to add shared code. If you want to add fields, functions, or classes, surround the code with <&& &&> and it will be shared. **_Warning_** in shared code special tags like echo() or ok() will not work, it must be pure C#

Nice coding!

