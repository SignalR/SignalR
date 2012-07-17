/*jshint boss: true, rhino: true */
/*globals JSHINT*/

(function (args) {
    var filenames = [],
        optstr, // arg1=val1,arg2=val2,...
        predef, // global1=override,global2,global3,...
        opts   = { rhino: true },
        retval = 0;

    args.forEach(function (arg) {
        if (arg.indexOf("=") > -1) {
            //first time it's the options
            if (!optstr) {
                optstr = arg;
            } else if (!predef) {
                predef = arg;
            }
        } else {
            filenames.push(arg);
        }
    });

    if (filenames.length === 0) {
        print('Usage: jshint.js file.js');
        quit(1);
    }

    if (optstr) {
        optstr.split(',').forEach(function (arg) {
            var o = arg.split('=');
            if (o[0] === 'indent') {
                opts[o[0]] = parseInt(o[1], 10);
            } else {
                opts[o[0]] = (function (ov) {
                    switch (ov) {
                    case 'true':
                        return true;
                    case 'false':
                        return false;
                    default:
                        return ov;
                    }
                }(o[1]));
            }
        });
    }

    if (predef) {
        opts.predef = {};
        predef.split(',').forEach(function (arg) {
            var global = arg.split('=');
            opts.predef[global[0]] = (function (override) {
                return (override === 'false') ? false : true;
            }(global[1]));
        });
    }

    filenames.forEach(function (name) {

        var input = readFile(name);

        if (!input) {
            print('jshint: Couldn\'t open file ' + name);
            quit(1);
        }

        if (!JSHINT(input, opts)) {
            for (var i = 0, err; err = JSHINT.errors[i]; i += 1) {
                print(err.reason + ' (' + name + ':' + err.line + ':' + err.character + ')');
                print('> ' + (err.evidence || '').replace(/^\s*(\S*(\s+\S+)*)\s*$/, "$1"));
                print('');
            }
            retval = 1;
        }
    });

    quit(retval);
}(arguments));
