// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import * as path from "path";
import * as _fs from "fs";
import * as child_process from "child_process";
import * as http from "http";
import { promisify } from "util";

import * as karma from "karma";
import * as _debug from "debug";
import { EOL } from "os";

const debug = _debug("signalr-functional-tests:run");

const MAX_ATTEMPTS = 10;
const ARTIFACTS_DIR = path.resolve(__dirname, "..", "..", "artifacts");
const LOGS_DIR = path.resolve(ARTIFACTS_DIR, "logs");
const BROWSER_LOGS_DIR = path.resolve(LOGS_DIR, "browserlogs");

// Promisify things from fs we want to use.
const fs = {
    exists: promisify(_fs.exists),
    mkdir: promisify(_fs.mkdir),
    readFile: promisify(_fs.readFile),
    writeFile: promisify(_fs.writeFile),
};

// Gotten from System.IO.Path.InvalidPathChars in .NET
const INVALID_PATH_CHARS = [34, 60, 62, 124, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31];

function stripIllegalChars(input: string): string {
    for (const invalidCode of INVALID_PATH_CHARS) {
        input = input.replace(new RegExp(String.fromCharCode(invalidCode), "g"), "");
    }
    input = input.replace(/\//g, "");
    input = input.replace(/\//g, "");
    input = input.replace(/:/g, "");
    input = input.replace(/;/g, "");
    return input;
}

function splitAtFirst(input: string, match: string): [string, string] {
    const idx = input.indexOf(match);
    if (idx === -1) {
        return [input, null];
    } else {
        return [
            input.substring(0, idx),
            input.substring(idx + 1)
        ];
    }
}

function getArg(list: string[], name: string): string {
    for (let i = 0; i < list.length; i += 1) {
        if (list[i] === name) {
            return list[i + 1];
        }
    }
}

function quickfetch(url: string): Promise<http.IncomingMessage> {
    return new Promise<http.IncomingMessage>((resolve, reject) => {
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
let serverPath = path.join(__dirname, "..", "..", "artifacts", configuration, "bin", "Microsoft.AspNet.SignalR.Client.JS.Tests", "net461", "Microsoft.AspNet.SignalR.Client.JS.Tests.exe");

for (let i = 2; i < process.argv.length; i += 1) {
    switch (process.argv[i]) {
        case "--server-path":
            i += 1;
            serverPath = process.argv[i];
            break;
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
    const exePath = path.resolve(serverPath);

    if (!await fs.exists(exePath)) {
        console.error(`Server executable does not exist at '${exePath}'. Has it been built yet?`);
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
    let resp: http.IncomingMessage;
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
    if (!await fs.exists(ARTIFACTS_DIR)) {
        await fs.mkdir(ARTIFACTS_DIR);
    }
    if (!await fs.exists(LOGS_DIR)) {
        await fs.mkdir(LOGS_DIR);
    }
    if (!await fs.exists(BROWSER_LOGS_DIR)) {
        await fs.mkdir(BROWSER_LOGS_DIR);
    }
    config.browserConsoleLogOptions.path = path.resolve(BROWSER_LOGS_DIR, `console.${new Date().toISOString().replace(/:|\./g, "-")}.log`)

    console.log("server is ready, launching karma");

    config.singleRun = true;
    let results = await runKarma(config);

    return results.exitCode;

})().then(code => process.exit(code));