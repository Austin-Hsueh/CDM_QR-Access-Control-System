import ICreator from '@/models/ICreator';
import IFactory from '@/models/IFactory';
import ISite from '@/models/ISite';
import IResPeriodInfoDTO from '@/models/dto/IResPeriodInfoDTO';


export default interface IResProjectDetailDTO {
  id: number;
  pjCode: string;
  period: IResPeriodInfoDTO;
  site: ISite,
  factory: IFactory,
  teamNum: number;
  createTime: Date;
  creator: ICreator;
  workRate: number;
  remark: string;
}