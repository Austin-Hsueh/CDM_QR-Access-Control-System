export interface M_IUsers {
  userId: number;
  username: string;
  displayName: string;
  email: string;
  password?: string | null;
  phone: string | null;
  roleId?: number;
  roleName?: string;
  accessTime?: string;
  accessDays?: string;
  permissions?: string | null;
  permissionNames?: string[];
}
