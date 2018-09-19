// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

testUtilities.module("Transports Common - Response Facts");

QUnit.test("maximizePersistentResponse decompresses response correctly", function (assert) {
    var connection = testUtilities.createHubConnection(),
        response = {
            C: 1234,
            M: [{ uno: 1, dos: 2 }, { tres: 3, quatro: 4 }],
            T: "Same as above",
            L: 1337,
            G: "foo"
        },
        decompressed;

    decompressed = $.signalR.transports._logic.maximizePersistentResponse(response);
    assert.ok(decompressed.MessageId === response.C, "The decompressed messageId was was correctly set to " + response.C);
    assert.ok(decompressed.Messages === response.M, "The decompressed Messages was was correctly set to " + response.M);
    assert.ok(decompressed.ShouldReconnect === true, "The decompressed ShouldReconnect was was correctly set to true");
    assert.ok(decompressed.LongPollDelay === response.L, "The decompressed LongPollDelay was was correctly set to " + response.L);
    assert.ok(decompressed.GroupsToken === response.G, "The decompressed Disconnect was was correctly set to " + response.G);

    delete response.T;
    decompressed = $.signalR.transports._logic.maximizePersistentResponse(response);

    assert.ok(decompressed.ShouldReconnect === false, "The decompressed ShouldReconnect was was correctly set to false");
});

QUnit.test("updateGroups copies over token correctly", function (assert) {
    var connection = testUtilities.createHubConnection(),
        groupsToken = "SignalR is Awesome";

    $.signalR.transports._logic.updateGroups(connection);
    assert.isNotSet(connection.groupsToken, "The connections groupsToken is not set if we pass an invalid groupsToken to updateGroups.");

    $.signalR.transports._logic.updateGroups(connection, groupsToken);
    assert.ok(connection.groupsToken === groupsToken, "The connections groupsToken property is set to the correctly passed groupsToken.");
});
