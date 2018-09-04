// Karma configuration
// Generated on Tue Sep 04 2018 11:57:39 GMT-0700 (Pacific Daylight Time)

module.exports = function (config) {
  config.set({
    basePath: '',
    frameworks: ['qunit'],
    files: [
      'wwwroot/lib/jquery/jquery.js',
      'wwwroot/lib/networkmock/jquery.network.mock.js',
      'wwwroot/lib/signalr/jquery.signalR.js',
      'wwwroot/js/**/*.js',
      'wwwroot/Tests/**/*.js'
    ],
    port: 9876,
    colors: true,
    logLevel: config.LOG_INFO,
    autoWatch: false,
    singleRun: false,
    concurrency: Infinity,

    // Log browser messages to a file, not the terminal.
    browserConsoleLogOptions: {
      level: "debug",
      terminal: false
    },

    // Increase some timeouts that are a little aggressive when multiple browsers (or SauceLabs) are in play.
    browserDisconnectTimeout: 10000, // default 2000
    browserDisconnectTolerance: 1, // default 0
    browserNoActivityTimeout: 4 * 60 * 1000, //default 10000
    captureTimeout: 4 * 60 * 1000, //default 60000

    // Tell the app the URL of the SignalR server. If this causes problems, we can do the auto-detect trick that we use in aspnet/SignalR
    client: {
      args: ["--server", "http://localhost:8989"]
    },

    reporters: [
      "progress",
      "summary",
      "junit",
    ],
    junitReporter: {
      outputDir: "../../artifacts/logs",
      outputFile: "Microsoft.AspNet.SignalR.Client.JS.Tests.junit.xml"
    },

    browsers: ["Chrome"]
  })
}
