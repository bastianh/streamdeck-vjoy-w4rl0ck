function sendFromPlugin(payload) {
    console.log("sendFromPlugin payload", payload)
    document.getElementById("status").innerHTML = `Device #${payload.device}: (${payload.status})`
}

function setup_elements() {

    for (const element of document.querySelectorAll('[data-setting]')) {
        switch(element.nodeName) {
            case "INPUT":
            case "SELECT":
                switch(element.type) {
                    case "text":
                    case "select-one":
                    case "checkbox":
                        element.addEventListener("input", setSettings);
                        break;
                    default:
                        console.warn("Unknown element.type", element.type, element)
                }
                break;
            default:
                console.warn("Unknown element.nodeName", element.nodeName, element)

        }
    } 
    
    for (const controlElement of document.querySelectorAll('[data-visible]')) {
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
    setup_elements();
});


