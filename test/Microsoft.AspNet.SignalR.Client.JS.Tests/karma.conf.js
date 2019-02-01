// Karma configuration
// Generated on Tue Sep 04 2018 11:57:39 GMT-0700 (Pacific Daylight Time)

let browsers = [];

try {
  // Karma configuration for a local run (the default)
  const fs = require("fs");
  const which = require("which");

  // Bring in the launchers directly to detect browsers
  const ChromeHeadlessBrowser = require("karma-chrome-launcher")["launcher:ChromeHeadless"][1];
  const ChromiumHeadlessBrowser = require("karma-chrome-launcher")["launcher:ChromiumHeadless"][1];
  const FirefoxHeadlessBrowser = require("karma-firefox-launcher")["launcher:FirefoxHeadless"][1];
  const FirefoxDeveloperHeadlessBrowser = require("karma-firefox-launcher")["launcher:FirefoxDeveloperHeadless"][1];
  const EdgeBrowser = require("karma-edge-launcher")["launcher:Edge"][1];
  const SafariBrowser = require("karma-safari-launcher")["launcher:Safari"][1];
  const IEBrowser = require("karma-ie-launcher")["launcher:IE"][1];

  function browserExists(path) {
    // On linux, the browsers just return the command, not a path, so we need to check if it exists.
    if (process.platform === "linux") {
      return !!which.sync(path, { nothrow: true });
    } else {
      return fs.existsSync(path);
    }
  }

  function tryAddBrowser(name, b) {
    var path = b.DEFAULT_CMD[process.platform];
    if (b.ENV_CMD && process.env[b.ENV_CMD]) {
      path = process.env[b.ENV_CMD];
    }
    console.log(`Checking for ${name} at ${path}...`);

    if (path && browserExists(path)) {
      console.log(`Located ${name} at ${path}.`);
      browsers.push(name);
      return true;
    }
    else {
      console.log(`Unable to locate ${name}. Skipping.`);
      return false;
    }
  }

  // We use the launchers themselves to figure out if the browser exists. It's a bit sneaky, but it works.
  tryAddBrowser("ChromeHeadlessNoSandbox", new ChromeHeadlessBrowser(() => { }, {}));
  tryAddBrowser("ChromiumHeadless", new ChromiumHeadlessBrowser(() => { }, {}));
  if (!tryAddBrowser("FirefoxHeadless", new FirefoxHeadlessBrowser(0, () => { }, {}))) {
    tryAddBrowser("FirefoxDeveloperHeadless", new FirefoxDeveloperHeadlessBrowser(0, () => { }, {}));
  }

  // We need to receive an argument from the caller, but globals don't seem to work, so we use an environment variable.
  if (process.env.SIGNALR_TEST_ALL_BROWSERS === "true") {
    tryAddBrowser("Edge", new EdgeBrowser(() => { }, { create() { } }));
    tryAddBrowser("IE", new IEBrowser(() => { }, { create() { } }, {}));
    tryAddBrowser("Safari", new SafariBrowser(() => { }, {}));
  }
} catch (e) {
  console.error(e);
}

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
      args: ["--server", "http://localhost:8989"],
      qunit: {
        autostart: false,
      },
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

    browsers,
    // browsers: ["Chrome"],

    customLaunchers: {
      ChromeHeadlessNoSandbox: {
        base: 'ChromeHeadless',

        // ChromeHeadless runs about 10x slower on Windows 7 machines without the --proxy switches below. Why? ¯\_(ツ)_/¯
        flags: ["--no-sandbox", "--proxy-server='direct://'", "--proxy-bypass-list=*"]
      }
    },
  })
}
