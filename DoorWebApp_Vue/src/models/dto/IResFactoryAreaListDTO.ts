
import IResFactoryAreaDTO from "@/models/dto/IResFactoryAreaDTO";
/** 下拉選單 廠區 列表 */ 
export default interface IResFactoryAreaListDTO {
  totalAmount:number; 
  totalPage:number;
  factoryAreaList: IResFactoryAreaDTO[];
}
