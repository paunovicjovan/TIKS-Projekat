import { useLocation, useNavigate } from "react-router";
import React, { createContext, useEffect, useState } from "react";
import axios from "axios";
import { User } from "../Interfaces/User/User";
import { loginAPI, registerAPI } from "../Services/UserService";
import { UserRole } from "../Enums/UserRole";
import {AuthResponseDTO} from "../Interfaces/User/AuthResponseDTO.ts";

type UserContextType = {
  user: User | null;
  token: string | null;
  registerUser: (email: string, username: string, password: string, phoneNumber: string) => void;
  loginUser: (email: string, password: string) => void;
  logout: () => void;
  isLoggedIn: () => boolean;
};

type Props = {
  children: React.ReactNode;
};

export const UserContext = createContext<UserContextType>({} as UserContextType);

export const UserProvider = ({ children }: Props) => {
  const navigate = useNavigate();
  const location = useLocation();
  const from = location.state?.from?.pathname || '/';
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<User | null>(null);
  const [isReady, setIsReady] = useState<boolean>(false);

  useEffect(() => {
    const user = localStorage.getItem("user");
    const token = localStorage.getItem("token");
    if (user && token) {
      setUser(JSON.parse(user));
      setToken(token);
      axios.defaults.headers.common["Authorization"] = "Bearer " + localStorage.getItem("token");
    }
    setIsReady(true);
  }, []);

  const storeUserData = (authResponse: AuthResponseDTO) => {
    localStorage.setItem("token", authResponse.token);
    const user: User = {
      id: authResponse.id,
      username: authResponse.username,
      email: authResponse.email,
      phoneNumber: authResponse.phoneNumber,
      role: authResponse.role as UserRole
    };

    localStorage.setItem("user", JSON.stringify(user));
    axios.defaults.headers.common["Authorization"] = "Bearer " + localStorage.getItem("token");

    setToken(token);
    setUser(user!);

    navigate(from, { replace: true });
  }

  const registerUser = async (email: string, username: string, password: string, phoneNumber: string) => {
    await registerAPI(email, username, password, phoneNumber)
      .then((res) => {
        if (res) {
          storeUserData(res.data);
        }
      })
      .catch((e) => console.error(e));
  };

  const loginUser = async (username: string, password: string) => {
    await loginAPI(username, password)
      .then((res) => {
        if (res) {
          storeUserData(res.data);
        }
      })
      .catch((e) => console.error(e));
  };

  const isLoggedIn = () => {
    return !!user;
  };

  const logout = () => {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
    setUser(null);
    setToken("");
    navigate("/");
  };

  return (
    <UserContext.Provider
      value={{ loginUser, user, token, logout, isLoggedIn, registerUser }}
    >
      {isReady ? children : null}
    </UserContext.Provider>
  );
};

export const useAuth = () => React.useContext(UserContext);