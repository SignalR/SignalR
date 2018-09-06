import * as path from "path";
import * as fs from "fs";
import * as child_process from "child_process";
import * as http from "http";

import * as karma from "karma";
import * as _debug from "debug";

const debug = _debug("signalr-functional-tests:run");

const MAX_ATTEMPTS = 10;

function getArg(list: string[], name: string): string {
    for (let i = 0; i < list.length; i += 1) {
        if (list[i] === name) {
            return list[i + 1];
        }
    }
}

function quickfetch(url: string): Promise<http.ClientResponse> {
    return new Promise<http.ClientResponse>((resolve, reject) => {
        var req = http.request(url, (response) => {
            resolve(response);
        });

        req.on("error", (e) => {
            reject(e);
        });

        req.end()
    });
}

function delay(ms: number): Promise<void> {
    return new Promise((resolve, reject) => {
        setTimeout(resolve, ms);
    });
}

function waitForExit(p: child_process.ChildProcess): Promise<void> {
    return new Promise((resolve, reject) => {
        p.on("close", resolve);
    });
}

function runKarma(config: any): Promise<karma.TestResults> {
    return new Promise((resolve, reject) => {
        const server = new karma.Server(config);
        server.on("run_complete", (browsers, results) => {
            resolve(results);
        });
        server.start();
    });
}

let server: child_process.ChildProcess;
process.on("exit", (code) => {
    // On-exit logic.
    if(server) {
        console.log("Terminating server process");
        server.kill();
    }
});

// Parse arguments
let configuration = "Debug";

for (let i = 2; i < process.argv.length; i += 1) {
    switch (process.argv[i]) {
        case "--configuration":
            i += 1;
            configuration = process.argv[i];
            break;
        case "-v":
        case "--verbose":
            _debug.enable("signalr-functional-tests:*");
            break;
        case "--all-browsers":
            console.log("including non-headless browsers");
            process.env.SIGNALR_TEST_ALL_BROWSERS = "true";
            break;
    }
}

(async function main(): Promise<number> {

    // Check for the executable
    const exePath = path.resolve(__dirname, "bin", configuration, "net461", "Microsoft.AspNet.SignalR.Client.JS.Tests.exe");

    if (!fs.existsSync(exePath)) {
        console.error("Server executable does not exist. Has it been built yet?");
        return 1;
    }

    // Load karma config
    const configPath = path.resolve(__dirname, "karma.conf.js");
    const config = (karma as any).config.parseConfig(configPath);

    if (config.browsers.length === 0) {
        console.log("Unable to locate any suitable browsers. Skipping browser functional tests.");
        return 0;
    }

    // Get the server URL the tests are expecting
    let serverUrl = getArg(config.client.args, "--server");
    if (!serverUrl.endsWith("/")) {
        serverUrl += "/";
    }

    // Spawn the server process
    console.log(`starting ${exePath} to listen at ${serverUrl}...`);
    server = child_process.spawn(exePath, ["--url", serverUrl]);

    // Loop trying to request the status endpoint
    let resp: http.ClientResponse;
    let attempts = 0;
    do {
        attempts += 1;
        debug(`polling ${serverUrl}status... (attempt ${attempts} of ${MAX_ATTEMPTS})`)
        resp = await quickfetch(`${serverUrl}status`);
        await delay(1000);
    } while(resp.statusCode !== 200 && attempts < MAX_ATTEMPTS);

    if(resp.statusCode !== 200) {
        console.error("Server failed to respond after 10 attempts to check status");
        return 1;
    }

    console.log("server is ready, launching karma");

    config.singleRun = true;
    let results = await runKarma(config);

    return results.exitCode;

})().then(code => process.exit(code));