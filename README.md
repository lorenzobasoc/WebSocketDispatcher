# .NET Core WebSocket Solution

A simple .NET Core solution for experimenting with WebSockets. This solution consists of three separate projects, each handling different aspects of WebSocket communication.

## Projects Overview

### 1. SenderClientServer
- **Description:** Handles sending data via WebSocket and listens for a closing message from the Dispatcher.
- **Responsibilities:**
  - Sends data to the DispatcherServer.
  - Listens for and processes closing messages via WebSocket.

### 2. DispatcherServer
- **Description:** Acts as the central hub, receiving data from the Sender, storing it in a Postgres database, and forwarding it to the Receiver.
- **Responsibilities:**
  - Receives data from SenderClientServer.
  - Stores received data in a Postgres database.
  - Forwards the data to the Receiver.

### 3. Receiver
- **Description:** Listens for and receives data forwarded by the DispatcherServer.
- **Responsibilities:**
  - Receives data from the DispatcherServer via WebSocket.