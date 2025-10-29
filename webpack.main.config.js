const path = require('path');
const rules = require('./webpack.rules');

module.exports = {
  mode: process.env.NODE_ENV || 'development',
  entry: path.resolve(__dirname, 'src', 'main', 'index.js'),
  module: {
    rules,
  },
  resolve: {
    extensions: ['.js', '.jsx'],
  },
  output: {
    path: path.resolve(__dirname, '.webpack', 'main'),
    filename: 'index.js',
  },
  target: 'electron-main',
};
