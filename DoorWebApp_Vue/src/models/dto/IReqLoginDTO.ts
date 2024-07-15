import { LocaleType } from "../enums/LocaleType";

export default interface IReqLoginDTO {
  username: string;
  password: string;
  locale: LocaleType,
  isKeepLogin: boolean
}
