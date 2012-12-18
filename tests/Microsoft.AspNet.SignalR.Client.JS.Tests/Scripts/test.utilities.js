testUtilities = {
	createHubConnection: function () {
		if (window.document.testUrl !== 'auto') {
			return $.hubConnection(window.document.testUrl, { useDefaultPath: false });
		}
		return $.hubConnection('signalr', { useDefaultPath: false });
	}
};