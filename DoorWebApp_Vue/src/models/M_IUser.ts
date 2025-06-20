export interface M_IUsers {
  userId: number;
  username: string;
  displayName: string;
  email: string;
  password?: string | null;
  phone: string | null;
  roleId: number;
  roleName?: string;
  accessTime?: string;
  accessDays?: string;
  groupNames: string[];
  groupIds: number[];
  datefrom: string;
  dateto: string;
  timefrom: string;
  timeto: string;
  days: string;
  type: number;
  address: string;
  idcard: string;
}

export interface M_IUsersContent {
  totalItems: number;
  totalPages: number;
  pageSize: number;
  pageItems: M_IUsers[];
}
