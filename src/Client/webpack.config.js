var path = require("path");
var webpack = require("webpack");
var fableUtils = require("fable-utils");
var CopyWebpackPlugin = require('copy-webpack-plugin');
var HtmlWebpackPlugin = require('html-webpack-plugin');
var WebpackHelper = require('./webpack.helper');

function resolve(filePath) {
  return path.join(__dirname, filePath)
}

// -- PROJECT INFORMATION
const TITLE = 'William Tetlow';

const BUNDLE_DEST = resolve('../WebApplication/WebRoot/dist/');
const BUNDLE_FILENAME = 'bundle.[hash].js';

const PROJECT_ENTRY_POINT = resolve('./src/Client.fsproj');

const NODE_MODULES_LOCATION = resolve("../../node_modules/");

const IMG_DIRECTORY_LOCATION = resolve('./public/img');
const IMG_DIRECTORY_DEST = `${BUNDLE_DEST}/img`;

const WEBPACK_SERVER_INDEX_TEMPLATE = resolve('./index-templates/index-dev.cshtml');
const WEBPACK_SERVER_INDEX_FILENAME = 'index.html';

const DEVELOPMENT_SERVER_INDEX_TEMPLATE = resolve('./index-templates/_LayoutDev.cshtml');
const PRODUCTION_SERVER_INDEX_TEMPLATE = resolve('./index-templates/_Layout.cshtml');
const SERVER_INDEX_FILENAME = '_Layout.cshtml'; // NOTE: Filename is relative to BUNDLE_DEST

const WEBPACK_DEV_SERVER_PORT = 3000;
const API_ADDRESS = 'http://localhost:8080';
// ------------------------------------------------------------------------------------------------------------------

var babelOptions = fableUtils.resolveBabelOptions({
  presets: [["es2015", { "modules": false }]],
  plugins: [["transform-runtime", {
    "helpers": true,
    // We don't need the polyfills as we're already calling
    // cdn.polyfill.io/v2/polyfill.js in index.html
    "polyfill": false,
    "regenerator": false
  }]]
});

var isProduction = process.argv.indexOf("-p") >= 0;
console.log("Bundling for " + (isProduction ? "production" : "development") + "...");

module.exports = {
  devtool: "source-map",
  entry: PROJECT_ENTRY_POINT,
  output: {
    filename: BUNDLE_FILENAME,
    path: BUNDLE_DEST,
  },
  resolve: {
    modules: [
      "node_modules", NODE_MODULES_LOCATION
    ]
  },
  devServer: {
    contentBase: BUNDLE_DEST,
    proxy: {
      '/api/*': {
        target: API_ADDRESS,
        changeOrigin: true
      }
    },
    port: WEBPACK_DEV_SERVER_PORT,
    hot: true,
    inline: true
  },
  module: {
    rules: [
      {
        test: /\.fs(x|proj)?$/,
        use: {
          loader: "fable-loader",
          options: {
            babel: babelOptions,
            define: isProduction ? [] : ["DEBUG"]
          }
        }
      },
      {
        test: /\.js$/,
        exclude: /node_modules/,
        use: {
          loader: 'babel-loader',
          options: babelOptions
        },
      },
      {
        test: /\.sass$/,
        use: [
          "style-loader",
          "css-loader",
          "sass-loader"
        ]
      },
      {
        test: /\.(png|jpg|gif)$/,
        use: [
          {
            loader: 'file-loader',
            options: {
              name: '[path][name].[ext]?[hash]',
            }
          }
        ]
      }
    ]
  },
  plugins: WebpackHelper.plugins()
    .inProduction()
      .use(HtmlWebpackPlugin, { inject: false, template: PRODUCTION_SERVER_INDEX_TEMPLATE, filename: SERVER_INDEX_FILENAME, title: TITLE })
    
    .inDevelopment()
      .use(webpack.HotModuleReplacementPlugin)
      .use(webpack.NamedModulesPlugin)
      // Generate index.html for webpack-dev-server during development
      .use(HtmlWebpackPlugin, { inject: false, template: WEBPACK_SERVER_INDEX_TEMPLATE, filename: WEBPACK_SERVER_INDEX_FILENAME, title: TITLE })
      .use(HtmlWebpackPlugin, { inject: false, template: DEVELOPMENT_SERVER_INDEX_TEMPLATE, filename: SERVER_INDEX_FILENAME, title: TITLE })
    
    .inBothEnvironments()
      .use(CopyWebpackPlugin, [{ from: IMG_DIRECTORY_LOCATION, to: IMG_DIRECTORY_DEST },])
      
    .build(isProduction),
};
