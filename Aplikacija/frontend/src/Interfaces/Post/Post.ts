import {User} from "../User/User.ts";
import {Estate} from "../Estate/Estate.ts";

export interface Post {
  id: string;
  title: string;
  content: string;
  createdAt: Date;
  author: User;
  estate: Estate | null;
}