// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

import * as path from "path";
import * as _fs from "fs";
import { promisify } from "util";
import { EOL } from "os";

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

// Promisify things from fs we want to use.
const fs = {
    exists: promisify(_fs.exists),
    mkdir: promisify(_fs.mkdir),
    readFile: promisify(_fs.readFile),
    writeFile: promisify(_fs.writeFile),
};

(async function main() {
    const input = process.argv[2];
    const output = process.argv[3];
    const content = (await fs.readFile(input)).toString();

    let browserLogs = {};
    const matcher = /([A-Za-z]+) \d+\.\d+\.\d+ \([^\)]*\) ([A-Z]*): (.*)/;
    for (let line of content.split(/\r\n|\n/)) {
        let matches = matcher.exec(line);
        if (matches && matches[3].indexOf("||") !== -1) {
            let browserName = matches[1];
            let level = matches[2];
            let [fullTestName, message] = matches[3].substring(1, matches[3].length - 1).split("||");
            let [moduleName, testName] = splitAtFirst(fullTestName, "/");

            if (!browserLogs[browserName]) {
                browserLogs[browserName] = {};
            }
            if (!browserLogs[browserName][moduleName]) {
                browserLogs[browserName][moduleName] = {}
            }
            if (!browserLogs[browserName][moduleName][testName]) {
                browserLogs[browserName][moduleName][testName] = []
            }
            browserLogs[browserName][moduleName][testName].push(`${level}: ${message}`);
        }
    }

    if (!await fs.exists(output)) {
        await fs.mkdir(output);
    }

    for (let browser in browserLogs) {
        var browserDir = path.resolve(output, stripIllegalChars(browser));
        if (!await fs.exists(browserDir)) {
            await fs.mkdir(browserDir);
        }

        for (let moduleName in browserLogs[browser]) {
            var moduleDir = path.resolve(browserDir, stripIllegalChars(moduleName));
            if (!await fs.exists(moduleDir)) {
                await fs.mkdir(moduleDir);
            }

            for (let testName in browserLogs[browser][moduleName]) {
                var testFileContent = browserLogs[browser][moduleName][testName].join(EOL);
                var testFileName = path.resolve(moduleDir, stripIllegalChars(testName));
                await fs.writeFile(testFileName, testFileContent);
            }
        }
    }

    return 0;
})().then((code) => process.exit(code)).catch((err) => {
    console.error(err);
    process.exit(1);
});