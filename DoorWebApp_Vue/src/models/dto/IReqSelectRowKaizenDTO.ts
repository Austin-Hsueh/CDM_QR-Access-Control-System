export default interface IReqSelectRowKaizenDTO {
  id: number;                     
  codePJ: string;                   //專案代碼
  team: string;                     //小組
  kaizenPN: string;                 //改善料號
  kaizen: string;                   //改善對策
  jobNumber: string;                //作業編號
  kaizenPlan: string;               //改善方案
  kaizenContent: string;            //對策內容
  originPPH: string;                //原PPH
  kaizenBeforeWorkHours: number;    //改善前工時
  KaizenAfterWorkHours: number;     //改善後工時
  createDate: string;               //建立時間
  manufactureMethodId: number;      //工藝類別Id
  applicant: string;                //申請者       
}
  