import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export function useSignalR(onOrderUpdate?: (data: any) => void) {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const hubUrl = `${window.location.origin}/hubs/orders`;
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    newConnection.on('OrderUpdate', (data) => {
      if (onOrderUpdate) {
        onOrderUpdate(data);
      }
    });

    newConnection
      .start()
      .then(() => {
        setIsConnected(true);
        setConnection(newConnection);
      })
      .catch(() => {
        setIsConnected(false);
      });

    newConnection.onreconnecting(() => setIsConnected(false));
    newConnection.onreconnected(() => setIsConnected(true));
    newConnection.onclose(() => setIsConnected(false));

    return () => {
      newConnection.stop();
    };
  }, []);

  return { connection, isConnected };
}
