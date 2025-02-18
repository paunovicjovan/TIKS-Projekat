import {User} from "../User/User.ts";

export interface Comment {
  id: string;
  content: string;
  createdAt: Date;
  author: User;
}