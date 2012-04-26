$(function () {
    var demo = $.connection.demo,
        groupAddedCalled = false;

    demo.invoke = function (index) {
        $('#msg').append('<li>' + index + ' client state index ->' + this.index + '</li>');
    };

    demo.signal = function (id) {
        $('#dynamicTask').html('The dynamic task! ' + id);
    };

    demo.fromArbitraryCode = function (value) {
        $('#arbitraryCode').html('Sending ' + value + ' from arbitrary code without the hub itself!');
    };

    demo.groupAdded = function () {
        if (groupAddedCalled) {
            throw Error("groupAdded already called!");
        }
        $('#groupAdded').append('Group Added');
        groupAddedCalled = true;
    };

    demo.errorInCallback = function () {
        var o = null;
        o.doIt();
    };

    $.connection.hub.start(options, function () {
        demo.doSomethingAndCallError();

        demo.getValue(function (value) {
            $('#value').html('The value is ' + value + ' after 5 seconds');
        });

        var p = {
            Name: "Foo",
            Age: 20,
            Address: { Street: "One Microsoft Way", Zip: "98052" }
        };

        demo.complexType(p, function () {
            $('#value').html('Complex Type ->' + window.JSON.stringify(this.person));
        });

        demo.multipleCalls();

        demo.simpleArray([5, 5, 6]).done(function () {
            $('#simpleArray').html('Simple array works!');
        });

        demo.complexArray([p, p, p]).done(function () {
            $('#complexArray').html('Complex array works!');
        });

        demo.dynamicTask().fail(function () {
            $('#dynamicTask').html('The dynamic task failed :(');
        });

        demo.plainTask().done(function () {
            $('#plainTask').html('Plain Task Result');
        });

        demo.passingDynamicComplex(p).done(function (age) {
            $('#passingDynamicComplex').html('The person\'s age is ' + age);
        });

        demo.genericTaskTypedAsPlain().done(function (result) {
            $('#genericTaskTypedAsPlain').html(result);
        });

        demo.taskWithException().fail(function (e) {
            $('#taskWithException').html(e);
        });

        demo.genericTaskWithException().fail(function (e) {
            $('#genericTaskWithException').html(e);
        });

        demo.overload().done(function () {
            $('#overloads').html('Void Overload called');

            window.setTimeout(function () {
                demo.overload(1).done(function (n) {
                    $('#overloads').html('Overload with return value called =>' + n);
                });
            }, 1000);
        });

        demo.addToGroups();

        demo.name = 'Testing state!';
        demo.readStateValue().done(function (name) {
            $('#readStateValue').html('Read some state! => ' + name);
        });
    });
});