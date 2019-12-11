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

// room management
// room = {
//    source = null; // holds ws object. room dependent on source, so this maintains 1 source per room invariant.
//    clients = []; // holds ws objects
//    lastPingedClient = -1; // ensure server responds to appropriate client for webrtc initialization
//    lpcLock = 0; // ensure server responds to appropriate client for webrtc initialization
// }
let rooms = {};

// message configuration
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

function sendMessage(ws, msg) {
    if (ws.readyState === ws.OPEN) {
        ws.send(msg);
    }
}

/* Handler Functions */

// REGISTRATION: client -> signaller || source -> signaller
function registrationHandler(ws) {
    // set id to message contents
    ws.id = ws.message;
    if (ws.id !== "source" && ws.id !== "client") {
        console.log(`Rejected connection ${ws.id} ${ws.uid}`);
        ws.close(ServerFullCode, ServerFullError);
        return;
    }

    console.log(`Registered connection ${ws.uid} as ${ws.id}`);
}

// ROOM CREATION: source -> signaller
function roomCreateHandler(ws) {
    // error handling: room already exists
    if (ws.message in rooms || ws.message === "") {
        console.log(ws.message === "" ? "Empty room name is not allowed." : `Source ${ws.uid} is requesting room ${ws.message}. Already exists.`);
        sendMessage(ws, createMessage("RoomCreate", ""));
        return;
    }

    // create default room
    var room = {
        source : null,
        clients : [],
        lastPingedClient : -1,
        lpcLock : 0
    }

    ws.createdRoom = ws.message;

    // adds default room to rooms object & notifies source of success
    rooms[ws.message] = room;
    console.log(`${ws.id} ${ws.uid} created room ${ws.message}`);
    sendMessage(ws, createMessage("RoomCreate", ws.message));
}

// ROOM POLL: client -> signaller
function roomPollHandler(ws) {
    console.log(`${ws.id} ${ws.uid} polling rooms...`);
    console.log(Object.keys(rooms));
    sendMessage(ws, createMessage("RoomPoll", Object.keys(rooms).toString()));
}

// ROOM JOIN: client -> signaller
function roomJoinHandler(ws) {
    // does room exist
    if (!(ws.message in rooms)) {
        console.log(`${ws.id} ${ws.uid} requesting to join nonexistent room ${ws.message}`);
        sendMessage(ws, createMessage("RoomJoin", ""));
        return;
    }
    // room already has a source
    else if (ws.id === "source" && rooms[ws.message].source !== null) {
        console.log(`Source ${ws.uid} requesting to join room ${ws.message} but rejected as it already has a source.`);
        sendMessage(ws, createMessage("RoomJoin", ""));
        return;
    }
    // client attempting to join a room without a source (nondeterministic request)
    else if (ws.id === "client" && rooms[ws.message].source === null) {
        console.log(`Client ${ws.uid} requesting to join room ${ws.message} but rejected as it has no source.`);
        sendMessage(ws, createMessage("RoomJoin", ""));
        return;
    }

    // join room
    ws.rid = ws.message;

    if (ws.id === "source") {
        rooms[ws.rid].source = ws;
        // no one to notify as source must be first inhabitant
    }
    else {
        rooms[ws.rid].clients.push(ws);
        // notify source/clients
        sendMessage(rooms[ws.rid].source, createMessage("Plain", `${ws.id} ${ws.uid} joined room ${ws.rid}`));
        rooms[ws.rid].clients.forEach(client => {
            if (client !== ws) sendMessage(client, createMessage("Plain", `${ws.id} ${ws.uid} joined room ${ws.rid}`));
        });
    }

    // notify requestor of successful join
    console.log(`${ws.id} ${ws.uid} joined room ${ws.rid}`);
    sendMessage(ws, createMessage("RoomJoin", ws.rid));
}

// PLAIN MESSAGE: client -> signaller -> source || source -> signaller -> all clients
function plainMessageHandler(ws) {
    if (ws.rid === null) return;

    ws.target === "source" ? sendMessage(rooms[ws.rid].source, ws.raw) : rooms[ws.rid].clients.forEach((client => {
        if (client !== ws) sendMessage(client, ws.raw);
    }));
}

