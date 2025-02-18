import styles from "./Home.module.css";
import { Link } from "react-router-dom";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { useState, useEffect } from 'react';
import { faHouseChimney, faPeopleArrows, faPlus } from "@fortawesome/free-solid-svg-icons";
import pocetna from "../../Assets/pocetna.jpg";

export const Home = () => {

    const [showButton, setShowButton] = useState(false);

    useEffect(() => {
        window.addEventListener("scroll", handleScroll);
        return () => {
          window.removeEventListener("scroll", handleScroll);
        };
      }, []);

    const handleScroll = () => {
        if (window.scrollY > 20) {
          setShowButton(true);
        } else {
          setShowButton(false);
        }
      };

    const scrollToTop = () => {
        window.scrollTo({top: 0, behavior: "smooth"});
    };

    return (
        <div className={`container-fluid p-0 bg-beige`}>
            <div className={`position-relative w-100`}>
                <img src={pocetna} alt="pocetna slika" className={`w-100 vh-100 object-fit-cover position-absolute top-0 start-0`} style={{ filter: "brightness(70%)" }}/>
                
                <div className={`container position-relative d-flex flex-column align-items-start justify-content-center text-white`} style={{ minHeight: "100vh" }}>
                    <div className={`col-xxl-4 col-xl-5 col-lg-6 col-md-8 text-start ms-lg-5 mt-n5`}>
                        <h1 className={`text-beige fw-bold mb-3 mt-n5 ${styles.senka}`}>Tvoj idealan dom čeka na tebe!</h1>
                        <p className={`lead text-beige fw-normal mb-4 ${styles.senka}`}>
                            Bilo da tražiš savršen stan u centru grada, mirnu kuću na periferiji ili 
                            komercijalni prostor za tvoj biznis, tu smo da ti pomognemo u pronalaženju nekretnine iz snova.
                        </p>
                        <Link
                            to="/search-estates"
                            className={`btn btn-lg text-white rounded shadow-lg py-3 px-4 ${styles.slova1} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}>
                            Pronađi Nekretninu
                        </Link>
                    </div>
                </div>
            </div>

            <div className={`container my-5 d-flex justify-content-center`}>
                <div className={`row justify-content-center text-center mb-5`}>
                    <div className={`col-md-3 mt-5`}>
                        <FontAwesomeIcon icon={faHouseChimney} className={`text-light-blue fs-1`} />            
                        <h5 className={`mt-3 text-golden`}>Ogroman broj ponuda</h5>
                        <p className={`text-blue`}>Pregledaj najnovije i najatraktivnije nekretnine koje odgovaraju tvojim željama.</p>
                        <Link
                                to="/search-estates"
                                className={`btn-lg text-white text-center rounded py-2 px-2 ${styles.slova} ${styles.dugme2} ${styles.linija_ispod_dugmeta}`}
                            >
                                Pregledaj Nekretnine
                        </Link>
                    </div>
                    <div className={`col-md-3 mt-5`}>
                        <FontAwesomeIcon icon={faPlus} className={`text-light-blue fs-1`} />
                        <h5 className={`mt-3 text-golden`}>Jednostavno postavljanje oglasa</h5>
                        <p className={`text-blue`}>Kreirajte oglas sa svim potrebnim informacijama o nekretnini.</p>
                        <Link
                                to="/create-estate"
                                className={`btn-lg text-white text-center rounded py-2 px-2 ${styles.slova} ${styles.dugme2} ${styles.linija_ispod_dugmeta}`}
                            >
                                Kreiraj Nekretninu
                        </Link>
                    </div>
                    <div className={`col-md-3 mt-5`}>
                        <FontAwesomeIcon icon={faPeopleArrows} className={`text-light-blue fs-1`} />
                        <h5 className={`mt-3 text-golden`}>Poboljšajte našu uslugu</h5>
                        <p className={`text-blue`}>Svaka povratna informacija nam pomaže da postanemo bolji!</p>
                        <Link
                                to="/forum"
                                className={`btn-lg text-white text-center rounded py-2 px-2 ${styles.slova} ${styles.dugme2} ${styles.linija_ispod_dugmeta}`}
                            >
                                Podeli Mišljenje
                        </Link>
                    </div>
                </div>
            </div>

            <hr className={`text-golden mx-5`}></hr>

            <div className={`text-center bg-light-yellow py-5`}>
                <h2 className={`text-light-blue`}>Počni svoju potragu odmah!</h2>
                <p className={`text-blue`}>Registrujte se i pronađite savršene nekretnine već danas!</p>
                <Link to="/register" className={`btn-lg text-white text-center rounded py-2 px-2 ${styles.slova} ${styles.dugme3} ${styles.linija_ispod_dugmeta}`}>Registruj Se</Link>
            </div>

            <button onClick={scrollToTop} className={`bg-blue text-white ${styles.pocetak} ${showButton ? 'd-block' : 'd-none'}`} title="Idi na pocetak">^</button>
        </div>
    );
}

export default Home;