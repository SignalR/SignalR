QUnit.module("Common - Utility Facts");

QUnit.theory("firefoxMajorVersion parses user agent version correctly",
    // Theory data
    [
        { userAgent: "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.8; rv:24.0) Gecko/20100101 Firefox/24.0", expect: 24 },
        { userAgent: "Mozilla/5.0 (Windows NT 6.1; rv:22.0) Gecko/20130405 Firefox/22.0", expect: 22 },
        { userAgent: "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:22.0) Gecko/20130328 Firefox/22.0", expect: 22 },
        { userAgent: "Mozilla/5.0 (Windows NT 6.1; Win64; x64; rv:14.0) Gecko/20120405 Firefox/14.0a1", expect: 14 },
        { userAgent: "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:11.0) Gecko Firefox/11.0", expect: 11 },
        { userAgent: "Mozilla/5.0 (X11; Mageia; Linux x86_64; rv:10.0.9) Gecko/20100101 Firefox/10.0.9", expect: 10 },
        { userAgent: "Mozilla/5.0 (Windows NT 6.2) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1467.0 Safari/537.36", expect: 0 },
        { userAgent: "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1; WOW64; Trident/6.0)", expect: 0 },
        { userAgent: "This is not a user agent string", expect: 0 },
        { userAgent: "Firefox/24.0", expect: 24 },
        { userAgent: "Firefox/24.0 ", expect: 24 }
    ],
    // Test function
    function (userAgent, expect) {
        var actual = $.signalR._.firefoxMajorVersion(userAgent);

        QUnit.assert.equal(actual, expect, "firefoxMajorVersion should return correct major version from user agent");
    });

QUnit.test("markActive stops connection if called after extended period of time.", function () {
    var connection = testUtilities.createConnection(),
        stopCalled = false;

    connection._.lastActiveAt = new Date(new Date().valueOf() - 3000).getTime()
    connection.reconnectWindow = 2900;

    connection.stop = function () {
        stopCalled = true;
    };

    $.signalR.transports._logic.markActive(connection);

    QUnit.equal(stopCalled, true, "Stop was called.");
});