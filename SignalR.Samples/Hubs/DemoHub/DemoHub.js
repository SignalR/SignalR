$(function () {

    signalR.demoHub.invoke = function (index) {
        $('#msg').append('<li>' + index + ' client state index ->' + this.state.index + '</li>');
    };

    signalR.hub.start(function () {
        signalR.demoHub.getValue(function (value) {
            $('#value').html('The value is ' + value + ' after 5 seconds');
        });

        var p = {
            Name: "Foo",
            Age: 20,
            Address: { Street: "One Microsoft Way", Zip: "98052" }
        };

        signalR.demoHub.complexType(p, function () {
            $('#value').html('Complex Type ->' + window.JSON.stringify(this.state.person));
        });

        signalR.demoHub.multipleCalls();
    });
});