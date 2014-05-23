var azureTestManager = angular.module('azureTestManager', []);

function Argument(displayName, prefix, defaultValue, postfix) {
    var self = this;
    self.displayName = displayName;
    self.prefix = prefix;
    self.value = (defaultValue != undefined) ? defaultValue : '';
    self.postfix = (postfix != undefined) ? postfix : '';
    self.argumentString = function () {
        return (self.value != '') ?
            self.prefix + self.value + self.postfix :
            '';
    };
}

azureTestManager.controller('TestManagerController', function ($scope) {
    var hub = $.connection.testManagerHub;
    $scope.updating = false;

    $scope.arguments = [
        new Argument("Host", "/Url:http://", "localhost:29573", "/TestConnection"),
        new Argument("Transport", "/Transport:"),
        new Argument("Batch Size", "/BatchSize:"),
        new Argument("Connect interval", "/ConnectInterval:"),
        new Argument("Connections", "/Connections:"),
        new Argument("Connect Timeout", "/ConnectTimeout:"),
        new Argument("Minimum Server MBytes", "/MinServerMBytes:"),
        new Argument("Send Payload Size", "/SendBytes:"),
        new Argument("Send Timeout", "/SendTimeout:"),
        new Argument("Controller URL", "/ControllerUrl:"),
        new Argument("Number of Clients", "/NumClients:"),
        new Argument("Log File", "/LogFile:", "{0}-{1}.csv"),
        new Argument("Sample Interval", "/SampleInterval:"),
        new Argument("SignalR Instance", "/SignalRInstance:")
    ];

    $scope.getArgumentString = function () {
        var argumentList = [];
        for (var index = 0, length = $scope.arguments.length; index < length; index++) {
            var argument = $scope.arguments[index].argumentString();
            if (argument != "") {
                argumentList.push(argument);
            }
        }
        return argumentList.join(" ");
    };

    $scope.instances = 1;

    $scope.connectionManager = new ConnectionManager();

    $scope.$watch('connectionManager.allSelected', function () {
        if ($scope.connectionManager.allSelected) {
            var connections = $scope.connectionManager.connections;
            for (var index = 0, length = connections.length; index < length; index++) {
                connections[index].selected = true;
            }
        }
        if (!$scope.connectionManager.allSelected && !$scope.connectionManager.clearing) {
            var connections = $scope.connectionManager.connections;
            for (var index = 0, length = connections.length; index < length; index++) {
                connections[index].selected = false;
            }
        }
        $scope.connectionManager.clearing = false;
    });

    $scope.$watch(
        function ($scope) {
            return $scope.connectionManager.connections.map(function (connection) {
                return connection.selected;
            });
        },
        function (newValue, oldValue) {
            for (var index = 0, length = newValue.length; index < length; index++) {
                if (!newValue[index]) {
                    $scope.connectionManager.clearing = true;
                    $scope.connectionManager.allSelected = false;
                    break;
                }
            }
        }, true);

    $scope.addUpdateWorker = function (connectionId, address, status) {
        var worker = $scope.connectionManager.getConnection(connectionId).worker;
        worker.address = address;
        worker.status = status;
        worker.lastUpdate = new Date().toTimeString();
    };

    $scope.removeWorker = function (connectionId) {
        $scope.connectionManager.removeConnection(connectionId);
    };

    $scope.addUpdateProcess = function (connectionId, processId, processStatus) {
        var process = $scope.connectionManager.getConnection(connectionId).getProcess(processId);
        process.state = processStatus;
        process.lastUpdate = new Date().toTimeString();
    };

    $scope.removeProcess = function (connectionId, processId) {
        $scope.connectionManager.getConnection(connectionId).removeProcess(processId);
    }

    $scope.addErrorTrace = function (connectionId, processId, message) {
        var process = $scope.connectionManager.getConnection(connectionId).getProcess(processId);
        process.lastError = message;
    };

    $scope.addOutputTrace = function (connectionId, processId, message) {
        var process = $scope.connectionManager.getConnection(connectionId).getProcess(processId);
        process.lastOutput = message;
    }

    $scope.startProcesses = function () {
        var argumentString = $scope.getArgumentString();
        var connections = $scope.connectionManager.connections;

        for (var index = 0, length = connections.length; index < length; index++) {
            if (connections[index].selected) {
                hub.server.startProcesses(connections[index].connectionId, $scope.instances, argumentString);
            }
        }
    };

    $scope.stopProcess = function (connectionId, processId) {
        hub.server.stopProcess(connectionId, processId);
    };

    hub.client.addUpdateWorker = function (id, address, status, instancePids, instanceStates) {
        $scope.$apply(function () {
            $scope.addUpdateWorker(id, address, status, instancePids, instanceStates);
        });
    };

    hub.client.addUpdateProcess = function (connectionId, processId, processStatus) {
        $scope.$apply(function () {
            $scope.addUpdateProcess(connectionId, processId, processStatus);
        });
    }

    hub.client.removeProcess = function (connectionId, processId) {
        $scope.$apply(function () {
            $scope.removeProcess(connectionId, processId);
        });
    }

    hub.client.addErrorTrace = function (connectionId, processId, message) {
        $scope.$apply(function () {
            $scope.addErrorTrace(connectionId, processId, message);
        });
    }

    hub.client.addOutputTrace = function (connectionId, processId, message) {
        $scope.$apply(function () {
            $scope.addOutputTrace(connectionId, processId, message);
        });
    }

    hub.client.disconnected = function (id) {
        $scope.$apply(function () {
            $scope.removeWorker(id)
        });
    };

    $.connection.hub.start().done(function () {
        hub.server.join('manager');
    });
});