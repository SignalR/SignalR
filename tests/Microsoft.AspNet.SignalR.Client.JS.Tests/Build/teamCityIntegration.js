(function (window) {
    var testName = "",
        runSuite = function () {
            console.log("##teamcity[testSuiteStarted name='QUnit Tests']");
        },
        sanitize = function (what) {
            return what.replace(/['\n\r\|\[\]]/g, function (match) {
                return "|" + match;
            });
        };

    // We do not want to output TeamCity related information if this is not a command line run
    if (window.document.commandLineTest) {
        /**************************************************************************************                                                          
        We need to output logging to team city on command line builds.  However this will not
        work if the Chutzpah test runner does not have its debugging flag set to true.  Reason
        being is that the debug flag pumps logs through phantomjs to the console.  From the
        console TeamCity then picks up the logs and deals with them appropriately.  Ultimately
        updating the test count on TeamCity.
        ***************************************************************************************/

        QUnit.testStart = function (test) {
            // Only run suite once
            if (!window.document.running) {
                window.document.running = true;
                runSuite();
            }

            // Every time a test starts update the test name
            testName = test.name;
        };

        // Log the appropriate messages to team city
        QUnit.log = function (args) {
            var name = sanitize((testName + " > " + args.message)),
                expected = sanitize(args.expected),
                actual = sanitize(args.actual);

            console.log("##teamcity[testStarted name='" + name + "']");

            if (!args.result) {
                console.log("##teamcity[testFailed type='comparisonFailure' name='" + name + "' details='expected=" + expected + ", actual=" + actual + "' expected='" + expected + "' actual='" + actual + "']");
            }

            console.log("##teamcity[testFinished name='" + name + "']");
        };

        // On suite completion notify team city
        QUnit.done = function () {
            console.log("##teamcity[testSuiteFinished name='QUnit Tests']");
        };
    }
})(window);