import ICreator from '../ICreator';

/** 期數基本資料(Dialog編輯用) */
export default interface IResPeriodInfoDTO {
  /** 期數Id(流水號) */
  id: number;

  /** 期數 */
  periodNum: number;

  /** 建立者 */
  creator: ICreator;
  // /** 建立者UserId */
  // creatorUserId: number;
  
  // /** 建立者帳號名稱 */
  // creatorUsername: string;

  // /** 建立者DisplayName */
  // creatorDisplayName: string;

  /** 建立日期,  格式:yyyy-MM-dd */
  createTime: Date;

  /** 開始月份, 格式:yyyy-MM */
  startMonth: Date;

  /** 結束月份, 格式:yyyy-MM */
  endMonth: Date;

  /** 備註 */
  remark: string;
}
