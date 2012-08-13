/// <reference path="QUnit/qunit.js" />

  test("A basic test", function () {
      ok(true, "this test is fine");
      var value = "hello";
      equal("hello", value, "We expect value to be hello");
  });