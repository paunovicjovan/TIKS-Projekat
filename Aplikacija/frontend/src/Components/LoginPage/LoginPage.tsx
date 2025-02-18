import styles from "./LoginPage.module.css";
import {Link} from "react-router-dom";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faEye, faEyeSlash} from "@fortawesome/free-solid-svg-icons";
import {ChangeEvent, useState} from 'react';
import {useAuth} from "../../Context/useAuth";
import toast from "react-hot-toast";

export const LoginPage = () => {
  const [email, setEmail] = useState<string>('');
  const [password, setPassword] = useState<string>('');
  const [passwordVisible, setPasswordVisible] = useState<boolean>(false);

  const {loginUser} = useAuth();

  const handleLogin = async () => {

    try {
      if (!(email.trim()) || !password) {
        toast.error("Niste uneli e-mail i lozinku.");
        return;
      }
      await loginUser(email, password);
    } catch (error: any) {
      toast.error(error.response.data);
    }
  };

  const handleEmailChange = (e: ChangeEvent<HTMLInputElement>) => {
    setEmail(e.target.value);
  };

  const handlePasswordChange = (e: ChangeEvent<HTMLInputElement>) => {
    setPassword(e.target.value);
  };

  const handlePasswordVisible = (state: boolean) => {
    setPasswordVisible(state);
  }

  return (
    <div className={`container-fluid bg-gray d-flex justify-content-center flex-grow-1`}>
      <div className={`col-xxl-7 col-xl-7 col-lg-6 col-md-10 col-sm-12 p-5 m-4 bg-light rounded d-flex flex-column`}>
        <div className={`m-4`}></div>
        <h4 className={`text-center text-blue`}>Prijavite Se</h4>
        <h6 className={`text-golden text-center mb-3`}>Dobrodošli nazad!</h6>
        <div className={`form-floating mb-2 mt-2`}>
          <input type="email" className={`form-control ${styles.fields}`} id="email" placeholder="Unesite e-mail"
                 name="email" value={email} onChange={handleEmailChange} required/>
          <label htmlFor="email" className={`${styles.input_placeholder}`}>Unesite e-mail</label>
        </div>
        <div className={`form-floating mb-2 mt-2`}>
          <input type={passwordVisible ? "text" : "password"} className={`form-control ${styles.fields}`} id="password"
                 placeholder="Unesite lozinku" name="password" value={password} onChange={handlePasswordChange}
                 required/>
          <label htmlFor="password" className={`${styles.input_placeholder}`}>Unesite lozinku</label>
          {passwordVisible ?
            <FontAwesomeIcon icon={faEyeSlash} className={`${styles.password_eye}`}
                             onClick={() => handlePasswordVisible(false)}/> :
            <FontAwesomeIcon icon={faEye} className={`${styles.password_eye}`}
                             onClick={() => handlePasswordVisible(true)}/>}
        </div>
        <button className={`mt-5 rounded-3 bg-gray p-3 mt-2 border-0 text-light ${styles.dugme}`}
                onClick={handleLogin}>Prijavite Se
        </button>
        <p className={`text-blue mt-2 mb-6 text-center`}>
          Nemate nalog?&nbsp;
          <Link className={`text-golden text-decoration-none`} to="/register">Registrujte se.</Link>
        </p>
      </div>
    </div>
  );
};