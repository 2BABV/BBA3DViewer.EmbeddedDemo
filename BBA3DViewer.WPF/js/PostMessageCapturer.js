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