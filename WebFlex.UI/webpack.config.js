const path = require("path");
const fs = require("fs");
const MiniCssExtractPlugin = require("mini-css-extract-plugin");
const { CleanWebpackPlugin } = require("clean-webpack-plugin");

const version = "1.0.0";

const tsRoot = path.resolve(__dirname, "Scripts/src/ts");
const viewsRoot = path.resolve(tsRoot, "views");

const cssRoot = path.resolve(__dirname, "Scripts/src/css");
const cssViewsRoot = path.resolve(cssRoot, "views");

const tempRoot = path.resolve(__dirname, "Scripts/.generated");
const distRoot = path.resolve(__dirname, `wwwroot/${version}`);

function ensureDir(dir) {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
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

function getMatchingPageCssPath(entryName) {
    if (!entryName.startsWith("views/")) {
        return null;
    }

    const relativeCssPath = entryName.replace(/^views\//, "") + ".css";
    const cssPath = path.resolve(cssViewsRoot, relativeCssPath);

    if (!fs.existsSync(cssPath)) {
        return null;
    }

    return cssPath;
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

        const wrapperPath = createPageWrapper(entryName, fullPath);
        const cssPath = getMatchingPageCssPath(entryName);

        entries[entryName] = cssPath == null
            ? wrapperPath
            : [wrapperPath, cssPath];
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
        clean: true,
        filename: "js/[name].js",
        path: distRoot,
        devtoolModuleFilenameTemplate: info => {
            return "file:///" + path.resolve(info.absoluteResourcePath).replace(/\\/g, "/");
        },
        assetModuleFilename: "assets/[hash][ext][query]"
    },
    devtool: "source-map",
    resolve: {
        extensions: [".ts", ".js"],
        modules: [
            "node_modules",
            path.resolve(__dirname, "node_modules"),
            path.resolve(__dirname, "../node_modules"),
            "Scripts"
        ]
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
                use: [
                    MiniCssExtractPlugin.loader,
                    "css-loader"
                ]
            },
            {
                test: /\.(svg|png|ico|jpg|jpeg|gif)$/i,
                type: "asset/resource",
                generator: {
                    filename: "imgs/[hash][ext][query]"
                }
            },
            {
                test: /\.(ttf|otf|eot|woff|woff2)$/i,
                type: "asset/resource",
                generator: {
                    filename: "fonts/[hash][ext][query]"
                }
            }
        ]
    },
    plugins: [
        new CleanWebpackPlugin(),
        new MiniCssExtractPlugin({
            filename: pathData => {
                const name = String(pathData.chunk.name).replaceAll("\\", "/");
                return `css/${name}.css`;
            },
            chunkFilename: "css/[id].css"
        })
    ]
};