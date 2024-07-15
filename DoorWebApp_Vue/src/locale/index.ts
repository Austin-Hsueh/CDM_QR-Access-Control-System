import { castToVueI18n, createI18n } from "vue-i18n";
import * as zh_tw from "@/locale/lang/zh-TW.json";
import * as zh_cn from "@/locale/lang/zh-CN.json";
import * as en_us from "@/locale/lang/en-US.json";

//設置語言
const messages = {
  en_us,
  zh_tw,
  zh_cn,
};

//const DefaultLanguage = localStorage.getItem("locale") || navigator.language.toLowerCase().replace("-", "_");
const DefaultLanguage = localStorage.getItem("locale") || 'en_us';
localStorage.setItem("locale", DefaultLanguage);
//console.log(`Default Languale : ${DefaultLanguage}`);


const i18n = createI18n({
  legacy: false,
  locale: DefaultLanguage,
  fallbackLocale: "en_us",
  messages: messages,
});

export default i18n;