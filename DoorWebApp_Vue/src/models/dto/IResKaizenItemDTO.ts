export default interface IResKaizenItemDTO {
  /** 改善事項Id */
  id: number;

  /** 專案代碼 */
  projectCode: string;

  /** 改善事項代碼 */
  kaizenCode: string;

  /** 改善料號 */
  pn: string;

  /** 小組編號 */
  teamNum: string;

  /** 作業編號 */
  workNum: string;

  /** 作業說明 */
  workRemark: string;

  /** 工藝類別: Id */
  manufMethodId: number;

  /** 工藝類別: 名稱  */
  manufMethodName: string;

  /** 改善方案 */
  impPlan: string;

  /** 改善對策 */
  impMethod: string;

  /** 對策內容 */
  impMethodDetail: string;

  /** 工時調整日期 (yyyy-MM-dd) */
  implementDay: string;

  /** 超連結 */
  hyperlink: string;

  /** 改善前工時(秒) */
  procTimeBeforeImp: string;

  /**  改善後工時(秒), 來自ERP資料 */
  procTimeAfterImp: string;

  // /** 改善前面積(平方公尺) */
  // areaBeforeImp: string;

  // /** 改善後面積(平方公尺) */
  // areaAfterImp: string;

  // /** 改善前供應商L/T(日) */
  // venderLTBeforeImp: string;

  // /** 改善後供應商L/T(日) */
  // venderLTAfterImp: string;

  // /** 改善前製程L/T(日) */
  // procLTBeforeImp: string;

  // /** 改善後製程L/T(日) */
  // procLTAfterImp: string;

  // /** 改善前PPH(個) */
  // pphBeforeImp: string;

  // /** 改善後PPH(個) */
  // pphAfterImp: string;

  /** 建立者: 使用者Id */
  creatorUserId: number;

  /** 建立者:顯示名稱 */
  creatorDisplayName: string;

  /** 建立時間 (yyyy-MM-dd HH:mm:ss) */
  createTime: string;
}
