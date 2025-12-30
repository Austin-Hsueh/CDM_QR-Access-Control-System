export interface M_ICreateRuleForm {
  username: string;
  displayName: string;
  email: string;
  phone: string;
  password: string;
  roleid: number;
  type:number;
  address: string;
  idcard: string;
  contactPerson:string;
  contactPhone:string;
  relationshipTitle:string;
  splitRatio?: number;

}

export interface M_IUpdateRuleForm {
  userId: number;
  username: string;
  displayName: string;
  email: string;
  phone: string;
  password: string;
  roleid: number;
  type:number;
  address: string;
  idcard: string;
  contactPerson:string;
  contactPhone:string;
  relationshipTitle:string;
  splitRatio?: number;
}

export interface M_IDeleteRuleForm {
  userId: number;
  username: string;
  displayName: string;
  email: string;
  phone: string;
  password: string;
  roleid: number;
  IsDelete: boolean;
}

export interface M_IAddChildRuleForm {
  parentId: number;
  childId: number;
  username: string;
}