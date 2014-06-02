var azureTestManager = angular.module('azureTestManager', ['ui.bootstrap']);

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



    $scope.loadTarget = function () {
        $('#targetPage').attr('src', $('#Host').val());
    }

    $scope.arguments = [
        new Argument("Host", "/Url:", "http://localhost:29573", "/TestConnection"),
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

    $scope.configuration = new Configuration();

    $scope.connectionManager = new ConnectionManager();

    $scope.getConnected = function () {
        var sum = 0;
        for (var connectionIndex = 0, connectionLength = $scope.connectionManager.connections.length; connectionIndex < connectionLength; connectionIndex++) {
            var connection = $scope.connectionManager.connections[connectionIndex];
            for (var processIndex = 0, processLength = connection.processes.length; processIndex < processLength; processIndex++) {
                if (connection.processes[processIndex].state != 'Terminated') {
                    sum += connection.processes[processIndex].connected;
                }
            }
        }
        return sum;
    }

    $scope.getReconnected = function () {
        var sum = 0;
        for (var connectionIndex = 0, connectionLength = $scope.connectionManager.connections.length; connectionIndex < connectionLength; connectionIndex++) {
            var connection = $scope.connectionManager.connections[connectionIndex];
            for (var processIndex = 0, processLength = connection.processes.length; processIndex < processLength; processIndex++) {
                if (connection.processes[processIndex].state != 'Terminated') {
                    sum += connection.processes[processIndex].reconnected;
                }
            }
        }
        return sum;
    }

    $scope.getDisconnected = function () {
        var sum = 0;
        for (var connectionIndex = 0, connectionLength = $scope.connectionManager.connections.length; connectionIndex < connectionLength; connectionIndex++) {
            var connection = $scope.connectionManager.connections[connectionIndex];
            for (var processIndex = 0, processLength = connection.processes.length; processIndex < processLength; processIndex++) {
                if (connection.processes[processIndex].state != 'Terminated') {
                    sum += connection.processes[processIndex].disconnected;
                }
            }
        }
        return sum;
    }

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
        var options = {
            hour: "2-digit", minute: "2-digit", second: "2-digit"
        };
        worker.lastUpdate = new Date().toLocaleTimeString("en-us", options);
    };

    $scope.removeWorker = function (connectionId) {
        $scope.connectionManager.removeConnection(connectionId);
    };

    $scope.addUpdateProcess = function (connectionId, processId, processStatus, connectedClients, reconnectedClients, disconnectedClients) {
        var process = $scope.connectionManager.getConnection(connectionId).getProcess(processId);
        process.state = processStatus;
        process.connected = connectedClients;
        process.reconnected = reconnectedClients;
        process.disconnected = disconnectedClients;
        process.lastUpdate = new Date().toTimeString();
    };

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
                hub.server.startProcesses(connections[index].connectionId, $scope.configuration.instances, argumentString);
            }
        }
    };

    $scope.stopAllProcesses = function () {
        for (var connectionIndex = 0, connectionLength = $scope.connectionManager.connections.length; connectionIndex < connectionLength; connectionIndex++) {
            var connection = $scope.connectionManager.connections[connectionIndex];
            for (var processIndex = 0, processLength = connection.processes.length; processIndex < processLength; processIndex++) {
                hub.server.stopProcess(connection.connectionId, connection.processes[processIndex].processId);
            }
        }
    }

    $scope.stopProcess = function (connectionId, processId) {
        hub.server.stopProcess(connectionId, processId);
    };

    hub.client.addUpdateWorker = function (id, address, status, instancePids, instanceStates) {
        $scope.$apply(function () {
            $scope.addUpdateWorker(id, address, status, instancePids, instanceStates);
        });
    };

    hub.client.addUpdateProcess = function (connectionId, processId, processStatus, connectedClients, reconnectedClients, disconnectedClients) {
        $scope.$apply(function () {
            $scope.addUpdateProcess(connectionId, processId, processStatus, connectedClients, reconnectedClients, disconnectedClients);
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