// OFFER: client -> signaller -> source
function offerHandler(ws) {
    if (ws.rid === null) return;

    // ensures only 1 connection at a time
    if (rooms[ws.rid].lpcLock) return;
    rooms[ws.rid].lpcLock = 1;
    rooms[ws.rid].lastPingedClient = ws.uid;

    // broadcast connecting client uid to source
    sendMessage(rooms[ws.rid].source, createMessage("Register", rooms[ws.rid].lastPingedClient.toString()));
    // forward offer to source
    sendMessage(rooms[ws.rid].source, ws.raw);
}

// ANSWER: source -> signaller -> specific client
function answerHandler(ws) {
    if (ws.rid === null) return;

    rooms[ws.rid].clients.forEach((client) => {
        if (client.uid === rooms[ws.rid].lastPingedClient) sendMessage(client, ws.raw);
    });
}

// ICE CANDIDATE: client -> signaller -> source || source -> signaller -> specific client
function iceCandidateHandler(ws) {
    if (ws.rid === null) return;

    if (ws.target === "source") {
        if (ws.uid !== rooms[ws.rid].lastPingedClient) return;
        sendMessage(rooms[ws.rid].source, ws.raw);
    }
    else {
        rooms[ws.rid].clients.forEach((client) => {
            if (client.uid === rooms[ws.rid].lastPingedClient) sendMessage(client, ws.raw);
        });
        // release lock
        rooms[ws.rid].lpcLock = 0;
    }
}

// CURSOR UPDATE: client -> signaller -> all other clients
function cursorUpdateHandler(ws) {
    if (ws.rid === null) return;

    rooms[ws.rid].clients.forEach((client) => {
        if (client !== ws) sendMessage(client, ws.raw);
    });
}

/* Configuration */

try {
    let rawData = fs.readFileSync(path.resolve(__dirname, '../Library/Utilities/config.json'));
    let jsonData = JSON.parse(rawData);

    // associate proper handlers
    messageHandlers["Register"] = registrationHandler;
    messageHandlers["RoomCreate"] = roomCreateHandler;
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
catch (error) {
    console.log("Failed to parse config.json.");
    console.error(error);
    process.exit(1);
}
console.log("Loaded configuration from config.json. Current messageTypes:");
console.log(messageHandlers);

/* Connection */

// handler for a new connection to the server
wss.on('connection', function connection(ws, request, client) {
    console.log(ws);
    // establish connection identifiers
    ws.uid = uniqueConnectionId++; // unique connection ID
    ws.id = null; // "source" or "client"
    ws.rid = null; // room ID
    ws.createdRoom = ""; // if source disconnects after creating a room, but before joining it

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
            console.log(`Failed to parse json message from ${ws.id} ${ws.rid}:${ws.uid}`);
            return;
        }

        console.log(`==== Message received from ID (${ws.id}) Room:UID (${ws.rid}:${ws.uid}) ====`);
        console.log(message);

        // return error if message type is invalid.
        if (message[messageKey] in messageHandlers === false) {
            sendMessage(ws, createMessage("Plain", "Invalid message type."));
            return;
        }

        // call appropriate handler.
        ws.raw = rawMessage;
        ws.message = message[messageValue];
        messageHandlers[message[messageKey]](ws);
    });

    // handler for the socket connection closing
    ws.on('close', () => {
        console.log(`Closed connection ${ws.id} ${ws.rid}:${ws.uid}`);

        // only valid if room ID exists
        if (ws.createdRoom !== "" && ws.rid === null) delete rooms[ws.createdRoom];
        if (ws.rid === null || !(ws.rid in rooms)) return;

        if (ws.id === "source") {
            console.log(`${ws.rid}:${ws.uid} was source. Shutting down all clients in room ${ws.rid}.`);
            rooms[ws.rid].clients.forEach((client) => {
                sendMessage(client, createMessage("Shutdown", ""));
                console.log(`Sent shutdown to ${client.id} ${client.rid}:${client.uid}`);
            });

            delete rooms[ws.rid];
        }
        else if (ws.id === "client") {
            sendMessage(rooms[ws.rid].source, createMessage("Shutdown", ws.uid.toString()));
            sendMessage(rooms[ws.rid].source, createMessage("Plain", `client ${ws.uid} left room ${ws.rid}`));
            console.log(`Sent client ${ws.uid} shutdown to the source`);
        }
    });
});

console.log(`Running on port ${port} at address ${addr}`);
