module.exports = {
    aliases: {

    },
    clean: [

    ],
    mappings: {
        // The full build, not vue.runtime.min.js: the request wizard compiles its row templates
        // from <script type="text/x-template"> blocks at runtime, which needs the compiler.
        "@node_modules/vue/dist/vue.min.js": "@libs/vue/"
    }
};
