import toast from "react-hot-toast";
import {ChangeEvent, useState} from "react";
import {useAuth} from "../../Context/useAuth.tsx";
import styles from './RegisterPage.module.css'
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faEye, faEyeSlash, faPhone} from "@fortawesome/free-solid-svg-icons";
import {Link} from "react-router-dom";

export const RegisterPage = () => {
  const [email, setEmail] = useState<string>("");
  const [username, setUsername] = useState<string>("");
  const [password, setPassword] = useState<string>("");
  const [phoneNumber, setPhoneNumber] = useState<string>("");

  const [passwordVisible, setPasswordVisible] = useState<boolean>(false);

  const { registerUser } = useAuth();

  const handleUsernameChange = (e: ChangeEvent<HTMLInputElement>) => {
    setUsername(e.target.value);
  };

  const handleEmailChange = (e: ChangeEvent<HTMLInputElement>) => {
    setEmail(e.target.value);
  };

  const handlePhoneNumberChange = (e: ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/[^0-9+ ]/g, "");
    setPhoneNumber(value);
  };

  const handlePasswordChange = (e: ChangeEvent<HTMLInputElement>) => {
    setPassword(e.target.value);
  };

  const handlePasswordVisible = (state: boolean) => {
    setPasswordVisible(state);
  }

  const handleRegister = async () => {
    const trimmedEmail = email.trim();
    const trimmedUsername = username.trim();
    const trimmedPhoneNumber = phoneNumber.trim();
    setEmail(trimmedEmail);
    setUsername(trimmedUsername);
    setPhoneNumber(trimmedPhoneNumber);

    if (!trimmedEmail || !password || !trimmedUsername || !trimmedPhoneNumber) {
      toast.error("Niste popunili sva polja.")
      return;
    }

    const usernameRegex = /^[a-zA-Z0-9._]+$/;
    if(!usernameRegex.test(trimmedUsername)){
      toast.error("Korisničko ime nije u validnom formatu. Dozvoljena su mala i velika slova abecede, brojevi, _ i .");
      return;
    }

    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if(!emailRegex.test(email)){
      toast.error("Uneti e-mail nije validan.");
      return;
    }

    const passwordRegex = /^(?=.*\d)(?=.*[!@#-/+$%^&*(),.?":{}|<>])(?=.*[A-Z]).{8,}$/;
    if(!passwordRegex.test(password)){
      toast.error("Lozinka mora da bude dužine barem 8 karaktera, da sadrži cifru, specijalni znak i veliko slovo.");
      return;
    }

    try {
      await registerUser(email, username, password, phoneNumber);
    } catch (error) {
      console.error(error);
    }
  };

  return (
    <div className={`container-fluid bg-gray d-flex justify-content-center flex-grow-1`}>
      <div className={`col-xxl-7 col-xl-7 col-lg-6 col-md-10 col-sm-12 p-5 m-4 bg-light rounded d-flex flex-column`}>
        <h4 className={`mt-5 text-center text-blue`}>Registrujte Se</h4>
        <h6 className={`text-center mb-3 text-golden`}>Dobrodošli!</h6>
        <div className={`form-floating mb-2 mt-2`}>
          <input
            type="text"
            className={`form-control ${styles.fields}`}
            id="username"
            placeholder="Unesite korisničko ime"
            onChange={handleUsernameChange}
            name="username"
            value={username}
            required
          />
          <label htmlFor="username" className={`${styles.input_placeholder}`}>
            Unesite korisničko ime
          </label>
        </div>
        <div className="input-group mb-2 mt-2">
        <span className={`input-group-text ${styles.fields}`}>
          <FontAwesomeIcon icon={faPhone} />
        </span>
          <div className="form-floating">
            <input
              type="tel"
              className={`form-control ${styles.fields}`}
              id="phoneNumber"
              placeholder="Unesite broj telefona"
              onChange={handlePhoneNumberChange}
              name="phoneNumber"
              value={phoneNumber}
              required
              inputMode="numeric"
              pattern="^\+?[0-9]{9,15}$"
              maxLength={15}
              minLength={9}
            />
            <label htmlFor="phoneNumber" className={`${styles.input_placeholder}`}>
              Unesite broj telefona
            </label>
          </div>
        </div>
        <div className={`form-floating mb-2 mt-2`}>
          <input
            type="email"
            className={`form-control ${styles.fields}`}
            id="email"
            placeholder="Unesite e-mail"
            onChange={handleEmailChange}
            name="email"
            value={email}
            required
          />
          <label htmlFor="email" className={`${styles.input_placeholder}`}>
            Unesite e-mail
          </label>
        </div>
        <div className={`form-floating mb-2 mt-2`}>
          <input
            type={passwordVisible ? "text" : "password"}
            className={`form-control ${styles.fields}`}
            id="password"
            placeholder="Unesite lozinku"
            onChange={handlePasswordChange}
            name="password"
            value={password}
            required
          />
          <label htmlFor="password" className={`${styles.input_placeholder}`}>
            Unesite lozinku
          </label>
          {passwordVisible ?
            <FontAwesomeIcon icon={faEyeSlash} className={styles.password_eye} onClick={() => handlePasswordVisible(false)} /> :
            <FontAwesomeIcon icon={faEye} className={styles.password_eye} onClick={() => handlePasswordVisible(true)} />}
        </div>
        <button
          className={`rounded-3 p-3 mt-2 border-0 text-light ${styles.dugme}`}
          onClick={handleRegister}
        >
          Registrujte Se
        </button>
        <p className={`text-blue mt-2 text-center`}>
          Imate nalog?&nbsp;
          <Link
            className={`text-golden text-decoration-none`}
            to="/login"
          >
            Prijavite se.
          </Link>
        </p>
      </div>
    </div>
  );
};