const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const rules = require('./webpack.rules');

module.exports = {
  mode: process.env.NODE_ENV || 'development',
  module: {
    rules,
  },
  resolve: {
    extensions: ['.js', '.jsx'],
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: path.resolve(__dirname, 'src', 'renderer', 'index.html'),
      filename: 'index.html',
    }),
  ],
};
