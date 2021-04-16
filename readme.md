 # 3D Viewer embedded demo

The 3D viewer can be embedded in other (web) applications.
In this demo we'll look into how to embed the 3D Viewer into native Windows WPF applications.

## How it works
The viewer is a simple application which can render 3D objects based on a json object which you'll need to supply to the viewer.
You can achieve this by using the PostMessage api.

## How do I embed the viewer in my own website?
You can do the following using html and javascript:
```html

<iframe id="bbaObjectViewerFrame" src="https://viewer3d.2ba.nl/Viewer3D" frameBorder="0"></iframe>

<script>
	var json = {...}; // The json to send to the viewer
    var viewerReady = false; // A boolean value indicating wheter or not the viewer is ready.

    // Add an event listener to your window
	window.addEventListener("message", messageReceived);

    // This function will be called when the viewer uses the window.postMessage api 
    // which will be called on the Iframe's window.opener or window.parent if the former was null.
	function messageReceived(e) {
        if (e.data.type == "Viewer3D:ObjectLoaded") {

            // This is called when the object has been loaded.
            console.log("Object loaded!");
        }
        else if (e.data.type == "Viewer3D:ViewerReady") {
            viewerReady = true;

            // Send a message to the viewer to start loading the object.
            loadObject(json);
        }
	}

    function loadObject(jsonObject) {
        // Make sure the viewer is initalised.
        if (!viewerReady) {
            throw "The viewer is not initialised yet.";
            return;
        }

        var msg = {
            type: "LoadViewer", // This type let's the viewer know that you want to load an object.
            data: jsonObject
        };

        // Get the iframe
        var iframe = frames["bbaObjectViewerFrame"];

        // Gets the window from the frame
        var iframewindow = iframe.contentWindow ? iframe.contentWindow : iframe.contentDocument.defaultView;
        iframewindow.postMessage(msg, "https://viewer3d.2ba.nl")
    }
</script>
```

In the msg object you see the properties `type` and `data`. Type is a string which the viewer expects. This value is used to decide what the viewer has to do with the `data` property, which is an object.

## How can I use the PostMessage api in a native windows application using an embedded browser?
Using the PostMessage api in an embedded browser is a bit tricky. However, I've wrote 2 small bits of code which makes it possible to use and listen to the PostMessage api via C# code.

### Sending a message
To send a message you will have to execute the script below using the `ExecuteScriptAsync(js)` method from a `WebView2` instance and replace the variables. It is almost the same as embedding it in a web project.
```javascript
// MessageSender.js

// Replace these variables
var msgType = "__type__";
var msgTargetOrigin = "__targetOrigin__";
var msgJson = __json__;

var msg = {
	type: msgType,
	data: msgJson
};

// Do the post message so the viewer can receive it.
window.postMessage(msg, msgTargetOrigin);
```

### Receiving a message 
To receive a message it's a bit more difficult. Using Microsoft's WebView2 implementation you cannot directly listen to the `message` event. To achieve this I have overwritten the `window.PostMessage(...)` method which can be seen in the code below. This code will send the message to the .NET program and then call the original `window.PostMessage(...)` method

```javascript
// PostMessageCapturer.js

var _postMessage = window.postMessage; // Original method reference

// Override the postMessage method so we can hook into it in our .NET program.
window.postMessage = function (msg, targetOrigin, transfer) {
	// Execute the original window.postMessage.
	_postMessage(msg, targetOrigin, transfer)
	if (window.chrome.webview) {
		// Send the message to our .NET program.
		window.chrome.webview.postMessage(msg);
	}
}
```

You can run a live demo by running the `BBA3DViewer.WPF` project. You can compile this project using the .NET 5 sdk