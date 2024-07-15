const { defineConfig } = require('@vue/cli-service')
module.exports = defineConfig({
  outputDir : '../DoorWebApp/wwwroot',
  transpileDependencies: true,
  chainWebpack: (config) => {
    config.plugin('html').tap(args => {
      args[0].title = '門禁'
      return args;
    })
  },
});
