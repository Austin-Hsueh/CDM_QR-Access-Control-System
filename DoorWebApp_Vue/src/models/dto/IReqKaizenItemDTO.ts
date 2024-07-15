export default interface IReqKaizenItemDTO {
  /** 專案代碼 */
  projectCode: string;

  /** 改善料號 */
  pn: string;

  /** 客戶名稱 */
  customerName: string;

  /** 作業編號 */
  workNum: number;

  /** 工藝類別Id */
  manufMethodId: number;


  /** 改善方案 */
  impPlan: string;

  /** 改善對策 */
  impMethod: string;

  /** 對策內容 */
  impMethodDetail: string;
  
  /** 工時調整日期(yyyy-MM-dd) */
  implementDay: string;

  /** 超連結 */
  hyperlink?: string;

  
  // /** 改善前工時(秒) */
  // procTimeBeforeImp: number;

  /** 改善後工時(秒) */
  procTimeAfterImp: number;

  // /** 改善前面積(平方公尺) */
  // areaBeforeImp?: number;

  // /** 改善前製程L/T */
  // procLTBeforeImp?:number;

  // /** 改善前供應商L/T */
  // venderLTBeforeImp?: number;

  // /** 改善前PPH */
  // pphBeforeImp?: number;


  
  // /** 改善後面積(平方公尺) */
  // areaAfterImp?: number;

  // /** 改善後供應商L/T */
  // venderLTAfterImp?: number;

  // /** 改善後製程L/T */
  // procLTAfterImp?:number;

  // /** 改善後PPH */
  // pphAfterImp?: number;

}