import { Language } from "element-plus/es/locale";
import { LocaleType } from "./enums/LocaleType";

export default interface IUserInfo {
  token: string;
  userId: number;
  username: string;
  displayName: string;
  locale: LocaleType;
  el_local?: Language;
  permissions: number[];
  qrcode: string;
  roleId: number;
}
