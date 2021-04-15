// Replace these variables
var msgType = "__type__";
var msgTargetOrigin = "__targetOrigin__";
var msgJson = __json__;

// A json schema which is known by the 3d viewer
var msg = {
	type: msgType,
	data: msgJson
};

// Do the post message so the viewer can receive it.
window.postMessage(msg, msgTargetOrigin);