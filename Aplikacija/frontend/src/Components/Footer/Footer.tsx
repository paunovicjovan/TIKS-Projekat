import {Link} from "react-router-dom";
import styles from './Footer.module.css'

export const Footer = () => {
    const scrollToTop = () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    };

    return (
        <div
            className={`footer container-fluid py-2 text-center text-blue bg-sand`}
        >
            <div className={`container navbar navbar-expand-lg justify-content-center`}>
                <ul className={`navbar-nav mt-2 mb-1`}>
                    <li className={`nav-item`}>
                        <a href="#onama" className={`text-blue mx-2 text-decoration-none`} onClick={scrollToTop}>
                            O NAMA
                        </a>
                    </li>
                    <li className={`nav-item`}>
                        <Link to="/search-estates" className={`text-blue mx-2 text-decoration-none`}
                              onClick={scrollToTop}>
                            NEKRETNINE
                        </Link>
                    </li>
                    <li className={`nav-item`}>
                        <Link to="/login" className={`text-blue mx-2 text-decoration-none`} onClick={scrollToTop}>
                            PRIJAVA
                        </Link>
                    </li>
                    <li className={`nav-item`}>
                        <Link to="/register" className={`text-blue mx-2 text-decoration-none`} onClick={scrollToTop}>
                            REGISTRACIJA
                        </Link>
                    </li>
                </ul>
            </div>

            <hr className={`text-text-blue`}/>
            <p className={`text-center pt-2`}>&copy;Domovida 2025</p>
        </div>
    );
};