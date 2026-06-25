const path = require("path");
const fs = require("fs");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");

const tsRoot = path.resolve(__dirname, "Scripts/src/ts");
const viewsRoot = path.resolve(tsRoot, "views");
const tempRoot = path.resolve(__dirname, "Scripts/.generated");

function ensureDir(dir) {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
}

function toImportPath(filePath) {
    return filePath.replace(/\\/g, "/");
}

function createPageWrapper(entryName, sourcePath) {
    ensureDir(tempRoot);

    const safeName = entryName.replace(/[\\/]/g, "__");
    const wrapperPath = path.join(tempRoot, `${safeName}.ts`);

    let importPath = path.relative(path.dirname(wrapperPath), sourcePath);
    importPath = "./" + importPath.replace(/\\/g, "/").replace(/\.ts$/, "");

    const content = `
import Page from "${importPath}";
import { runPage } from "../src/ts/framework/page";

runPage(Page);
`;

    fs.writeFileSync(wrapperPath, content.trim(), "utf8");

    return wrapperPath;
}

function getViewEntries(dir, prefix = "views") {
    const entries = {};

    if (!fs.existsSync(dir)) {
        return entries;
    }

    const items = fs.readdirSync(dir, { withFileTypes: true });

    for (const item of items) {
        const fullPath = path.join(dir, item.name);

        if (item.isDirectory()) {
            Object.assign(entries, getViewEntries(fullPath, `${prefix}/${item.name}`));
            continue;
        }

        if (!item.isFile() || !item.name.endsWith(".ts")) {
            continue;
        }

        const name = item.name.replace(/\.ts$/, "");
        const entryName = `${prefix}/${name}`;

        entries[entryName] = createPageWrapper(entryName, fullPath);
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
        path: path.resolve(__dirname, "wwwroot/1.0.0/js"),
        devtoolModuleFilenameTemplate: info => {
            return "file:///" + path.resolve(info.absoluteResourcePath).replace(/\\/g, "/");
        }
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
            },
            {
                test: /\.css$/i,
                use: ["style-loader", "css-loader"]
            }
        ]
    },
    plugins: [
        new CleanWebpackPlugin()
    ]
};