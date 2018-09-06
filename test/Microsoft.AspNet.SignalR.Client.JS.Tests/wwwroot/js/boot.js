// Scripts that run before everything.

// We need to handle reference errors related to JSONP since they are expected behavior when aborting
// a JSONP poll. QUnit will report them as errors though
window.onerror = function(message) {
    if(window.document.jsonpTestsEnabled) {
        // We don't get much detail in this handler :(. This might be too broad...
        return message === "Script error.";
    }
};