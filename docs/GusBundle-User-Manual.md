# **GusBundle User Manual**

So, here we are, you want to use the TOR network, and GusBundle is what you need.

First of all, download the package from the [downloads](https://gusnet.codeplex.com/releases/view/112791) section.

Install it and launch it. It will pop up in a few seconds and will hide. Don't worry, it's in the system tray, double click it and it will show itself.

Ok, if everything went OK you are ready to use the TOR network!

To use it through any program, configure it to use an HTTP proxy at 127.0.0.1:8191.

To test the network status I recommend to access to a well-known page like google, bing or yahoo.

Now that you know it works, let's see the interface and what does who.

|| GusNet's GusBundle Interface ||

This is the GusBundle's main interface. 
You can see in order: your hidden service address, list of configured paths on your hidden service, tools to control the paths, a list of buttons to control TOR and some start up options.

## Your hidden service

A hidden service is created automatically when you start GusNet's GusBundle. You can see it's address in the top box at the interface.

To start serving pages you must add a path. Each path is composed by a virtual path and a physical path. The virtual path is the one at your hidden service (per example "/forum") and the physical path is where the pages to serve are stored.

With GusNet's GusBundle you can serve [GusScript](GusScript.md) pages. If you only need to serve HTML, then you can rename your files to .gsc and you are set. If you need more complex behaviors and know how to program, the you can use [GusScript](GusScript.md), which is a variant of C# with some PHP touches.

Remember that to execute a page you must add it to the link as "file=yourgsc", so, if you have configured the path "/forum" and want to execute the page "createpost.gsc", then the final link should point to "/forum?file=createpost". Note that the extension is not needed.

If no file is passed to the request, the server will seek for a default "index.gsc".

You can access also your server pages through 127.0.0.1:8190, it's very useful to test your pages without going through the TOR network.

## Controlling TOR

You can start and stop TOR with the first two buttons in the TOR controller panel, and the third one will ask for a new identity in the TOR network.

## General options

Finally, you can configure the program to autoboot with your computer and to autostart TOR when booted.

## Final words

As you can see, GusNet's GusBundle will allow you to be online in the tor network in a few seconds, install it, add a path, and voila! you are ready! you can navigate through TOR and have your own hidden service.

If you use, love or hate GusNet's GusBundle, don't hesitate to leave a comment in the [discussion](https://gusnet.codeplex.com/discussions) section.

Have fun and be no evil!

