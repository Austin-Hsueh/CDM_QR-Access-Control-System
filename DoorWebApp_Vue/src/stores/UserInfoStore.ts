import IUserInfo from '@/models/IUserInfo';
import { defineStore } from 'pinia'
import i18n from "@/locale";
import element_zh_TW from "element-plus/es/locale/lang/zh-tw";
import element_zh_CN from "element-plus/es/locale/lang/zh-cn";
import element_en_US from "element-plus/es/locale/lang/en";
import { LocaleType } from "@/models/enums/LocaleType";

export const useUserInfoStore = defineStore('userInfo', {
  state: (): IUserInfo => {
    return {
      token: '',
      userId: 0,
      username: '',
      displayName: '',
      locale: LocaleType.en_us,
      el_local: undefined,
      permissions: [],
      qrcode: '',
      roleId: 1,
    }
  },
  getters: {
  
  },
  actions: {
    setLocale(locale: LocaleType) {
      this.locale = locale;

      localStorage.setItem('locale', locale);
      i18n.global.locale.value = locale;

      switch (locale) {
        case LocaleType.en_us:
          this.el_local = element_en_US;
          break;
        case LocaleType.zh_tw:
          this.el_local = element_zh_TW;
          break;
        case LocaleType.zh_cn:
          this.el_local = element_zh_CN;
          break;
        default:
          this.el_local = element_en_US;
          break;
      }
    },
    getLocale(): LocaleType {
      const userLocaleStr = localStorage.getItem("locale");

      let userLocaleType = LocaleType[userLocaleStr as keyof typeof LocaleType];
      if(!userLocaleStr || !userLocaleType) {
        userLocaleType = LocaleType.en_us
        this.setLocale(userLocaleType);
      }

      return userLocaleType;
    },
    clearToken() {
      localStorage.removeItem('accessToken');
    },
    setToken(token:string, IsSaveToLocalStorage: boolean) {
      this.token = token;
      if(IsSaveToLocalStorage) {
        localStorage.setItem('accessToken', token);
      } 
    },
    setQRcode(QRcode:string){
      this.qrcode = QRcode;
      localStorage.setItem('QRcode', QRcode);
    }
  }
})
