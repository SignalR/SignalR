QUnit.module("Connection Utility Facts");

QUnit.test("Is Cross Domain functions properly", function () {
    var con = $.connection,
        dummyURLBase = "http://localhost",
        against = {
            protocol: "http:",
            host: "localhost"
        };

    equal(false, con.fn.isCrossDomain(dummyURLBase + ":80/signalr", against), "Port 80 with stuff after is not cross domain.");
    equal(true, con.fn.isCrossDomain(dummyURLBase + ":8080/signalr", against), "Port 8080 still is cross domain with stuff after it");
    equal(true, con.fn.isCrossDomain(dummyURLBase + ":8080", against), "Port 8080 still is cross domain by itself");
    equal(false, con.fn.isCrossDomain(dummyURLBase + ":80", against), "Port 80 by itself is not cross domain");
    equal(false, con.fn.isCrossDomain(dummyURLBase + ":80/", against), "Port 80 with just a slash at the end is not cross domain");
    equal(false, con.fn.isCrossDomain(against.protocol + "//" + against.host, against), "Comparing against the current url should never be cross domain.");
    equal(true, con.fn.isCrossDomain("http://www.microsoft.com", against), "A completely different website should always be cross domain.");
});