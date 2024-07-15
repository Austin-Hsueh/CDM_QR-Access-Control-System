import IFactory from "../IFactory";

export default interface IResSiteInfoDTO {
  id: number;
  name: string;
  remark: string;
  factories: IFactory[];
}
