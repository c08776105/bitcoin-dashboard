<template>
  <v-container>
    <v-row>
      <v-col cols="12">
        <h1>Dashboard</h1>
        <div v-if="bitcoinConnectionState === ConnectionState.Disconnected">
          <v-text-field label="Node IP" v-model="nodeIP"/>
          <v-text-field label="Node Port" v-model="nodePort"/>
        </div>
        <div v-if="bitcoinConnectionState === ConnectionState.Connected">
          Connected to node at {{ nodeIpPort }}<br>
          Messages Sent: {{ sentMessages.length }}<br>
          Messages Received: {{ receivedMessages.length }}<br>
          Inv Vectors Received: {{ invVectors.length }}
          <v-card>
            <v-tabs
              v-model="tab"
              bg-color="primary"
            >
              <v-tab value="Sent Messages">Sent Messages</v-tab>
              <v-tab value="Received Messages">Received Messages</v-tab>
              <v-tab value="Inv Messages">inv Vectors</v-tab>
            </v-tabs>

            <v-card-text>
              <v-tabs-window v-model="tab">
                <v-tabs-window-item value="Sent Messages">
                  <v-virtual-scroll :height="300" :items="sentMessages">
                    <template v-slot:default="{ item }">
                      {{ new Date(item.sentTime).toLocaleTimeString() }} - {{ item.message }} -

                      <v-tooltip
                        location="top"
                      >
                        <template v-slot:activator="{ props }">
                          <v-btn
                            @click="appStore.getMessagePayload(item.id)"
                            icon
                            v-bind="props"
                            flat
                            size="xs"
                          >
                            <v-icon color="grey-lighten-1">
                              mdi-magnify
                            </v-icon>
                          </v-btn>
                        </template>
                        <span>View payload hex dump</span>
                      </v-tooltip>
                    </template>
                  </v-virtual-scroll>
                </v-tabs-window-item>

                <v-tabs-window-item value="Received Messages">


                  <v-virtual-scroll :height="300" :items="receivedMessages">
                    <template v-slot:default="{ item }">
                      {{ new Date(item.sentTime).toLocaleTimeString() }} - {{ item.message }} -
                      <v-tooltip
                        location="top"
                      >
                        <template v-slot:activator="{ props }">
                          <v-btn
                            @click="appStore.getMessagePayload(item.id)"
                            icon
                            v-bind="props"
                            flat
                            size="xs"
                          >
                            <v-icon color="grey-lighten-1">
                              mdi-magnify
                            </v-icon>
                          </v-btn>
                        </template>
                        <span>View payload hex dump</span>
                      </v-tooltip>
                    </template>
                  </v-virtual-scroll>
                </v-tabs-window-item>

                <v-tabs-window-item value="Inv Messages">
                  <v-virtual-scroll :height="300" :items="invVectors">
                    <template v-slot:default="{ item }">
                      {{ formatType(item.type) }} - {{ item.hash }} -

                      <v-tooltip
                        location="top"
                      >
                        <template v-slot:activator="{ props }">
                          <v-btn
                            @click="appStore.getData(item.hash, item.type)"
                            icon
                            v-bind="props"
                            flat
                            size="xs"
                          >
                            <v-icon color="grey-lighten-1">
                              mdi-database-search-outline
                            </v-icon>
                          </v-btn>
                        </template>
                        <span>Get data of {{ formatType(item.type) }} record</span>
                      </v-tooltip>
                    </template>
                  </v-virtual-scroll>
                </v-tabs-window-item>
              </v-tabs-window>
            </v-card-text>
          </v-card>

        </div>

        Node connection status: {{ bitcoinConnectionState }}
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="12">
        <h1>Controls</h1>
        <v-btn @click="appStore.connect()" color="primary">
          Connect
          <v-tooltip activator="parent">
            Connects to the node using the IP and Port provided
          </v-tooltip>
        </v-btn>
        <v-btn @click="appStore.disconnect()" color="error">
          Disconnect
          <v-tooltip activator="parent">
            Disconnects from the node
          </v-tooltip>
        </v-btn>
        <br>
        <div class="p-a-6">
          <v-btn @click="appStore.getAddresses()" color="secondary">
            Get Addresses
            <v-tooltip activator="parent">
              Sends a getaddr message to the node. Node may only respond with an addr every 24 hours
            </v-tooltip>
          </v-btn>
        </div>
      </v-col>
    </v-row>
    <v-row>
      <v-col cols="12">
        <h1>Debug Area</h1>
        <v-btn @click="appStore.echo('Testing')" color="primary">Echo</v-btn>
        <v-btn @click="appStore.getState()" color="primary">Refresh</v-btn>
      </v-col>
    </v-row>
  </v-container>

  <v-dialog max-width="500" v-model="displayPopup">
      <v-card :title="popupTitle">
        <v-card-text style="white-space:pre-wrap; font-size: 10px; font-family: monospace">
            {{ popupBody }}
        </v-card-text>

        <v-card-actions>
          <v-spacer></v-spacer>

          <v-btn
            text="Close Dialog"
            @click="displayPopup = false"
          ></v-btn>
        </v-card-actions>
      </v-card>
  </v-dialog>

  <div class="text-center">
    <v-overlay
      :model-value="bitcoinConnectionState === ConnectionState.Connecting"
      class="align-center justify-center"
    >
      <v-progress-circular
        color="primary"
        size="64"
        indeterminate
      ></v-progress-circular>
    </v-overlay>
  </div>
</template>

<script lang="ts" setup>
import {onMounted, ref} from 'vue';
import {useAppStore} from '@/stores/app';
import {storeToRefs} from "pinia";
import {ConnectionState} from "@/stores/ConnectionState";

const appStore = useAppStore();

const tab = ref<string>('Sent Messages');

const formatType = (type: number) => {
  return type === 1  ? 'tx'
  : item.type === 2  ? 'block'
  : '?';
}

const {
  nodeIP,
  nodePort,
  bitcoinConnectionState,
  sentMessages,
  receivedMessages,
  invVectors,
  displayPopup,
  popupTitle,
  popupBody,
  nodeIpPort
} = storeToRefs(appStore);

onMounted(() => {
  appStore.connectToSignalR();
});
</script>
