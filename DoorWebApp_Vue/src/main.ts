import { createApp } from "vue";
import ElementPlus from "element-plus";
import "element-plus/dist/index.css";
import App from "./App.vue";
import router from "./router";
import "bootstrap/dist/css/bootstrap.min.css";
import "bootstrap";
import { createPinia } from "pinia";
import * as ElementPlusIconsVue from "@element-plus/icons-vue";
import FontAwesomeIcon from "@/font-awesome";
import i18n from "@/locale";
import mock from "@/apis/mocks/mock";
import { MyAuthPlugin } from "@/plugins/myAuth";
import { useUserInfoStore } from "./stores/UserInfoStore";
import { usePaginatorSetup } from "@/stores/PaginatorStore";

//#region 初始設置

//設定運行環境
console.log(`APP ENV : ${process.env.VUE_APP_RUN_ENV}`);
if (process.env.VUE_APP_RUN_ENV !== "DEV") {
  console.log("restore mock");
  mock.restore(); //移除mock
} else {
  console.log("regist mock");
}

//初始化Pinia
const pinia = createPinia();

//#endregion



//#region 掛載vue app
//創建
const app = createApp(App);

//安裝套件
app
  .use(pinia)
  .use(MyAuthPlugin)
  //.use(ElementPlus, { locale: element_default_local })
  .use(ElementPlus, { i18n: (key:string, value:string) => i18n.global.t(key, value) })
  .use(router)
  .use(i18n);

//註冊ElementPlus Icons
for (const [key, component] of Object.entries(ElementPlusIconsVue)) {
  app.component(key, component);
}

//註冊font-awesome icon
app.component("font-awesome-icon", FontAwesomeIcon);

//掛載
app.mount("#app");

//#endregion
