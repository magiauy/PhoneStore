window.toggleBodyScroll = function (disable) {
    if (disable) {
        document.body.classList.add("overflow-hidden");
    } else {
        document.body.classList.remove("overflow-hidden");
    }
};

(function () {
    var win = window.Window && window.Window.prototype;
    if (!win || typeof win.postMessage !== "function") {
        return;
    }

    var originalPostMessage = win.postMessage;

    win.postMessage = function (message, targetOrigin, transfer) {
        var hasThirdArg = arguments.length > 2;
        if (hasThirdArg && transfer != null) {
            var symbolIterator = typeof Symbol !== "undefined" ? Symbol.iterator : null;
            var isIterable = symbolIterator && typeof transfer[symbolIterator] === "function";
            var hasLengthProp = typeof transfer.length === "number";

            if (!isIterable && !hasLengthProp) {
                // Invalid transfer list provided by third-party scripts; drop it to avoid runtime crashes.
                return originalPostMessage.call(this, message, targetOrigin);
            }
        }

        return hasThirdArg
            ? originalPostMessage.call(this, message, targetOrigin, transfer)
            : originalPostMessage.call(this, message, targetOrigin);
    };
})();
