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
