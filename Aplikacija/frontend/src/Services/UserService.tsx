import axios from "axios";
import {AuthResponseDTO} from "../Interfaces/User/AuthResponseDTO.ts";
import toast from "react-hot-toast";
import {User} from "../Interfaces/User/User.ts";

const apiUrl = `${import.meta.env.VITE_API_URL}/User`;

export const loginAPI = async (email: string, password: string) => {
  try {
    return await axios.post<AuthResponseDTO>(apiUrl + "/Login", {
      email,
      password
    });
  } catch (error: any) {
    toast.error(error.response?.data ?? "Neuspešna prijava.");
    return undefined;
  }
}

export const registerAPI = async (email: string, username: string, password: string, phoneNumber: string) => {
  try {
    return await axios.post<AuthResponseDTO>(apiUrl + "/Register", {
      email,
      username,
      password,
      phoneNumber
    });
  } catch (error: any) {
    toast.error(error.response?.data ?? "Neuspešna registracija.");
    return undefined;
  }
}

export const getUserByIdAPI = async (id: string) => {
  try {
    return await axios.get<User>(`${apiUrl}/GetUserById/${id}`);
  } catch (error: any) {
    toast.error(error.response?.data ?? "Neuspešno učitavanja podataka o korisniku.");
    return undefined;
  }
}

export const updateUserAPI = async (newUsername: string, newPhoneNumber: string) => {
  try {
    return await axios.put<User>(`${apiUrl}/Update`, {
      username: newUsername,
      phoneNumber: newPhoneNumber
    });
  } catch (error: any) {
    toast.error(error.response?.data ?? "Neuspešno ažuriranje podataka o korisniku.");
    return undefined;
  }
}

export const addToFavoritesAPI = async (estateId: string) => {
  try {
    return await axios.post<boolean>(`${apiUrl}/AddToFavorites/${estateId}`);
  } catch (error: any) {
    toast.error(error?.response?.data || "Došlo je do greške prilikom dodavanja nekretnine u omiljene.");
    return undefined;
  }
};

export const removeFromFavoritesAPI = async (estateId: string) => {
  try {
    return await axios.delete<boolean>(`${apiUrl}/RemoveFromFavorites/${estateId}`);
  } catch (error: any) {
    toast.error(error?.response?.data || "Došlo je do greške prilikom uklanjanja nekretnine iz omiljenih.");
    return undefined;
  }
};

export const canAddEstateToFavoriteAPI = async (estateId: string) => {
  try {
    return await axios.get<boolean>(`${apiUrl}/CanAddToFavorite/${estateId}`);
  } catch (error: any) {
    toast.error(error?.response?.data || "Došlo je do greške prilikom određivanja da li je moguće dodavanje nekretnine u omiljene.");
    return undefined;
  }
};