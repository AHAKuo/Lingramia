const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = [
  {
    test: /\.jsx?$/,
    exclude: /node_modules/,
    use: {
      loader: 'babel-loader',
      options: {
        presets: [
          ['@babel/preset-env', { targets: { electron: '28' } }],
          ['@babel/preset-react', { runtime: 'automatic' }],
        ],
      },
    },
  },
  {
    test: /\.css$/,
    use: [MiniCssExtractPlugin.loader, 'css-loader'],
  },
  {
    test: /\.(png|jpg|gif|svg)$/i,
    type: 'asset/resource',
  },
];
