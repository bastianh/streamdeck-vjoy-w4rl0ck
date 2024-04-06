function sendFromPlugin(payload) {
    console.log("got payload", payload)
    window.payload = payload;

    document.getElementById("status").innerHTML = `Device #${payload.device}: (${payload.status})`
}

function setup_autohider() {

    let controlElements = document.querySelectorAll('[data-visible]')

    for (const controlElement of controlElements) {
        const hideElement = document.getElementById(controlElement.getAttribute("data-visible"));
        hideElement.style.display = controlElement.checked ? "block" : "none";
        document.addEventListener('configurationLoaded', function () {
            hideElement.style.display = controlElement.checked ? "block" : "none"
        });
        controlElement.addEventListener('change', function () {
            hideElement.style.display = controlElement.checked ? "block" : "none"
        });
    }
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
    setup_autohider();
});


