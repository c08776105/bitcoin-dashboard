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
          Connected to node at {{ nodeIP }}:{{ nodePort }}<br>
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
                      {{ item.sentTime }} - {{ item.message }}
                    </template>
                  </v-virtual-scroll>
                </v-tabs-window-item>

                <v-tabs-window-item value="Received Messages">


                  <v-virtual-scroll :height="300" :items="receivedMessages">
                    <template v-slot:default="{ item }">
                      {{ item.sentTime }} - {{ item.message }}
                    </template>
                  </v-virtual-scroll>
                </v-tabs-window-item>

                <v-tabs-window-item value="Inv Messages">
                  <v-virtual-scroll :height="300" :items="invVectors">
                    <template v-slot:default="{ item }">
                      {{ item.type }} - {{ item.hash }}
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
        <v-btn @click="appStore.connect()" color="primary">Connect</v-btn>
        <v-btn @click="appStore.disconnect()" color="error">Disconnect</v-btn>
        <br>
        <div class="p-a-6">
        <v-btn @click="appStore.getAddresses()" color="secondary">Get Addresses</v-btn>
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
</template>

<script lang="ts" setup>
import {onMounted, ref} from 'vue';
import {useAppStore} from '@/stores/app';
import {storeToRefs} from "pinia";
import {ConnectionState} from "@/stores/ConnectionState";

const appStore = useAppStore();

const tab = ref<string>('Sent Messages');

const {nodeIP, nodePort, bitcoinConnectionState, sentMessages, receivedMessages, invVectors} = storeToRefs(appStore);

onMounted(() => {
  appStore.connectToSignalR();
});
</script>
