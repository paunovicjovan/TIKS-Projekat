import {UserRole} from "../../Enums/UserRole.ts";

export interface User {
  id: string;
  username: string;
  email: string;
  phoneNumber: string;
  role: UserRole;
}