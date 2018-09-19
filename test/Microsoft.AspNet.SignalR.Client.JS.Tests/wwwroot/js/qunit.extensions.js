(function ($, window) {
    var QUnitTest = QUnit.test,
        QUnitModule = QUnit.module,
        functionalFlag = "Functional",
        unitFlag = "Unit",
        buildFlag = function (flag) {
            return " (" + flag + ")";
        },
        runModule = true;

    QUnit.assert.comment = function (message) {
        return this.ok(true, message);
    };

    QUnit.assert.fail = function (message) {
        return this.ok(false, message);
    };

    QUnit.assert.isTrue = function (result, message) {
        return this.ok(result === true, message);
    };

    QUnit.assert.isFalse = function (result, message) {
        return this.ok(result === false, message);
    };

    QUnit.assert.isSet = function (actual, message) {
        return this.notEqual(typeof (actual), "undefined", message);
    };

    QUnit.assert.isNotSet = function (actual, message) {
        return this.equal(typeof (actual), "undefined", message);
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

            QUnit.test(name, function (assert) {
                var timeoutId,
                    testCleanup,
                    hasFinished = false,
                    failOnTimeout = true;

                var done = assert.async();

                function end() {
                    if (!hasFinished) {
                        clearTimeout(timeoutId);
                        failOnTimeout = true;
                        hasFinished = true;
                        if (testCleanup) {
                            testCleanup();
                        }
                        done();
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

    QUnit.skipIf = function (condition) {
        if (!condition) {
            return {
                test: function () { QUnit.test.apply(this, Array.prototype.slice.call(arguments)) },
                asyncTest: function () { QUnit.asyncTest.apply(this, Array.prototype.slice.call(arguments)) },
                asyncTimeoutTest: function () { QUnit.asyncTimeoutTest.apply(this, Array.prototype.slice.call(arguments)) },
            }
        } else {
            return {
                test: function () { },
                asyncTest: function () { },
                asyncTimeoutTest: function () { },
            }
        }
    }

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
                if(!window._config.runUnit) {
                    // Don't define the test at all
                    return;
                }
                arguments[0] += buildFlag(unitFlag);
            } else if(!window._config.runFunctional) {
                // Don't define the test at all
                return;
            }

            QUnitTest.apply(this, arguments);
        }
    }
})($, window);
