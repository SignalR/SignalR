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
            hub.connection.id,
            $('#address').val(),
            $('#status').val());
    }

    $scope.updateProcess = function (id, status) {
        hub.server.addUpdateProcess(
            hub.connection.id,
            id,
            status);
    }

    $scope.addErrorTrace = function (id) {
        hub.server.addErrorTrace(
            hub.connection.id,
            id,
            $('#errorTrace').val());
    }

    $scope.addOutputTrace = function (id) {
        hub.server.addOutputTrace(
            hub.connection.id,
            id,
            $('#outputTrace').val());
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
        hub.server.joinConnectionGroup();
    });
});
