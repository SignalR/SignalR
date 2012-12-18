(function ($, window) {
	QUnit.asyncTimeoutTest = function (name, timeout, test) {
		/// <summary>Runs an async test with a specified timeout.</summary>
		/// <param name="name" type="String">The name of the test.</param>
		/// <param name="timeout" type="Number">The number of milliseconds to wait before timing out the test.</param>
		/// <param name="test" type="Function">
		///     The test function to be run, which accepts an end function parameter which should be called when the test is finished.&#10;
		///     The test function can optionally return a cleanup function to be run when the test is finished (either successfully or timed out).&#10;
		///     e.g.
		///     function doTest(end) {&#10;
		///         var somethingExpensive = new Expensive();&#10;
		///         Qunit.ok(true, "Test passed");&#10;
		///         return function() {&#10;
		///             somethingExpensive.stop();&#10;
		///         };&#10;
		///     }&#10;
		/// </param>

		QUnit.asyncTest(name, function () {
			var timeoutId,
				testCleanup,
				hasFinished = false;

			function end(){ 
				if (!hasFinished) {
					clearTimeout(timeoutId);
					hasFinished = true;
					testCleanup();
					QUnit.start();
				}
			}

			timeoutId = setTimeout(function () {
				QUnit.ok(false, "Test timed out.");
				end();
			}, timeout);

			testCleanup = test(end) || $.noop;

			if (!$.isFunction(testCleanup)) {
				throw new Error("Return value of test must be falsey or a function");
			}
		});
	};

	QUnit.skip = {
	    test: function () { },
	    asyncTest: function () { },
	    asyncTimeoutTest: function () { },
	};
})($, window);