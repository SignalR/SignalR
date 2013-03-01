QUnit.module("Connection Utility Facts");

QUnit.test("Is Cross Domain functions properly", function () {
    var con = $.connection,
        dummyURLBase = "http://localhost",
        dummyURLHttpsBase = "https://localhost",
        against = {
            protocol: "http:",
            host: "localhost"
        },
        againstHttps = {
            protocol: "https:",
            host: "localhost"
        };

    // Turn off the disabled cross domain (this only applies for command line testin)
    // We want to accurately test the isCrossDomain function so we need to be sure it's
    // enabled.
    window.document.crossDomainDisabled = false;
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLBase + ":80/signalr", against), "Port 80 with stuff after is not cross domain.");
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLBase + ":8080/signalr", against), "Port 8080 still is cross domain with stuff after it");
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLBase + ":8080", against), "Port 8080 still is cross domain by itself");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLBase + ":80", against), "Port 80 by itself is not cross domain");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLBase + ":80/", against), "Port 80 with just a slash at the end is not cross domain");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLHttpsBase + ":443/signalr", againstHttps), "Port 443 with stuff after is not cross domain.");
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLHttpsBase + ":4430/signalr", againstHttps), "Port 443 still is cross domain with stuff after it");
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLHttpsBase + ":4430", againstHttps), "Port 443 still is cross domain by itself");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLHttpsBase + ":443", againstHttps), "Port 443 by itself is not cross domain");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLHttpsBase + ":443/", againstHttps), "Port 443 with just a slash at the end is not cross domain");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLBase + ":443/", { protocol: "http:", host: "localhost:443" }), "Specifying ports directly with an off browser but equivalent urls should not be cross domain. (http but custom 443 port)");
    QUnit.equal(false, con.fn.isCrossDomain(dummyURLHttpsBase + ":443/", { protocol: "https:", host: "localhost:443" }), "Specifying ports directly with an off browser but equivalent urls should not be cross domain. (https)");
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLBase + ":443/", { protocol: "http:", host: "localhost:80" }), "Same server different ports should always be cross domain (http)"); // SUPER edge case scenario
    QUnit.equal(true, con.fn.isCrossDomain(dummyURLHttpsBase + ":443/", { protocol: "https:", host: "localhost:80" }), "Same server different ports should always be cross domain (https)"); // SUPER edge case scenario
    QUnit.equal(false, con.fn.isCrossDomain(against.protocol + "//" + against.host, against), "Comparing against the current url should never be cross domain (http).");
    QUnit.equal(false, con.fn.isCrossDomain(againstHttps.protocol + "//" + againstHttps.host, againstHttps), "Comparing against the current url should never be cross domain (https).");
    QUnit.equal(true, con.fn.isCrossDomain("http://www.microsoft.com", against), "A completely different website should always be cross domain (http).");
    QUnit.equal(true, con.fn.isCrossDomain("http://www.microsoft.com", againstHttps), "A completely different website should always be cross domain (https).");
    window.document.crossDomainDisabled = true;
});