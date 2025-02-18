import { EstateCategory } from "../../Enums/EstateCategory.ts";
import {User} from "../User/User.ts";

export interface Estate {
  id: string;
  title: string;
  description: string;
  price: number;
  squareMeters : number;
  totalRooms : number;
  category : EstateCategory;
  floorNumber ?: number;
  images : string[];
  longitude : number;
  latitude : number;
  userId:string;
  user: User | null;
}