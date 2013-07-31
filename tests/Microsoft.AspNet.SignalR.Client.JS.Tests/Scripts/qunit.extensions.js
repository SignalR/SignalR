(function ($, window) {
    var QUnitTest = QUnit.test,
        QUnitModule = QUnit.module,
        functionalFlag = "Functional",
        unitFlag = "Unit",
        buildFlag = function (flag) {
            return " (" + flag + ")";
        },
        runModule = true;

    QUnit.comment = function (message) {
        return QUnit.ok(true, message);
    };

    QUnit.fail = function (message) {
        return QUnit.ok(false, message);
    };

    QUnit.isTrue = function (result, message) {
        return QUnit.ok(result === true, message);
    };

    QUnit.isFalse = function (result, message) {
        return QUnit.ok(result === false, message);
    };

    QUnit.isSet = function (actual, message) {
        return QUnit.notEqual(typeof (actual), "undefined", message);
    };

    QUnit.isNotSet = function (actual, message) {
        return QUnit.equal(typeof (actual), "undefined", message);
    };    

    QUnit.theory = function (name, data, test) {
        /// <param name="name" type="String">The name of the test.</param>
        /// <param name="data" type="Array">An array of objects containing the theory data.</param>
        /// <param name="test" type="Function">The test function to run that accepts the theory data.</param>
        testUtilities.theory(data, function () {
            var args = $.makeArray(arguments),
                orig = args[0],
                testName = name + ": ";

            for (var key in orig) {
                testName = testName + key + "=" + orig[key] + ", ";
            }

            testName = testName.substr(0, testName.length - 2);

            QUnit.test(testName, function () {
                test.apply(QUnit, args.splice(1, args.length));
            });
        });
    };

    QUnit.asyncTimeoutTest = function (name, timeout, test) {
        /// <summary>Runs an async test with a specified timeout.</summary>
        /// <param name="name" type="String">The name of the test.</param>
        /// <param name="timeout" type="Number">The number of milliseconds to wait before timing out the test.</param>
        /// <param name="test" type="Function">
        ///     The test function to be run, which accepts an end function parameter which should be called when the test is finished.&#10;
        ///     The test function can optionally accept a second parameter containing wrappers for all of QUnit's assertions (http://api.qunitjs.com/category/assert/).
        ///     These assertions will not be invoked if the test has already completed preventing an unwanted assertion failure during a subsequent test.
        ///     The test function can optionally return a cleanup function to be run when the test is finished (either successfully or timed out).&#10;
        ///     e.g.
        ///     function doTest(end, assert) {&#10;
        ///         var somethingExpensive = new Expensive();&#10;
        ///         assert.ok(true, "Test passed");&#10;
        ///         return function() {&#10;
        ///             somethingExpensive.stop();&#10;
        ///         };&#10;
        ///     }&#10;
        /// </param>
        if (runModule) {
            // Append the functional flag to the end of the name
            name += buildFlag(functionalFlag);

            QUnit.asyncTest(name, function () {
                var timeoutId,
                    testCleanup,
                    hasFinished = false,
                    failOnTimeout = true,
                    assert = {
                        expectTimeout: function() {
                            failOnTimeout = false;
                        },
                        comment: function (message) {
                            if (!hasFinished) {
                                QUnit.comment(message);
                            }
                        },
                        fail: function (message) {
                            if (!hasFinished) {
                                QUnit.fail(message);
                            }
                        },
                        isTrue: function (result, message) {
                            if (!hasFinished) {
                                QUnit.isTrue(result, message);
                            }
                        },
                        isFalse: function (result, message) {
                            if (!hasFinished) {
                                QUnit.isFalse(result, message);
                            }
                        },
                        deepEqual: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.deepEqual(actual, expected, message);
                            }
                        },
                        equal: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.equal(actual, expected, message);
                            }
                        },
                        notDeepEqual: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.notDeepEqual(actual, expected, message);
                            }
                        },
                        notEqual: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.notEqual(actual, expected, message);
                            }
                        },
                        notStrictEqual: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.notStrictEqual(actual, expected, message);
                            }
                        },
                        ok: function (state, message) {
                            if (!hasFinished) {
                                QUnit.ok(state, message);
                            }
                        },
                        strictEqual: function (actual, expected, message) {
                            if (!hasFinished) {
                                QUnit.strictEqual(actual, expected, message);
                            }
                        },
                        throws: function (block, expected, message) {
                            if (!hasFinished) {
                                QUnit.throws(block, expected, message);
                            }
                        },
                        isSet: function (actual, message) {
                            if (!hasFinished) {
                                QUnit.isSet(actual, message);
                            }
                        },
                        isNotSet: function (actual, message) {
                            if (!hasFinished) {
                                QUnit.isNotSet(actual, message);
                            }
                        }
                    };

                function end() {
                    if (!hasFinished) {
                        clearTimeout(timeoutId);
                        failOnTimeout = true;
                        hasFinished = true;
                        testCleanup();
                        QUnit.start();
                    }
                }

                timeoutId = setTimeout(function () {
                    assert.ok(!failOnTimeout, "Test timed out.");
                    end();
                }, timeout);

                testCleanup = test(end, assert, name) || $.noop;

                if (!$.isFunction(testCleanup)) {
                    throw new Error("Return value of test must be falsey or a function");
                }
            });
        }
    };

    QUnit.skip = {
        test: function () { },
        asyncTest: function () { },
        asyncTimeoutTest: function () { },
    };

    // Overwriting the original test so we can first check if we want to run the test
    QUnit.test = function (testname, expected, callback, async) {
        if (runModule) {
            // Append Unit to the end of the name argument only if it wasn't already marked as functional.
            // The QUnit test suite utilizes the generic test method for async tests so a name may already
            // be tagged.
            if (arguments[0].indexOf(buildFlag(functionalFlag)) < 0) {
                arguments[0] += buildFlag(unitFlag);
            }
            
            QUnitTest.apply(this, arguments);
        }
    }

    // Overwriting the original module to never use the lifecycle parameter and instead take in a run bool
    QUnit.module = function (name, run) {
        if (run !== false) {
            run = true;
        }

        runModule = run;
        QUnitModule.call(this, name);
    }
})($, window);