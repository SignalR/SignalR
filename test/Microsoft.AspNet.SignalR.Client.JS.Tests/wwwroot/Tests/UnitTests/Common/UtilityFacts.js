// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("Common - Utility Facts");

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
].forEach(function (data) {
    QUnit.test("firefoxMajorVersion parses user agent '" + data.userAgent + "' to version '" + data.expect + "'", function (assert) {
        var actual = $.signalR._.firefoxMajorVersion(data.userAgent);

        assert.equal(actual, data.expect, "firefoxMajorVersion should return correct major version from user agent");
    });
})


QUnit.test("markActive stops connection if called after extended period of time.", function (assert) {
    var connection = testUtilities.createConnection(),
        stopCalled = false;

    connection._.lastActiveAt = new Date(new Date().valueOf() - 3000).getTime()
    connection._.keepAliveData.activated = true;
    connection.reconnectWindow = 2900;

    connection.stop = function () {
        stopCalled = true;
    };

    $.signalR.transports._logic.markActive(connection);

    assert.equal(stopCalled, true, "Stop was called.");
});