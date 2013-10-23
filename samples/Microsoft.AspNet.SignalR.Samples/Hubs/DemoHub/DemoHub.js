$(function () {
    var demo = $.connection.demo,
        typedDemo = $.connection.typedDemoHub,
        groupAddedCalled = false;

    demo.client.invoke = function (index) {
        $('#msg').append('<li>' + index + ' client state index ->' + this.state.index + '</li>');
    };

    demo.on('signal', function (id) {
        $('#dynamicTask').html('The dynamic task! ' + id);
    });

    demo.client.fromArbitraryCode = function (value) {
        $('#arbitraryCode').html('Sending ' + value + ' from arbitrary code without the hub itself!');
    };

    demo.client.groupAdded = function () {
        if (groupAddedCalled) {
            throw Error("groupAdded already called!");
        }
        $('#groupAdded').append('Group Added');
        groupAddedCalled = true;
    };

    demo.client.clientMethod = function () {
        throw new "This should never called because it's mispelled on the server side";
    };

    typedDemo.client.echo = function (message, invokeCount) {
        $('#typed').append('<p>' + message + ' #' + invokeCount + ' triggered!</p>')
    };

    $.connection.hub.logging = true;

    $.connection.hub.start({ transport: activeTransport }, function () {

        typedDemo.server.echo("Typed echo callback").done(function () {
            $('#typed').append('<p>TypedDemoHub.Echo(string message) invoked!</p>')
        });

        demo.server.getValue().done(function (value) {
            $('#value').html('The value is ' + value + ' after 5 seconds');
        });

        var $progress = $("#progress"),
            $progressBar = $progress.find(".bar"),
            $progressStatus = $progress.find("span"),
            reportProgress = demo.server.reportProgress("Long running job")
                .progress(function (value) {
                    $progressBar.width(value + "%");
                    // Give the CSS animation some time to finish
                    setTimeout(function () {
                        $progressStatus.html(value + "%");
                    }, 250);
                })
                .done(function (result) {
                    setTimeout(function () {
                        $progressBar.width("100%");
                        $progressStatus.html(result);
                    }, 250);
                });

        var p = {
            Name: "Foo",
            Age: 20,
            Address: { Street: "One Microsoft Way", Zip: "98052" }
        };

        demo.server.complexType(p).done(function () {
            $('#complexType').html('Complex Type ->' + window.JSON.stringify(this.state.person));
        });

        demo.server.simpleArray([5, 5, 6]).done(function () {
            $('#simpleArray').html('Simple array works!');
        });

        demo.server.complexArray([p, p, p]).done(function () {
            $('#complexArray').html('Complex array works!');
        });

        demo.server.dynamicTask().fail(function () {
            $('#dynamicTask').html('The dynamic task failed :(');
        });

        demo.server.plainTask().done(function () {
            $('#plainTask').html('Plain Task Result');
        });

        demo.server.passingDynamicComplex(p).done(function (age) {
            $('#passingDynamicComplex').html('The person\'s age is ' + age);
        });

        demo.server.genericTaskWithContinueWith().done(function (result) {
            $('#genericTaskWithContinueWith').html(result);
        });

        demo.server.taskWithException().fail(function (e) {
            $('#taskWithException').html(e.toString());
        });

        demo.server.genericTaskWithException().fail(function (e) {
            $('#genericTaskWithException').html(e.toString());
        });

        demo.server.synchronousException().fail(function (e) {
            $('#synchronousException').html(e.toString());
        });

        demo.server.overload().done(function () {
            $('#overloads').html('Void Overload called');

            window.setTimeout(function () {
                demo.server.overload(1).done(function (n) {
                    $('#overloads').html('Overload with return value called =>' + n);
                });
            }, 1000);
        });

        demo.server.inlineScriptTag().done(function (val) {
            $("#inlineScriptTag").html(val);
        });

        demo.server.addToGroups();

        demo.state.name = 'Testing state!';
        demo.server.readStateValue().done(function (name) {
            $('#readStateValue').html('Read some state! => ' + name);
        });

        demo.server.mispelledClientMethod();

        reportProgress.done(function () {
            // Don't start this until now because it blocks on the server which holds up the websocket
            demo.server.multipleCalls()
        });
    });
});