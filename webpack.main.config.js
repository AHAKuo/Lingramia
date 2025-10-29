const path = require('path');
const rules = require('./webpack.rules');
const plugins = require('./webpack.plugins');

module.exports = {
  entry: './src/main/main.ts',
  module: {
    rules,
  },
  plugins,
  resolve: {
    extensions: ['.js', '.ts', '.tsx', '.jsx', '.json'],
  },
};
