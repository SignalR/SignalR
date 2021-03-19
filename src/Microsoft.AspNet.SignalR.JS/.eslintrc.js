module.exports = {
    "env": {
        "browser": true
    },
    "extends": "eslint:recommended",
    "parserOptions": {
        "ecmaVersion": 2015,
        "sourceType": "module"
    },
    "rules": {
        "eqeqeq": "error",
        "no-redeclare": ["error", {"builtinGlobals": false}],
        "no-unused-vars": "off",
        "no-shadow-restricted-names": "off",
        "no-prototype-builtins": "off"
    }
};
