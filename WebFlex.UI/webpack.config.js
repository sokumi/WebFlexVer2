const path = require("path");
const fs = require("fs");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");

const tsRoot = path.resolve(__dirname, "Scripts/src/ts");
const viewsRoot = path.resolve(tsRoot, "views");

function getViewEntries(dir, prefix = "views") {
    const entries = {};

    if (!fs.existsSync(dir)) {
        return entries;
    }

    const items = fs.readdirSync(dir, { withFileTypes: true });

    for (const item of items) {
        const fullPath = path.join(dir, item.name);

        if (item.isDirectory()) {
            Object.assign(
                entries,
                getViewEntries(fullPath, `${prefix}/${item.name}`)
            );

            continue;
        }

        if (!item.isFile() || !item.name.endsWith(".ts")) {
            continue;
        }

        const name = item.name.replace(/\.ts$/, "");
        const entryName = `${prefix}/${name}`;

        entries[entryName] = fullPath;
    }

    return entries;
}

module.exports = {
    mode: process.env.NODE_ENV || "development",
    entry: {
        app: path.resolve(tsRoot, "app.ts"),
        ...getViewEntries(viewsRoot)
    },
    output: {
        filename: "[name].js",
        path: path.resolve(__dirname, "wwwroot/1.0.0/js")
    },
    devtool: "source-map",
    resolve: {
        extensions: [".ts", ".js"]
    },
    module: {
        rules: [
            {
                test: /\.ts$/,
                use: "ts-loader",
                exclude: /node_modules/
            }
        ]
    },
    plugins: [
        new CleanWebpackPlugin()
    ]
};