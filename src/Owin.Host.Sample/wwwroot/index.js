let sendMessage = document.getElementById('sendMessage');

let connectButton = document.getElementById('connectButton');
let sendButton = document.getElementById('sendButton');
let clearLogButton = document.getElementById('clearMessage');
let messages = document.getElementById('messages');
let log = document.getElementById("log");
let ws = null;
let connected = false;
ConsoleLogHTML.connect(log); // Redirect log messages

connectButton.onclick = e => {
    if (ws !== null) {
        console.warn("Closing...");
        ws.close();
        return;
    }
    console.log("Connecting...");
    const url = "ws://" + window.location.host + "/chat";
    ws = new WebSocket(url);
    ws.onopen = onOpen;
    ws.onclose = onClose;
    ws.onmessage = onMessage;
};

sendButton.onclick = e => {
    const msg =document
        .getElementById('sendMessageData')
        .value;
    console.debug("<- " + msg);
    ws.send(msg);
};

function onOpen() {
    console.info('CONNECTED');
    connected = true;
    
    connectButton.innerHTML = 'Disconnect';
    sendMessage.removeAttribute('disabled');
    sendButton.removeAttribute('disabled');
}

function onClose() {
    connected = false;
    console.error("Disconnected");
    connectButton.innerHTML = 'Connect WebSocket';
    sendMessage.setAttribute('disabled', 'disabled');
    sendButton.setAttribute('disabled', 'disabled');
    ws = null;
}

function onMessage (event) {
    const data = event.data;
    console.info("-> " + data);
}
