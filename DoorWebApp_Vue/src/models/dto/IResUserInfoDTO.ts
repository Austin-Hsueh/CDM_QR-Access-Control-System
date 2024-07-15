import IRole from "@/models/IRole";

export default interface IResUserInfoDTO {
  userId: number,
  username: string,
  displayName: string,
  email: string,
  lastLoginTime: string,
  roles: IRole[],
}