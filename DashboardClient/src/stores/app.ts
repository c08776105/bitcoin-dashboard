// Utilities
import {defineStore} from 'pinia'
import {ref} from 'vue';
import {HubConnectionBuilder, HubConnectionState} from "@microsoft/signalr";
import {ConnectionState} from "@/stores/ConnectionState";
import {InvVector} from "@/stores/InvVector";
import {NodeMessage} from "@/stores/NodeMessage";

export const useAppStore = defineStore('app', () => {
  let connection: HubConnection = undefined;

  const nodeIP = ref<string>('45.144.112.208');
  const nodePort = ref<short>(8333);

  const bitcoinConnectionState = ref<ConnectionState>(ConnectionState.Disconnected);

  const connectionAttempts = 0;

  const protocolVersion = ref<number>(0);
  const userAgent = ref<string>('');
  const sentMessages = ref<NodeMessage[]>([]);
  const receivedMessages = ref<NodeMessage[]>([]);
  const invVectors = ref<InvVector[]>([]);

  const connectToSignalR = async () => {
    connection = new HubConnectionBuilder()
      .withUrl('http://localhost:5003/bitcoinNodeHub')
      .build();

    await connection.start();
    configureHandlers();

    getState();
  }

  // Hub actions
  const getState = () => {
    connection.invoke('GetState');
  }

  const echo = async (echoString: string) => {
    try {
      await connection.invoke('Echo', echoString);
    } catch (err) {
      console.error(err);
    }
  };

  const connect = async () => {
    try {
      if (connection.state === HubConnectionState.Disconnected) {
        bitcoinConnectionState.value = ConnectionState.Connecting;
        await connection.start();
      }
    } catch (err) {
      console.error(err);
      bitcoinConnectionState.value = ConnectionState.Error;
    }

    if (connection.state === HubConnectionState.Connected) {
      try {
        connection.invoke('ConnectToNode', nodeIP.value, nodePort.value);
      } catch (err) {
        console.error(err);
        bitcoinConnectionState.value = ConnectionState.Error;
      }

    }

    if (connection.state === HubConnectionState.Connecting) {
      if (connectionAttempts > 5) {
        console.log('Connection timeout after SignalR hub connection attempts exceeded timeout period');
      } else {
        console.log('SignalR hub still connecting, trying connection again in 1 second...');
        setTimeout(connect, 1000);
      }
    }
  };

  const disconnect = () => {
    connection.invoke('DisconnectFromNode');
  }

  const getAddresses = () => {
    connection.invoke('SendGetAddresses');
  }

  // Event handlers
  const configureHandlers = () => {
    connection.on("Echo", (echoString: string) => {
      console.log(echoString);
    });

    connection.on("State", (state) => {
      console.log(state);

      if (state.connected) {
        bitcoinConnectionState.value = ConnectionState.Connected;
      }

      protocolVersion.value = state.protocolVersion;
      userAgent.value = state.userAgent;
      sentMessages.value = state.sentMessages;
      receivedMessages.value = state.receivedMessages;
      invVectors.value = state.invVectors;
    });

    connection.on('ConnectError', (ex: string) => {
      console.error(ex);
      bitcoinConnectionState.value = ConnectionState.Error;
    });

    connection.on('ConnectSuccess', () => {
      console.log("Received ConnectSuccess event from SignalR hub");
      bitcoinConnectionState.value = ConnectionState.Connected;
    });

    connection.on('DisconnectError', (ex: string) => {
      console.error(ex);
      bitcoinConnectionState.value = ConnectionState.Error;
    });

    connection.on('DisconnectSuccess', () => {
      bitcoinConnectionState.value = ConnectionState.Disconnected;
    });

    connection.on('SendGetAddressesError', (ex: string) => {
      console.error(ex);
    });

    connection.on('SendGetAddressesSuccess', () => {
      console.log('Successfully sent GetAddresses message to node');
    });
  };

  return {
    connectToSignalR,
    connection,
    nodeIP,
    nodePort,
    bitcoinConnectionState,
    connect,
    disconnect,
    getAddresses,
    configureHandlers,
    echo,
    getState,
    protocolVersion,
    userAgent,
    sentMessages,
    receivedMessages,
    invVectors
  };
})
