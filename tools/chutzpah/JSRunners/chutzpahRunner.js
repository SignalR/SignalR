/*globals phantom, require, console*/
var chutzpah = {};

chutzpah.runner = function (onInitialized, onPageLoaded, isFrameworkLoaded, onFrameworkLoaded, isTestingDone) {
    /// <summary>Executes a test suite and evaluates the results using the provided functions.</summary>
    /// <param name="onInitialized" type="Function">Callback function which is called when the page initialized but not loaded.</param>
    /// <param name="onPageLoaded" type="Function">Callback function which is called when the page is loaded.</param>
    /// <param name="isFrameworkLoaded" type="Function">Function that returns true of false if the test framework has been loaded.</param>
    /// <param name="onFrameworkLoaded" type="Function">Callback function which is called when the test framework is loaded.</param>
    /// <param name="isTestingDone" type="Function">Function that returns true of false if the test suite should be considered complete and ready for evaluation.</param>
    'use strict';

    var page = require('webpage').create(),
        testFrameworkLoaded = false,
        testFile = null,
        testMode = null,
        timeOut = null,
        startTime = null;

    function trySetupTestFramework() {
        if (!testFrameworkLoaded) {
            var loaded = page.evaluate(isFrameworkLoaded);
            if (loaded) {
                testFrameworkLoaded = true;
                page.evaluate(onFrameworkLoaded);
            }
        }
    }


    function waitFor(testIfDone, timeOutMillis) {
        var maxtimeOutMillis = timeOutMillis,
            isDone = false,
            interval;

        function intervalHandler() {
            var now = new Date().getTime();
            if (!isDone && (now - startTime < maxtimeOutMillis)) {
                isDone = testIfDone();
            } else {
                if (!isDone) {
                    phantom.exit(3); // Timeout
                } else {
                    clearInterval(interval);
                }
            }
        }

        interval = setInterval(intervalHandler, 100);
    }

    function wrap(txt) {
        return '#_#' + txt + '#_# ';
    }

    function writeEvent(eventObj, json) {

        // Everytime we get an event update the startTime. We want timeout to happen
        // when were have gone quiet for too long
        startTime = new Date().getTime();
        switch (eventObj.type) {
            case 'FileStart':
            case 'TestStart':
            case 'TestDone':
            case 'Log':
            case 'Error':
                console.log(wrap(eventObj.type) + json);
                break;
                
            case 'FileDone':
                console.log(wrap(eventObj.type) + json);
                phantom.exit(eventObj.failed > 0 ? 1 : 0);
                break;
               
            default:
                break;
        }
    }

    function captureLogMessage(message) {
        try {
            message = message.trim();
            var obj = JSON.parse(message);
            if (!obj || !obj.type) throw "Unknown object";
            writeEvent(obj, message);

        }
        catch (e) {
            // The message was not a test status object so log as message
            var log = { type: 'Log', log: { message: message } };
            writeEvent(log, JSON.stringify(log));
        }
    }

    function onError(msg, stack) {
        var error = { type: 'Error', error: { message: msg, stack: stack } };
        writeEvent(error, JSON.stringify(error));
    }

    function pageOpenHandler(status) {
        var waitCondition = function () { return page.evaluate(isTestingDone); };

        if (status === 'success') {

            // Initialize startTime, this will get updated everytime we recieve 
            // content from the test framework
            startTime = new Date().getTime();
            trySetupTestFramework();
            page.evaluate(onPageLoaded);
            waitFor(waitCondition, timeOut);
        }
        else {
            phantom.exit(2);
        }
    }

    if (phantom.args.length === 0) {
        console.log('Error: too few arguments');
        phantom.exit();
    }

    testFile = phantom.args[0];
    testMode = phantom.args[1] || "execution";
    timeOut = parseInt(phantom.args[2]) || 5001;
    page.onConsoleMessage = captureLogMessage;
    page.onError = onError;

    page.onInitialized = function () {
        if (testMode === 'discovery') {
            page.evaluate(function () {
                window.chutzpah = { testMode: 'discovery' };
            });
        }
        else {
            page.evaluate(function () {
                window.chutzpah = { testMode: 'execution' };
            });
        }
        
        page.evaluate(onInitialized);
    };
    

    page.onResourceReceived = function (url) {
        trySetupTestFramework();
    };

    // Since tests run inside of Phantom are not part of your websites domain
    // lets loosen remote resource security restrictions to help with some testing scenarios

    // Allows local files to make ajax calls to remote urls
    page.settings.localToRemoteUrlAccessEnabled = true; //(default false) 

    // Stops all security (for example you can access content in other domain IFrames)
    page.settings.webSecurityEnabled = false; //(default true)

    page.open(testFile, pageOpenHandler);
};