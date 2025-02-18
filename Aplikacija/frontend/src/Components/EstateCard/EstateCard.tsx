import {useNavigate} from "react-router-dom";
import {Estate} from "../../Interfaces/Estate/Estate.ts";
import {useAuth} from "../../Context/useAuth.tsx";
import {deleteEstateAPI} from "../../Services/EstateService.tsx";
import toast from "react-hot-toast";
import styles from './EstateCard.module.css'
import Swal from "sweetalert2";
import {FontAwesomeIcon} from '@fortawesome/react-fontawesome';
import {faHeart} from '@fortawesome/free-solid-svg-icons';
import {useEffect, useState} from "react";
import {addToFavoritesAPI, canAddEstateToFavoriteAPI, removeFromFavoritesAPI} from "../../Services/UserService.tsx";

interface EstateCardProps {
  estate: Estate;
  canDelete?: boolean;
  type: number; // 1 za pretragu, 2 za profil
  loadEstates?: (() => Promise<void>) | null;
  refreshOnDeleteEstate?: ((id: string) => void) | null;
  onRemoveFromFavorite?: (estateId: string) => void;
}

export const EstateCard = ({estate, loadEstates, canDelete = true, type, refreshOnDeleteEstate, onRemoveFromFavorite}: EstateCardProps) => {
  const navigate = useNavigate();
  const {user} = useAuth();

  const [canAddToFavorite, setCanAddToFavorite] = useState(true);
  const [isOwnEstate, setIsOwnEstate] = useState<boolean>(false);

  useEffect(() => {
    checkIfCanAddToFavorite();

    if(estate.userId) {
      setIsOwnEstate(estate.userId == user?.id);
    }
    else if(estate.user) {
      setIsOwnEstate(estate.user.id == user?.id);
    }
    else
      setIsOwnEstate(false);
  }, [])

  const checkIfCanAddToFavorite = async () => {
    try {
      const response = await canAddEstateToFavoriteAPI(estate.id!);
      if (response?.status == 200) {
        setCanAddToFavorite(response.data);
      }
    } catch {
      toast.error("Došlo je do greške prilikom određivanja da li korisnik može da doda nekretninu u omiljene.");
    }
  }

  const confirmEstateDeletion = async () => {
    Swal.fire({
      title: "Da li sigurno želite da obrišete nekretninu?",
      text: "Uz nekretninu će biti obrisani i sve objave vezane za nju!",
      icon: "warning",
      position: "top",
      showCancelButton: true,
      confirmButtonColor: "#8cc4da",
      cancelButtonColor: "#d33",
      cancelButtonText: "Otkaži",
      confirmButtonText: "Obriši"
    }).then(async (result) => {
      if (result.isConfirmed) {
        await handleDelete();
      }
    });
  }

  const handleDelete = async () => {
    const response = await deleteEstateAPI(estate.id);
    if (response) {
      toast.success("Nekretnina uspešno obrisana.");
      if (type == 1)
        await loadEstates!();
      else if (type == 2)
        refreshOnDeleteEstate!(estate.id);
    }
  };

  const handleChange = () => {
    navigate(`/estate-page/${estate.id}`, {state: {setEdit: true}});
  };

  const handleNavigate = () => {
    navigate(`/estate-details/${estate.id}`);
  };

  const handleAddToFavorite = async () => {
    try {
      const response = await addToFavoritesAPI(estate!.id);
      if (response?.status === 200 && response.data) {
        setCanAddToFavorite(false);
        toast.success("Nekretnina je dodata u omiljene!");
      }
    } catch {
      toast.error("Došlo je do greške prilikom dodavanja u omiljene.");
    }
  };

  const handleRemoveFromFavorite = async () => {
    try {
      const response = await removeFromFavoritesAPI(estate!.id);
      if (response?.status === 200 && response.data) {
        setCanAddToFavorite(true);
        toast.success("Nekretnina je uklonjena iz omiljenih!");
        onRemoveFromFavorite?.(estate!.id);
      }
    } catch {
      toast.error("Došlo je do greške prilikom uklanjanja nekretnine iz omiljenih.");
    }
  };

  return (
    <div className={`card my-2 p-2 shadow`} style={{width: "18rem"}}>
      <img
        src={`${import.meta.env.VITE_SERVER_URL}/${estate.images[0]}`}
        className={`card-img-top`}
        alt={estate.title}
      />
      <div className={`card-body`}>
        <h5 className={`text-blue`}>{estate.title}</h5>
        <p className={`text-golden`}>{estate.price}&nbsp;€</p>
        <button className={`btn btn-sm text-white text-center rounded py-2 px-2 ${styles.dugme} ${styles.slova}`}
                onClick={handleNavigate}>
          Pogledaj Detalje
        </button>
        { !isOwnEstate &&
          (
            canAddToFavorite ? (<>
                <button className={`btn btn-outline-danger ms-2`} onClick={handleAddToFavorite}>
                  <FontAwesomeIcon icon={faHeart}/>
                </button>
              </>) :
              (<>
                <button className={`btn btn-danger ms-2`} onClick={handleRemoveFromFavorite}>
                  <FontAwesomeIcon icon={faHeart}/>
                </button>
              </>)
          )}
        {isOwnEstate && (
          <>
            <div className={`mt-2`}>
              {canDelete &&
                <button
                  className={`btn btn-sm text-white text-center rounded py-2 px-2 me-2 ${styles.dugme1} ${styles.slova}`}
                  onClick={confirmEstateDeletion}>
                  Obriši
                </button>
              }
              <button className={`btn btn-sm text-gray text-center rounded py-2 px-2 ${styles.dugme2} ${styles.slova}`}
                      onClick={handleChange}>
                Izmeni
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default EstateCard;
