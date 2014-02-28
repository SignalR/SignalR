QUnit.module("Transports Common - Ajax Abort Facts");

QUnit.test("No transport specified should result in noop behavior for ajaxAbort.", function () {
    var connection = testUtilities.createHubConnection(),
        failed = false;

    connection.log = function () {
        QUnit.ok(false, "ajaxAbort did not noop with an unspecified transport on the connection object.");
        failed = true;
    };

    $.signalR.transports._logic.ajaxAbort(connection);

    if (!failed) {
        QUnit.ok(true, "ajaxAbort noop'd, success!");
    }
});

QUnit.test("Asynchronous flag defaults and is used correctly in ajaxAbort", function () {
    var connection = testUtilities.createHubConnection(),
        failed = false,
        // Saved current jquery ajax function so we can replace it once we've finished
        savedAjax = $.ajax,
        expectedAsync = true;

    connection.transport = "foo";

    $.ajax = function (obj) {
        if (obj.async === expectedAsync) {
            QUnit.ok(true, "Correct async value passed, expected: " + expectedAsync);
        }
        else {
            QUnit.ok(false, "Incorrect async value passed, expected: " + expectedAsync);
        }
    };

    expectedAsync = true;
    $.signalR.transports._logic.ajaxAbort(connection);

    expectedAsync = true;
    $.signalR.transports._logic.ajaxAbort(connection, true);

    expectedAsync = false;
    $.signalR.transports._logic.ajaxAbort(connection, false);

    // Put the $.ajax function back where it was supposed to be
    $.ajax = savedAjax;
});