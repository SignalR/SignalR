var azureTestManager = angular.module('azureTestManagerTest', []);

function Instance(id, state) {
    var self = this;
    self.id = id;
    self.state = state;
}

azureTestManager.controller('AzureTestManagerTestController', function ($scope) {
    $scope.instances = [
        new Instance('123', 'Running'),
        new Instance('456', 'Stopped')
    ];

    $scope.addInstance = function () {
        $scope.instances.push(new Instance('', ''));
    };

    var getInstanceInfo = function () {
        var arrays = [[], []];
        for (var index = 0, length = $scope.instances.length; index < length; index++) {
            arrays[0].push($scope.instances[index].id)
            arrays[1].push($scope.instances[index].state)
        }
        return arrays;
    };

    var hub = $.connection.testManagerHub;

    $scope.updateWorker = function () {
        hub.server.addUpdateWorker(
            $('#address').val(),
            $('#status').val());
    }

    $scope.updateProcess = function (id, status) {
        hub.server.addUpdateProcess(
            id,
            status);
    }

    $scope.removeProcess = function (id) {
        hub.server.removeProcess(
            id);
    }

    hub.client.startProcesses = function (instances, argumentString) {
        $('#count').text(instances);
        $('#argumentString').text(argumentString);
    };

    hub.client.stopProcess = function (processId) {
        $('#stoppedProcess').text(processId);
    }

    hub.client.disconnected = function (id) { }

    $.connection.hub.start().done(function () {
        $('#trace').click(function () {
            hub.server.addTrace(
                $('#address').val(),
                $('#traceMessage').val());
        });

        hub.server.joinConnectionGroup();
    });

});
