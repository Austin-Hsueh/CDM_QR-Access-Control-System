import IFactory from "./IFactory";

export default interface ISite {
  id: number;
  name: string;
  remark: string;
  factories: IFactory[]
}