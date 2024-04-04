function sendFromPlugin(payload) {
    var elem = document.getElementById("vJoyId");
    var textProperty = "vJoyName";
    var valueProperty = "vJoyIndex";
    var valueField = "device";

    var items = payload["devices"];
    elem.options.length = 0;

    for (var idx = 0; idx < items.length; idx++) {
        var opt = document.createElement('option');
        opt.value = items[idx][valueProperty];
        opt.text = items[idx][textProperty];
        elem.appendChild(opt);
    }
    elem.value = payload[valueField];
    document.getElementById("status").innerHTML = payload.status
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