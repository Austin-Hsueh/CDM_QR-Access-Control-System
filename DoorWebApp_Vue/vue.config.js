const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  outputDir : '../DoorWebApp/wwwroot',
  transpileDependencies: true,
  devServer: {
    proxy: {
      '/api': {
        target: 'http://system.clair-de-musique-tw.com', // .NET API 服务器地址
        changeOrigin: true
      }
    }
  },
  chainWebpack: (config) => {
    config.plugin('html').tap(args => {
      args[0].title = '門禁'
      return args;
    })
  },
});
