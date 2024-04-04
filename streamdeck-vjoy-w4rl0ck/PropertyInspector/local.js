function sendFromPlugin(payload) {
    console.log("got payload", payload)
    window.payload = payload;
    
    document.getElementById("status").innerHTML = `Device #${payload.device}: (${payload.status})`
}

document.addEventListener('websocketCreate', function () {
    websocket.addEventListener('message', function (evt) {
        var jsonObj = JSON.parse(evt.data);
        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            // console.log("PAYLOAD", payload);
            sendFromPlugin(payload)
        }
    });
});