// requirements
const WebSocketServer = require('ws').Server;
const ip = require("ip");
const fs = require("fs");
const path = require("path");

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
let lpcLock = 0;

// get configuration from file.
let messageTypes = {};
let messageKey = "";
let messageValue = "";
try {
    let rawdata = fs.readFileSync(path.resolve(__dirname, '../Library/Utilities/config.json'));
    let jsonData = JSON.parse(rawdata);
    for (mt in jsonData.Signaller.MessageTypes) {
        messageTypes[jsonData.Signaller.MessageTypes[mt]] = jsonData.Signaller.MessageTypes[mt];
    }
    messageKey = jsonData.Signaller.MessageKey;
    messageValue = jsonData.Signaller.MessageValue;
}
catch {
    console.log("Failed to parse config.json.");
    process.exit(1);
}
console.log("Loaded configuration from config.json. Current messageTypes:");
console.log(messageTypes);

// room management
let rooms = {};

// handler for a new connection to the server
wss.on('connection', function connection(ws, request, client) {

    // mark this connection uniquely
    ws.uid = uniqueConnectionId++;
    ws.id = null;

    console.log(`Opened connection ${ws.uid}`);

    // handler for receiving a message on the socket
    ws.on('message', (rawMessage) => {
        let target = ws.id === null ? null : (ws.id === "client" ? "source" : "client");

        // signallerClient uses JSON only.
        // exception handling will catch non-JSON artifacts and return an error.
        let message;
        try {
            message = JSON.parse(rawMessage);
        }
        catch(e) {
            console.log(`Failed to parse json message from ${ws.id} ${ws.uid}`);
        }

        console.log(`==== Message received from ${ws.id} ${ws.uid} ====`);
        console.log(message);

        // Return error if messageType is invalid.
        if (message[messageKey] in messageTypes === false) {
            var msg = {};
            msg[messageKey] = messageTypes["Plain"];
            msg[messageValue] = "Invalid message type.";
            ws.send(JSON.stringify(msg));
            return;
        }

        // registration. client -> signaller || source -> signaller.
        if (message[messageKey] === messageTypes["Register"]) {
            ws.id = message[messageValue];

            if ((ws.id !== "source" && ws.id !== "client") || (ws.id === "source" && sourceConnections === 1)) {
                console.log(`Rejected connection ${ws.id} ${ws.uid}`);
                ws.close(ServerFullCode, ServerFullError);
                return;
            }

            // increment open source connections
            console.log(`Registered connection ${ws.uid} as ${ws.id}`);
            sourceConnections += ws.id === "source";

            // notify all connected devices of new member (debug)
            wss.clients.forEach((client) => {
                if (client !== ws && client.readyState === ws.OPEN) {
                    var msg = {};
                    msg[messageKey] = messageTypes["Plain"];
                    msg[messageValue] = `${ws.id} with uid ${ws.uid} connected to Signaller`;
                    client.send(JSON.stringify(msg));
                }
            });

        }
        // plain message. client -> signaller -> source || source -> signaller -> all clients.
        else if (message[messageKey] === messageTypes["Plain"]) {
            wss.clients.forEach((client) => {
                if (client !== ws && client.id === target && client.readyState === ws.OPEN) {
                    client.send(rawMessage);
                }
            });
        }
        // cursor update. client -> signaller -> all other clients.
        else if (message[messageKey] === messageTypes["CursorUpdate"]) {
            wss.clients.forEach((client) => {
                // target = source based on above logic. but this case is an exception, we don't want to send to source.
                if (client !== ws && client.id != target && client.readyState === ws.OPEN) {
                    client.send(rawMessage);
                }
            })
        }
        // webrtc exchange. client -> signaller -> source.
        else if (target === "source") {

            if (message[messageKey] === messageTypes["Offer"]) {
                // ensures only 1 connection at a time
                if (lpcLock) {
                    return;
                }
                lpcLock = 1;
                lastPingedClient = ws.uid;

                // broadcast connecting client uid to source
                wss.clients.forEach((client) => {
                    if (client.id === "source" && client.readyState === ws.OPEN) {
                        var msg = {};
                        msg[messageKey] = messageTypes["Register"];
                        msg[messageValue] = lastPingedClient.toString();
                        client.send(JSON.stringify(msg));
                    }
                })
            }

            if (ws.uid !== lastPingedClient) {
                return;
            }

            wss.clients.forEach((client) => {
                if (client.id === target && client.readyState === ws.OPEN) {
                    client.send(rawMessage);
                }
            });
        }
        // webrtc exchange. source -> signaller -> specific client.
        else {
            wss.clients.forEach((client) => {
                if (client.uid === lastPingedClient && client.readyState === ws.OPEN) {
                    client.send(rawMessage);
                }
            });

            if (message[messageKey] === messageTypes["IceCandidate"]) {
                lpcLock = 0;
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
            console.log(`${ws.uid} was a source connection. Sending shutdown message to all clients.`);
            --sourceConnections;
            wss.clients.forEach((client) => {
                if (client !== ws) {
                    var msg = {};
                    msg[messageKey] = messageTypes["Shutdown"];
                    msg[messageValue] = "";
                    client.send(JSON.stringify(msg));
                    console.log(`Sent shutdown to ${client.id} ${client.uid}`);
                }
            });
        }
        else if (ws.id === "client") {
            wss.clients.forEach((client) => {
                if (client.id === "source" && client.readyState === ws.OPEN) {
                    console.log(`Sending shutdown message for client ${client.uid} to the source`);
                    var msg = {};
                    msg[messageKey] = messageTypes["Shutdown"];
                    msg[messageValue] = ws.uid.toString();
                    client.send(JSON.stringify(msg));
                }
            });
        }
    });
});

console.log(`Running on port ${port} at address ${addr}`);