# MinecraftServerProxy
Run multiple Minecraft: Java Edition servers behind a single port. 

## About

I created this project so that I could run many Minecraft servers on one computer without needing to open a new port for each one. My end goal is to use this project to create a Minecraft server control panel that can be used to manage many Minecraft servers running in Docker (check out this awesome Docker image: https://github.com/itzg/docker-minecraft-server).

**MinecraftServerProxy** is inspired by other projects such as https://github.com/Ktlo/MCSHub, https://github.com/handtruth/mcshub, and https://github.com/janispritzkau/minecraft-reverse-proxy (among others). 

## Tools / Libraries

##### MinecraftServerProxy

* .NET Standard 2.0
* SimpleTCP (https://github.com/jchristn/SimpleTcp)

##### DemoApplication

* .NET Core 3.1

## Getting Started

##### Setting up

Start by creating a new `ProxyConfiguration`. A `ProxyConfiguration` contains the listening IP Address and listening Port of the proxy server in addition to the list of Minecraft servers behind the proxy.

```
using MinecraftServerProxy.Configuration;
...
var configuration = new ProxyConfiguration("127.0.0.1", 25565);
configuration.Servers["connor1.localhost"] = new ServerConfiguration("127.0.0.1", 25570);
configuration.Servers["connor2.localhost"] = new ServerConfiguration("127.0.0.1", 25571);
```

**MinecraftServerProxy** assumes that you are using it within an ASP.NET Core web application or some other .NET Core Generic Host application (see https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-3.1 and https://docs.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-3.1&tabs=visual-studio for more information).

Assuming you are using one of these types of projects, add the set up the `ProxyWorker` Background Service using the `AddMinecraftServerProxy` method. The `AddMinecraftServerProxy` method also takes your configuration object as a parameter.

```
services.AddMinecraftServerProxy(configuration);
```

By running the `AddMinecraftServerProxy` method, you register the `ProxyConfiguration` with dependency injection as a *Singleton* and add the `ProxyWorker` as a *Hosted Service*. With this setup, you can access and modify the singleton `ProxyConfiguration` from your ASP.NET Core Controllers or other dependency injection enabled services. New Minecraft servers can be added to the `ProxyConfiguration` at runtime.

The next time you start your application, you should see some log messages from the `ProxyWorker`. 

Please see the DemoApplication for a full code example. 

##### Testing the proxy

To test the proxy, we will use this Docker image: https://github.com/itzg/docker-minecraft-server. I assume you have Docker installed on your system. 

*Note:* When running a Minecraft server, you must accept the Minecraft EULA before the Minecraft server software will start. The following commands accept the Minecraft EULA automatically so that the containers will start properly, but please know that this does *not* mean I am accepting the Minecraft EULA on your behalf. Anyways, back to testing.

Start two Minecraft servers on different ports. The first is on port *25570* and the second is on port *25571*. Please see the `docker run` documentation for more details on the following commands (https://docs.docker.com/engine/reference/run/):

```
docker run -it -e EULA=TRUE -p 25570:25565 --name mc1 --rm itzg/minecraft-server
docker run -it -e EULA=TRUE -p 25571:25565 --name mc2 --rm itzg/minecraft-server
```

Start your Minecraft: Java Edition client and navigate to the Multiplayer menu.

In the Multiplayer menu, click *Add Server*. Enter **Minecraft Server 1** as the *Server Name* and **connor1.localhost** as the *Server Address*. Click *Done*. 

Click *Add Server* again. This time enter the details for the second server instance: **Minecraft Server 2** and **connor2.localhost**.

Once you've finished entering the details, you'll notice that neither server is responding. This is because you are trying to connect to **connor1.localhost** and **connor2.localhost** using the default port of *25565*. Your servers are running on ports **25570** and **25571** respectively. This is where the proxy comes in.

Start your **MinecraftServerProxy** enabled project and click *Refresh* in the Minecraft client. Your servers should now be acccessible. Joining *Minecraft Server 1* will take you to the first server while joining *Minecraft Server 2* will take you to the other server.

## How it works

When you reference **connor1.localhost** in the Multiplayer menu, the Minecraft client tries to connect to the Minecraft server running on **127.0.0.1:25565**. There isn't a Minecraft server running at this location. Rather, our proxy is. 

The proxy reads the incoming Minecraft server handshaking information and determines that the Minecraft client is trying to connect to **connor1.localhost**. The proxy searches the configured `ProxyConfiguration` for **connor1.localhost** and sees that **connor1.localhost** is associated with **127.0.0.1:25570**. The proxy sends the data from the Minecraft client to the Minecraft server running on **127.0.0.1:25570**. When the proxy receives the response from the server, it returns the response to the Minecraft client. 

## License Information

##### SimpleTCP

**SimpleTCP** is licensed under the *MIT* license by https://github.com/jchristn
