const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  outputDir : '../DoorWebApp/wwwroot',
  transpileDependencies: true,
  devServer: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:80', // .NET API 服务器地址 (launchSettings.json)
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
