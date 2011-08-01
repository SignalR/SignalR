$(function () {
    var demo = $.connection.demo;

    demo.invoke = function (index) {
        $('#msg').append('<li>' + index + ' client state index ->' + this.index + '</li>');
    };

    $.connection.hub.start(function () {
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

        demo.plainTask().done(function () {
            $('#plainTask').html('Plain Task Result');
        });

        demo.genericTaskTypedAsPlain().done(function (result) {
            $('#genericTaskTypedAsPlain').html(result);
        });

        demo.taskWithException().fail(function (e) {
            $('#taskWithException').html(e);
        });
    });
});