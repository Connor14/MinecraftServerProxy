# MinecraftServerProxy
Run multiple Minecraft: Java Edition servers behind a single port. 

## About

I created this project so I could run many Minecraft servers on one computer without needing to open a new port for each one. My end goal is to use this project to create a Minecraft server control panel that can be used to manage many Minecraft servers running in Docker (check out this awesome Docker image: https://github.com/itzg/docker-minecraft-server).

**MinecraftServerProxy** is inspired by other projects such as https://github.com/Ktlo/MCSHub, https://github.com/handtruth/mcshub, and https://github.com/janispritzkau/minecraft-reverse-proxy (among others). 

## Tools / Libraries

### MinecraftServerProxy

* .NET 6.0
* Microsoft.Extensions.Hosting - *MIT*
* System.IO.Pipelines - *MIT*
* Pipelines.Sockets.Unofficial (https://github.com/mgravell/Pipelines.Sockets.Unofficial) - *MIT*

### MinecraftServerProxyStandalone

* .NET 6.0
* Serilog (https://github.com/serilog/serilog) - *Apache-2.0*
* Serilog.Extensions.Hosting (https://github.com/serilog/serilog-extensions-hosting) - *Apache-2.0*
* Serilog.Settings.Configuration (https://github.com/serilog/serilog-settings-configuration) - *Apache-2.0*
* Serilog.Sinks.Console (https://github.com/serilog/serilog-sinks-console) - *Apache-2.0*
* Serilog.Sinks.File (https://github.com/serilog/serilog-sinks-file) - *Apache-2.0*

## Getting Started

There are multiple ways to use **MinecraftServerProxy**. 
* Minecraft server administrators may want to use the standalone application for their networks. 
    * See [Setting up the standalone application](#Setting-up-the-standalone-application) and [Testing the proxy](#Testing-the-proxy) for more information
* Developers may want to integrate the **MinecraftServerProxy** library into their existing applications.
    * See [Integrating the library](#Integrating-the-library) for more information

### Setting up the standalone application

This section assumes you've already downloaded a **MinecraftServerProxyStandalone** release that is appropriate for your operating system.

1. Download a **MinecraftServerProxyStandalone** release and default `appsettings.json` configuration file
    * See https://github.com/Connor14/MinecraftServerProxy/releases for pre-built downloads
2. Copy the release files and `appsettings.json` to the same directory
    * On Linux, copy the downloaded executable to the same directory as `appsettings.json`
    * On Windows, extract the downloaded release to the same directory as `appsettings.json`
3. If desired, modify `appsettings.json` to configure your proxy's settings
    * [Testing the proxy](#Testing-the-proxy) will assume you are using the default `appsettings.json`
4. Run the `MinecraftServerProxyStandalone` executable

You can also run the application as a service. Here are the additional steps:

#### Linux Systemd

For more information, see the Microsoft Blog Post: https://devblogs.microsoft.com/dotnet/net-core-and-systemd/

1. Copy the `MinecraftServerProxyStandalone.service` file to `/etc/systemd/system`
2. Run `sudo systemctl daemon-reload`
3. Run `sudo systemctl status MinecraftServerProxyStandalone` and check output
4. Run `sudo systemctl start MinecraftServerProxyStandalone.service` to start the service
5. Run `sudo systemctl status MinecraftServerProxyStandalone` again and check output
6. Run `sudo systemctl enable MinecraftServerProxyStandalone.service` to start when the machine starts

#### Windows Service

TODO

### Integrating the library

Please see [MinecraftServerProxyStandalone](https://github.com/Connor14/MinecraftServerProxy/tree/master/MinecraftServerProxyStandalone) for a full code example. 

More information to come!

### Testing the proxy

To test the proxy, we will use this Docker image: https://github.com/itzg/docker-minecraft-server. I assume you have Docker installed on your system. 

*Note: When running a Minecraft server, you must accept the Minecraft EULA before the Minecraft server software will start. The following commands accept the Minecraft EULA automatically so that the containers will start properly, but please know that this does **not** mean I am accepting the Minecraft EULA on your behalf. Anyways, back to testing.*

1. Start two Minecraft servers on different ports. 
    * The first is on port *25570* 
    * The second is on port *25571*. 
    
Please see the `docker run` documentation for more details on the following commands (https://docs.docker.com/engine/reference/run/):

```
# Run in a command line / terminal instance:
docker run -it -e EULA=TRUE -p 25570:25565 --name mc1 --rm itzg/minecraft-server

# Run in another command line / terminal instance:
docker run -it -e EULA=TRUE -p 25571:25565 --name mc2 --rm itzg/minecraft-server
```

2. Start your Minecraft: Java Edition client and navigate to the Multiplayer menu.

3. Navigate to the Multiplayer menu 
    * Click *Add Server*
    * Enter **Minecraft Server 1** as the *Server Name* and **connor1.localhost** as the *Server Address*
    * Click *Done*
    * Click *Add Server* again
    * This time enter the details for the second server instance: **Minecraft Server 2** and **connor2.localhost**
    * Click *Done*

4. If you haven't started a **MinecraftServerProxyStandalone** instance yet, you will notice that neither server is responding
    * This is because you are trying to connect to **connor1.localhost** and **connor2.localhost** using the default port of **25565**. 
    * Your servers are running on ports **25570** and **25571** respectively. 
    * This is where the proxy comes in.

5. Start **MinecraftServerProxyStandalone** and click *Refresh* in the Minecraft client. Your servers should now be acccessible. 

6. Joining *Minecraft Server 1* will take you to the first server while joining *Minecraft Server 2* will take you to the other server.

## How it works

When you join **connor1.localhost** in the Multiplayer menu, the Minecraft client tries to connect to the Minecraft server running on **connor1.localhost:25565**. There isn't a Minecraft server running at this location. The proxy is. 

The proxy reads the incoming Minecraft server handshaking information and determines that the Minecraft client is trying to connect to **connor1.localhost**. The proxy searches the `ProxyConfiguration` in `appsettings.json` for **connor1.localhost** and sees that **connor1.localhost** is associated with **127.0.0.1:25570**. The proxy sends the data from the Minecraft client to the Minecraft server running on **127.0.0.1:25570**. When the proxy receives the response from the server, it returns the response to the Minecraft client. 

This pattern of passing data from `Minecraft client -> proxy -> Minecraft server -> proxy -> Minecraft client` continues while players are connected to the server.

## Future Enhancements

* Windows Service / Systemd integration
    * See https://www.nuget.org/packages/Microsoft.Extensions.Hosting.WindowsServices for Windows Services
    * See https://www.nuget.org/packages/Microsoft.Extensions.Hosting.Systemd for Systemd
    * Will need to be mindful of the current directory and location of `appsettings.json`

## License Information

See the [Tools / Libraries](#tools--libraries) section above
