Bitcoin Node Dashboard
=======================

## 1. Running

To run this dashboard, you first need the below prerequisites:
1. .NET 8 SDK
2. bun JavaScript runtime (https://bun.sh)

Once you have the prerequisites, you can run the dashboard by executing the below commands from the root directory of the project:

This server runs on port 5003 by default

```bash
cd DashboardServer
dotnet restore 
dotnet build
dotnet run
```

```bash
cd DashboardClient 
bun install
bun run dev
```

The dashboard can be accessed via a browser at http://localhost:3000 

## 2. Features
Upon startup, the server will not connect to any Bitcoin node. To connect to a Bitcoin node, you can use the "Connect" button on the dashboard. 

The server will then connect to the Bitcoin node and start fetching data from it. The dashboard will display the following information:

1. Sent and received messages between the server and the Bitcoin node
2. Parsed inventory messages from the Bitcoin node
3. Connection status with the Bitcoin node
4. Timestamps of each sent and received message

## 3. Design

### 3.1 Server

The server is implemented in C# using the .NET 8 SDK. It uses the `System.Net.Sockets` library to connect to the Bitcoin node. The server connects to a bitcoin node on port 8333 and sends messages to the Bitcoin node. The server also parses the messages received from the Bitcoin node and sends them to the client for display.

There is one primary mechanism to communicate with the server, which utilises SignalR (a websocket/long polling framework) which processes and sends messages to connecting clients in an event driven format.

The server consists of the following components:

DTOs: Data Transfer Objects for messages sent and received from the Bitcoin node
Controllers: API endpoints to test connectivity with a Bitcoin node using REST
Services: Business logic to connect to a Bitcoin node and send/receive messages
Hubs: SignalR hub to send and receive messages to/from connected clients
Utilities: Helper classes to parse messages from the Bitcoin node

There is no extra configuration required to run the server. The server will connect to the Bitcoin node on startup and start fetching data from it.

To change the port that the backend server runs on, edit the "applicationUrl" entry in Properties/launchSettings.json


### 3.2 Client

The client is a Vue.js application that connects to the server using SignalR. The client displays the messages sent and received from the Bitcoin node in real-time. The client also displays the parsed inventory messages from the Bitcoin node.

The client shows all messages that were sent and received to and from the Bitcoin node. The client also shows the connection status. The client displays HEX dumps for each of the request types, and can dispatch _getaddr_ and _getdata_ requests ad hoc.