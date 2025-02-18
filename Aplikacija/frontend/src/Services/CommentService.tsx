import axios from "axios";
import toast from "react-hot-toast";
import {Comment} from "../Interfaces/Comment/Comment.ts";
import {PaginatedResponseDTO} from "../Interfaces/Pagination/PaginatedResponseDTO.ts";

const apiUrl = `${import.meta.env.VITE_API_URL}/Comment`;

export const createCommentAPI = async (content: string, postId: string) => {
  try {
    return await axios.post<Comment>(`${apiUrl}/Create`, {content, postId});
  } catch (error: any) {
    toast.error(error.response?.data ?? "Greška pri kreiranju komentara.");
    return undefined;
  }
};

export const getCommentsForPostAPI = async (postId: string, skip: number = 0, limit: number = 10) => {
  try {
    return await axios.get<PaginatedResponseDTO<Comment>>(`${apiUrl}/GetCommentsForPost/${postId}`, {
      params: { skip, limit },
    });
  } catch (error: any) {
    toast.error(error.response?.data ?? "Greška pri učitavanju komentara.");
    return undefined;
  }
};

export const updateCommentAPI = async (commentId: string, content: string) => {
  try {
    return await axios.put<Comment>(`${apiUrl}/Update/${commentId}`, { content });
  } catch (error: any) {
    toast.error(error.response?.data ?? "Greška pri ažuriranju komentara.");
    return undefined;
  }
};

export const deleteCommentAPI = async (commentId: string) => {
  try {
    return await axios.delete(`${apiUrl}/Delete/${commentId}`);
  } catch (error: any) {
    toast.error(error.response?.data ?? "Greška pri brisanju komentara.");
    return undefined;
  }
};
