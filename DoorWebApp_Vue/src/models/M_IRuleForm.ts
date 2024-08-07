export interface M_ICreateRuleForm {
  username: string;
  displayName: string;
  email: string;
  phone: string;
  password: string;
  roleid: number;
}

export interface M_IUpdateRuleForm {
  userId: number;
  username: string;
  displayName: string;
  email: string;
  phone: string;
  password: string;
  roleid: number;
}
