import {useEffect, useState} from "react";
import {useAuth} from "../../Context/useAuth";
import {Estate} from "../../Interfaces/Estate/Estate";
import {getFavoriteEstatesForUserAPI} from "../../Services/EstateService";
import EstateCard from "../EstateCard/EstateCard";
import {Pagination} from "../Pagination/Pagination.tsx";

export const FavoriteEstates = () => {
  const {user} = useAuth();
  const [favoriteEstates, setFavoriteEstates] = useState<Estate[]>([]);
  const [totalEstatesCount, setTotalEstatesCount] = useState<number>(0);

  useEffect(() => {
    if (user) {
      fetchEstates(user.id, 1, 10);
    }
  }, [user]);

  const handlePaginateChange = async (page: number, pageSize: number) => {
    await fetchEstates(user!.id, page, pageSize);
  }

  const fetchEstates = async (userId: string, page: number, pageSize: number) => {
    const favoriteEstates = await getFavoriteEstatesForUserAPI(userId, page, pageSize);
    setFavoriteEstates(favoriteEstates?.data ?? []);
    setTotalEstatesCount(favoriteEstates?.totalLength ?? 0);
  }

  const handleRemoveFromFavorite = async (estateId: string) => {
    setFavoriteEstates(prev => prev.filter(estate => estate.id !== estateId));
    setTotalEstatesCount(prev => prev - 1);
  }

  return (
    <div className={`container-fluid bg-beige`}>
      <div className={`container bg-beige my-5`}>
        <h1 className={`text-center my-5 text-blue`}>Omiljene nekretnine</h1>
        <div className={`container`}>
          <div className={`row row-cols-1 row-cols-md-2 row-cols-lg-3 g-4 justify-content-center`}>
            {favoriteEstates.length > 0 ? (
              favoriteEstates.map((estate) => (
                <div key={estate.id} className={`col d-flex justify-content-center`}>
                  <EstateCard estate={estate} type={2} onRemoveFromFavorite={handleRemoveFromFavorite}></EstateCard>
                </div>
              ))
            ) : (
              <p className={`text-center text-muted mx-auto`}>Korisnik trenutno nema omiljenih nekretnina.</p>
            )}

          </div>
          {totalEstatesCount > 0 && (
            <Pagination totalLength={totalEstatesCount} onPaginateChange={handlePaginateChange}/>
          )}
        </div>
      </div>
    </div>
  );
};
