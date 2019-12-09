/* Imports & Setup */

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

// get configuration from file
let messageHandlers = {};
let messageKey = "";
let messageValue = "";


/* Helper Functions */

// message creation helper
function createMessage(key, val) {
    let message = {}
    message[messageKey] = key;
    message[messageValue] = val;
    return JSON.stringify(message);
}

/* Handler Functions */

// REGISTRATION: client -> signaller || source -> signaller
function registrationHandler(ws) {
    // set id to message contents
    ws.id = ws.message;
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
            client.send(createMessage("Plain", `${ws.id} with uid ${ws.uid} connected to Signaller`));
        }
    });
}

// ROOM CREATION: source -> signaller
function roomCreateHandler(ws) {

}

// ROOM CHANGE: source -> signaller
function roomChangeHandler(ws) {

}

// ROOM DELETION: source -> signaller
function roomDeleteHandler(ws) {

}

// ROOM POLL: client -> signaller
function roomPollHandler(ws) {

}

// ROOM JOIN: client -> signaller
function roomJoinHandler(ws) {

}

// PLAIN MESSAGE: client -> signaller -> source || source -> signaller -> all clients
function plainMessageHandler(ws) {
    wss.clients.forEach((client) => {
        if (client !== ws && client.id === ws.target && client.readyState === ws.OPEN) {
            client.send(ws.raw);
        }
    });
}

// OFFER: client -> signaller -> source
function offerHandler(ws) {
    // ensures only 1 connection at a time
    if (lpcLock) {
        return;
    }
    lpcLock = 1;
    lastPingedClient = ws.uid;

    // broadcast connecting client uid to source
    // forward offer to source as well
    wss.clients.forEach((client) => {
        if (client.id === ws.target && client.readyState === ws.OPEN) {
            client.send(createMessage("Register", lastPingedClient.toString()));
            client.send(ws.raw);
        }
    });
}

// ANSWER: source -> signaller -> specific client
function answerHandler(ws) {
    wss.clients.forEach((client) => {
        if (client.uid === lastPingedClient && client.readyState === ws.OPEN) {
            client.send(ws.raw);
        }
    });
}

// ICE CANDIDATE: client -> signaller -> source || source -> signaller -> specific client
function iceCandidateHandler(ws) {
    if (ws.target === "source") {
        if (ws.uid !== lastPingedClient) {
            return;
        }

        wss.clients.forEach((client) => {
            if (client.id === ws.target && client.readyState === ws.OPEN) {
                client.send(ws.raw);
            }
        });
    }
    else {
        wss.clients.forEach((client) => {
            if (client.uid === lastPingedClient && client.readyState === ws.OPEN) {
                client.send(ws.raw);
            }
        });

        lpcLock = 0;
    }
}

// CURSOR UPDATE: client -> signaller -> all other clients
function cursorUpdateHandler(ws) {
    wss.clients.forEach((client) => {
        // target = source based on above logic. but this case is an exception, we don't want to send to source.
        if (client !== ws && client.id != ws.target && client.readyState === ws.OPEN) {
            client.send(ws.raw);
        }
    });
}

/* Configuration */

try {
    let rawData = fs.readFileSync(path.resolve(__dirname, '../Library/Utilities/config.json'));
    let jsonData = JSON.parse(rawData);

    // associate proper handlers
    messageHandlers["Register"] = registrationHandler;
    messageHandlers["RoomCreate"] = roomCreateHandler;
    messageHandlers["RoomChange"] = roomChangeHandler;
    messageHandlers["RoomDelete"] = roomDeleteHandler;
    messageHandlers["RoomPoll"] = roomPollHandler;
    messageHandlers["RoomJoin"] = roomJoinHandler;
    messageHandlers["Plain"] = plainMessageHandler;
    messageHandlers["Offer"] = offerHandler;
    messageHandlers["Answer"] = answerHandler;
    messageHandlers["IceCandidate"] = iceCandidateHandler;
    messageHandlers["CursorUpdate"] = cursorUpdateHandler;


    // error check the above with the config file to ensure no difference in message types
    for (mtype in messageHandlers) {
        let diff = true;
        for (mt in jsonData.Signaller.MessageTypes) {
            if (jsonData.Signaller.MessageTypes[mt] == mtype) {
                diff = false;
                break;
            }
        }
        if (diff) {
            console.log("Please check messageHandlers and config.json messageTypes to ensure compatibility.");
        }
    }

    messageKey = jsonData.Signaller.MessageKey;
    messageValue = jsonData.Signaller.MessageValue;
}
catch {
    console.log("Failed to parse config.json.");
    process.exit(1);
}
console.log("Loaded configuration from config.json. Current messageTypes:");
console.log(messageHandlers);

/* Connection */

// handler for a new connection to the server
wss.on('connection', function connection(ws, request, client) {

    // mark this connection uniquely
    ws.uid = uniqueConnectionId++;
    ws.id = null;

    console.log(`Opened connection ${ws.uid}`);

    // handler for receiving a message on the socket
    ws.on('message', (rawMessage) => {
        ws.target = ws.id === null ? null : (ws.id === "client" ? "source" : "client");

        // JSON only. exception handling will catch non-JSON artifacts and return an error.
        let message;
        try {
            message = JSON.parse(rawMessage);
        }
        catch(e) {
            console.log(`Failed to parse json message from ${ws.id} ${ws.uid}`);
        }

        console.log(`==== Message received from ${ws.id} ${ws.uid} ====`);
        console.log(message);

        // return error if message type is invalid.
        if (message[messageKey] in messageHandlers === false) {
            ws.send(createMessage("Plain", "Invalid message type."));
            return;
        }

        // call appropriate handler.
        ws.raw = rawMessage;
        ws.message = message[messageValue];
        messageHandlers[message[messageKey]](ws);
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
                    client.send(createMessage("Shutdown", ""));
                    console.log(`Sent shutdown to ${client.id} ${client.uid}`);
                }
            });
        }
        else if (ws.id === "client") {
            wss.clients.forEach((client) => {
                if (client.id === "source" && client.readyState === ws.OPEN) {
                    client.send(createMessage("Shutdown", ws.uid.toString()));
                    console.log(`Sent client ${client.uid} shutdown to the source`);
                }
            });
        }
    });
});

console.log(`Running on port ${port} at address ${addr}`);