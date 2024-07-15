export default interface IResRoleInfoDTO {
  roleId: number;
  name: string;
  creatorUserId: number;
  creatorDisplayName: string;
  description: string;
  isEnable: boolean;
  permissionIds: number[];
  createTime: string;
}
