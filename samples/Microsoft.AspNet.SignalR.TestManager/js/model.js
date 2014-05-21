
function getItemIndex(array, property, value) {
    for (var index = 0, length = array.length; index < length; index++) {
        if (array[index][property] === value) {
            return index;
        }
    }
    return -1;
}

function Process(processId) {
    var self = this;
    self.processId = processId;
    self.state = '';
    self.lastUpdate = '';
}

function Worker() {
    var self = this;
    self.address = '';
    self.status = '';
    self.lastUpdate = '';
}

function Connection(connectionId) {
    var self = this;
    self.selected = true;
    self.connectionId = connectionId;
    self.worker = new Worker();
    self.processes = [];

    self.getProcess = function (processId) {
        var processIndex = getItemIndex(self.processes, "processId", processId)
        var process;
        if (processIndex != -1) {
            process = self.processes[processIndex];
        }
        else {
            process = new Process(processId);
            self.processes.push(process);
        }
        return process;
    };

    self.removeProcess = function (processId) {
        var processIndex = getItemIndex(self.processes, "processId", processId);
        if (processIndex != -1) {
            self.processes.splice(processIndex, 1);
        }
    }
}

function ConnectionManager($scope) {
    var self = this;
    self.allSelected = true;
    self.connections = []

    self.getConnection = function (connectionId) {
        var connectionIndex = getItemIndex(self.connections, "connectionId", connectionId);
        var connection;
        if (connectionIndex != -1) {
            connection = self.connections[connectionIndex];
        }
        else {
            connection = new Connection(connectionId);
            self.connections.push(connection);
        }
        return connection;
    };

    self.removeConnection = function (connectionId) {
        var connectionIndex = getItemIndex(self.connections, "connectionId", connectionId);
        if (connectionIndex != -1) {
            self.connections.splice(connectionIndex, 1);
        }
    }
}
