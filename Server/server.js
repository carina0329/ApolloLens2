// requirements
const WebSocketServer = require('ws').Server;
const ip = require("ip");

// network info
const addr = ip.address();
const port = process.env.PORT || "8080";

// the server itself
const wss = new WebSocketServer({ port: port });

// error codes
const ServerFullCode = 4000;
const ServerFullError = 'ServerFullError';

// used for logging
let uniqueConnectionId = 0;

// 1 source connection invariant
let sourceConnections = 0;

// last pinged client to ensure server responds to appropriate client
// when establishing webrtc connections
let lastPingedClient = -1;

// handler for a new connection to the server
wss.on('connection', function connection(ws, request, client) {

    // mark this connection uniquely
    ws.uid = uniqueConnectionId++;

    console.log(`Opened connection ${ws.uid}`);

    // handler for receiving a message on the socket
    ws.on('message', (message) => {
        // (1) registration uses a plain string
        // (2) hello world test. converts to JSON first.
        //  source forwards to all clients
        //  client only sends to source
        // (3) webrtc establishment via ice exhcnage, etc..
        //  client -> source. source -> same client.
        //  this is where keeping track of who's who matters most to enable one-to-many


        // registration / string
        if (message === "client" || message == "source") {
            ws.id = message;

            if (ws.id === "source" && sourceConnections === 1) {
                console.log(`Rejected connection ${ws.id} ${ws.uid}`);
                ws.close(ServerFullCode, ServerFullError);
            }

            // increment open source connections
            console.log(`Registered connection ${ws.uid} as ${ws.id}`);
            sourceConnections += ws.id === "source";
        }
        else {

            let jmsg = JSON.parse(message);
            let target = ws.id === "client" ? "source" : "client";

            if (jmsg.MessageContents === "Hello, World!") {
                wss.clients.forEach((client) => {
                    if (client !== ws && client.id === target && client.readyState === ws.OPEN) {
                        client.send(message);
                    }
                });
            }
            else if (jmsg.MessageType == "5") {
                wss.clients.forEach((client) => {
                    // target = source. we don't want to send to source
                    if (client !== ws && client.id != target && client.readyState === ws.OPEN) {
                        client.send(message);
                    }
                })
            }
            else if (target === "source") {
                lastPingedClient = ws.uid;
                wss.clients.forEach((client) => {
                    if (client.id === target && client.readyState === ws.OPEN) {
                        client.send(message);
                    }
                });
            }
            else {
                wss.clients.forEach((client) => {
                    if (client.uid === lastPingedClient && client.readyState === ws.OPEN) {
                        client.send(message);
                    }
                })
            }
        }
    });

    // handler for the socket connection closing
    ws.on('close', () => {
        console.log(`Closed connection ${ws.id} ${ws.uid}`);

        // decrement sourceConnections and drop all clients if source disconnects
        // source expected to stay connected to signaller throughout the call
        // to allow other clients to join at any time.
        if (ws.id === "source") {
            console.log(`${ws.uid} was a source connection. Dropping all clients.`);
            --sourceConnections;
            wss.clients.forEach((client) => {
                if (client !== ws) {
                    console.log(`Dropped connection ${client.id} ${client.uid}`);
                    client.close(ServerFullCode, ServerFullError);
                }
            })
        }
        
    });
});

console.log(`Running on port ${port} at address ${addr}`);