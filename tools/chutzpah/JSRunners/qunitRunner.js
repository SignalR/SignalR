/// <reference path="chutzpahRunner.js" />
/*globals phantom, chutzpah, window, QUnit*/

(function () {
    'use strict';
    
    phantom.injectJs('chutzpahRunner.js');

    function onInitialized() {
    }

    function isTestingDone() {
        return window.chutzpah.isTestingFinished === true;
    }
    
    function isQunitLoaded() {
        return window.QUnit;
    }
    
    function onQUnitLoaded() {
        function log(obj) {
            console.log(JSON.stringify(obj));
        }

        var activeTestCase = null,
            isGlobalError = false,
            fileStartTime = null,
            testStartTime = null,
            beginCallbackContent = QUnit.begin.toString(),
            callback = {};
        
        window.chutzpah.isTestingFinished = false;
        window.chutzpah.testCases = [];
        window.chutzpah.currentModule = null;

        if (window.chutzpah.testMode === 'discovery') {
            // In discovery mode override QUnit's functions

            window.module = QUnit.module = function (name) {
                window.chutzpah.currentModule = name;
            };
            window.test = window.asyncTest = QUnit.test = QUnit.asyncTest = function (name) {

                if (name === 'global failure') return;
                var testCase = { moduleName: window.chutzpah.currentModule, testName: name };
                log({ type: "TestDone", testCase: testCase });
            };
        }
        
        callback.begin = function () {
            // Testing began
            fileStartTime = new Date().getTime();
            log({ type: "FileStart" });
        };

        callback.moduleStart = function(info) {
            window.chutzpah.currentModule = info.name;
        };
        
        callback.moduleDone = function (info) {
            window.chutzpah.currentModule = null;
        };

        callback.testStart = function (info) {

            if (info.name === 'global failure') {
                isGlobalError = true;
                return;
            }
            
            isGlobalError = false;
            testStartTime = new Date().getTime();
            var newTestCase = { moduleName: info.module || window.chutzpah.currentModule, testName: info.name, testResults: [] };
            window.chutzpah.testCases.push(newTestCase);
            activeTestCase = newTestCase;
            
            log({ type: "TestStart", testCase: activeTestCase });
        };

        callback.log = function (info) {
            if (!isGlobalError && info.result !== undefined) {
                var testResult = {};
                
                testResult.passed = info.result;
                QUnit.jsDump.multiline = false; // Make jsDump use single line
                testResult.actual = QUnit.jsDump.parse(info.actual);
                testResult.expected = QUnit.jsDump.parse(info.expected);
                testResult.message = (info.message || "") + "";
                
                activeTestCase.testResults.push(testResult);
            }
        };

        callback.testDone = function (info) {
            if (info.name === 'global failure') return;
            // Log test case when done. This will get picked up by phantom and streamed to chutzpah
            var timetaken = new Date().getTime() - testStartTime;
            activeTestCase.timetaken = timetaken;
            log({ type: "TestDone", testCase: activeTestCase });
        };

        callback.done = function (info) {
            var timetaken = new Date().getTime() - fileStartTime;
            log({ type: "FileDone", timetaken: timetaken, passed: info.passed, failed: info.failed });
            window.chutzpah.isTestingFinished = true;
        };

        /*
            Check to see if we are running on a very old version of QUnit
            in newer version the callbacks are register functions. In older versions
            they are empty functions. The heuristic below is too look for 'config[key]'.
            If we see that it is a newer version of qunit. Otherwise we need to do some more work. 
        */
        if (beginCallbackContent.indexOf('config[key]') < 0) {
            var oldCallback = {
                begin: QUnit.begin,
                done: QUnit.done,
                log: QUnit.log,
                testStart: QUnit.testStart,
                testDone: QUnit.testDone,
                moduleStart: QUnit.moduleStart,
                moduleDone: QUnit.moduleDone
            };

            // For each event, call out callback and then any existing registered callback
            QUnit.begin = function() {
                callback.begin.apply(this, arguments);
                oldCallback.begin.apply(this, arguments);
            };
            QUnit.done = function () {
                callback.done.apply(this, arguments);
                oldCallback.done.apply(this, arguments);
            };
            QUnit.log = function () {
                callback.log.apply(this, arguments);
                oldCallback.log.apply(this, arguments);
            };
            QUnit.testStart = function () {
                callback.testStart.apply(this, arguments);
                oldCallback.testStart.apply(this, arguments);
            };
            QUnit.testDone = function () {
                callback.testDone.apply(this, arguments);
                oldCallback.testDone.apply(this, arguments);
            };
            QUnit.moduleStart = function () {
                callback.moduleStart.apply(this, arguments);
                oldCallback.moduleStart.apply(this, arguments);
            };
            QUnit.moduleDone = function () {
                callback.moduleDone.apply(this, arguments);
                oldCallback.moduleDone.apply(this, arguments);
            };
        }
        else {
            QUnit.begin(callback.begin);
            QUnit.done(callback.done);
            QUnit.log(callback.log);
            QUnit.testStart(callback.testStart);
            QUnit.testDone(callback.testDone);
        }
        
    }
    
    function onPageLoaded() {
        
    }

    try {
        chutzpah.runner(onInitialized, onPageLoaded, isQunitLoaded, onQUnitLoaded, isTestingDone);
    } catch (e) {
        phantom.exit(2); // Unkown error
    }
} ());
