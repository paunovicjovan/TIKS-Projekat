import {ChangeEvent, useEffect, useState} from "react";
import {Link, useLocation, useNavigate, useParams} from "react-router-dom";
import {getEstateAPI, updateEstateAPI} from "../../Services/EstateService";
import {Estate} from "../../Interfaces/Estate/Estate";
import {toast} from "react-hot-toast";
import styles from "./EstatePage.module.css";
import {CreatePost} from "../CreatePost/CreatePost.tsx";
import {PostCard} from "../PostCard/PostCard.tsx";
import {Pagination} from "../Pagination/Pagination.tsx";
import {Post} from "../../Interfaces/Post/Post.ts";
import {createPostAPI, getAllPostsForEstateAPI} from "../../Services/PostService.tsx";
import {CreatePostDTO} from "../../Interfaces/Post/CreatePostDTO.ts";
import {FontAwesomeIcon} from '@fortawesome/react-fontawesome';
import {faContactCard, faHeart, faPhone} from '@fortawesome/free-solid-svg-icons';
import {EstateCategory, getEstateCategoryTranslation} from "../../Enums/EstateCategory.ts";
import noposts from "../../Assets/noposts.png";
import MapWithMarker from "../Map/MapWithMarker.tsx";
import {useAuth} from "../../Context/useAuth.tsx";
import {deleteEstateAPI} from "../../Services/EstateService.tsx";
import Swal from "sweetalert2";
import {addToFavoritesAPI, canAddEstateToFavoriteAPI, removeFromFavoritesAPI} from "../../Services/UserService.tsx";

export const EstatePage = () => {
  const {id} = useParams();
  const user = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const setEdit = location.state?.setEdit || false;

  const [estate, setEstate] = useState<Estate | null>(null);
  const [isEstateLoading, setIsEstateLoading] = useState<boolean>(true);
  const [isPostsLoading, setIsPostsLoading] = useState<boolean>(true);
  const [canAddToFavorite, setCanAddToFavorite] = useState<boolean>(true);

  const [posts, setPosts] = useState<Post[]>([]);
  const [totalPostsCount, setTotalPostsCount] = useState<number>(0);
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);

  const [editMode, setEditMode] = useState(setEdit);
  const [updatedTitle, setUpdatedTitle] = useState(estate?.title || "");
  const [updatedDescription, setUpdatedDescription] = useState(estate?.description || "");
  const [updatedCategory, setUpdatedCategory] = useState(estate?.category || "");
  const [updatedPictures, setUpdatedPictures] = useState<FileList | null>(null);
  const [updatedPrice, setUpdatedPrice] = useState(estate?.price || '');
  const [updatedTotalRooms, setUpdatedTotalRooms] = useState(estate?.totalRooms || '');
  const [updatedFloorNumber, setUpdatedFloorNumber] = useState(estate?.floorNumber || '');
  const [updatedSquareMeters, setUpdatedSquareMeters] = useState(estate?.squareMeters || '');
  const [updatedLongitude, setUpdatedLongitude] = useState<number | null>(estate?.longitude ?? null);
  const [updatedLatitude, setUpdatedLatitude] = useState<number | null>(estate?.latitude ?? null);

  useEffect(() => {
    fetchEstate();
    checkIfCanAddToFavorite();
    loadPosts(1, 10);
  }, [id]);

  useEffect(() => {
    resetUpdatedFields();
  }, [estate]);

  const fetchEstate = async () => {
    try {
      if (!id) {
        toast.error("Nekretnina nije pronađena");
        return;
      }
      const estateResponse = await getEstateAPI(id);
      if (estateResponse) {
        setEstate(estateResponse);
      }
    } catch {
      toast.error("Greška pri učitavanju nekretnine");
    } finally {
      setIsEstateLoading(false);
    }
  };

  const checkIfCanAddToFavorite = async () => {
    try {
      const response = await canAddEstateToFavoriteAPI(id!);
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
    const response = await deleteEstateAPI(estate!.id);
    if (response) {
      toast.success("Nekretnina uspešno obrisana.");
    }
    navigate(`..`);
  };

  const handleCreatePost = async (title: string, content: string) => {
    try {
      const postDto: CreatePostDTO = {
        title,
        content,
        estateId: id ?? null
      }
      const response = await createPostAPI(postDto);

      if (response?.status == 200) {
        toast.success("Uspešno kreirana objava.");
        setPage(1);
        await loadPosts(1, pageSize);
      }
    } catch {
      toast.error("Došlo je do greške prilikom kreiranja objave.");
    }
  }

  const handlePaginateChange = async (page: number, pageSize: number) => {
    setPage(page);
    setPageSize(pageSize);
    await loadPosts(page, pageSize);
  }

  const loadPosts = async (page: number, pageSize: number) => {
    try {
      setIsPostsLoading(true);
      const response = await getAllPostsForEstateAPI(id!, page, pageSize);

      if (response?.status == 200) {
        setPosts(response.data.data);
        setTotalPostsCount(response.data.totalLength);
      }
    } catch {
      toast.error("Došlo je do greške prilikom učitavanja objava.");
    } finally {
      setIsPostsLoading(false);
    }
  }

  const handleCategoryChange = (e: ChangeEvent<HTMLSelectElement>) => {
    setUpdatedCategory(e.target.value);
  };

  const handlePicturesChange = (e: ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      setUpdatedPictures(e.target.files);
    }
  };

  const handleAddToFavorite = async () => {
    try {
      const response = await addToFavoritesAPI(estate!.id);
      if (response?.status === 200) {
        setCanAddToFavorite(false);
        toast.success("Nekretnina je dodata u omiljene!");
      }
    } catch {
      toast.error("Došlo je do greške prilikom dodavanja u omiljene.");
    }
  }

  const handleRemoveFromFavorite = async () => {
    try {
      const response = await removeFromFavoritesAPI(estate!.id);
      if (response?.status === 200 && response.data) {
        setCanAddToFavorite(true);
        toast.success("Nekretnina uklonjena iz omiljenih!");
      }
    } catch {
      toast.error("Došlo je do greške prilikom uklanjanja nekretnine iz omiljenih.");
    }
  }

  const handleUpdate = async () => {
    try {
      if (!estate) return;

      const formData = new FormData();
      formData.append('title', updatedTitle);
      formData.append('description', updatedDescription);
      formData.append('price', updatedPrice.toString());
      formData.append('squareMeters', updatedSquareMeters.toString());
      formData.append('totalRooms', updatedTotalRooms.toString());
      formData.append('category', updatedCategory);

      if (updatedCategory != EstateCategory.House)
        formData.append('floorNumber', updatedFloorNumber.toString());

      if (updatedPictures) {
        for (let i = 0; i < updatedPictures.length; i++) {
          formData.append('images', updatedPictures[i]);
        }
      }

      formData.append('longitude', updatedLongitude!.toString());
      formData.append('latitude', updatedLatitude!.toString());

      const response = await updateEstateAPI(estate.id, formData);

      if (response?.status === 200) {
        toast.success("Nekretnina je uspešno ažurirana.");
        setEstate(prevEstate => {
          return prevEstate ? {
            ...estate,
            ...response.data
          } : null;
        });
        setEditMode(false);
      }
    } catch {
      toast.error("Došlo je do greške prilikom ažuriranja nekretnine.");
    }
  };

  const handleCancelUpdate = () => {
    setEditMode(false);
    resetUpdatedFields();
  }

  const resetUpdatedFields = () => {
    if (estate) {
      setUpdatedTitle(estate.title);
      setUpdatedDescription(estate.description);
      setUpdatedPrice(estate.price);
      setUpdatedTotalRooms(estate.totalRooms);
      setUpdatedSquareMeters(estate.squareMeters);
      setUpdatedFloorNumber(estate.floorNumber ?? '');
      setUpdatedCategory(estate.category);
      setUpdatedLongitude(estate.longitude);
      setUpdatedLatitude(estate.latitude);
    }
  }

  return (
    <div className={`container-fluid bg-beige d-flex justify-content-center`}>
      <div className={`container mt-5`}>
        {/*Nekretnina*/}
        {isEstateLoading ? (
          <p className={`text-center text-muted mt-3`}>Učitavanje nekretnine...</p>
        ) : (
          estate ? (
            <>
              <div className={`card shadow`}>
                <div className={`row g-0`}>
                  <div className={`col-md-6`}>
                    <div className={`${styles.imageGallery} p-3`}>
                      <div id="carouselExampleIndicators" className={`carousel slide shadow rounded overflow-hidden`}>
                        <div className={`carousel-indicators`}>
                          {estate.images.map((_, i) =>
                            <button type="button" key={i} data-bs-target="#carouselExampleIndicators"
                                    data-bs-slide-to={`${i}`} className={`${i == 0 ? 'active' : ''}`}></button>
                          )}
                        </div>
                        <div className={`carousel-inner`}>
                          {estate.images.map((pictureName, i) => (
                              <div className={`carousel-item ${i === 0 ? "active" : ""}`} key={i}>
                                <img src={`${import.meta.env.VITE_SERVER_URL}${pictureName}`} className={`d-block w-100`}
                                     alt="..."/>
                              </div>
                            )
                          )}
                        </div>
                        <button className={`carousel-control-prev`} type="button"
                                data-bs-target="#carouselExampleIndicators" data-bs-slide="prev">
                          <span className={`carousel-control-prev-icon`} aria-hidden="true"></span>
                          <span className={`visually-hidden`}>Previous</span>
                        </button>
                        <button className={`carousel-control-next`} type="button"
                                data-bs-target="#carouselExampleIndicators" data-bs-slide="next">
                          <span className={`carousel-control-next-icon`} aria-hidden="true"></span>
                          <span className={`visually-hidden`}>Next</span>
                        </button>
                      </div>
                    </div>
                  </div>
                  <div className={`col-md-6`}>
                    {!editMode ? (
                      <div className={`card-body`}>
                        <div className={`d-flex justify-content-between align-items-start mb-4`}>
                          <div>
                            <h1 className={`mb-4 text-blue`}>{estate?.title}</h1>
                            <p className={`lead mb-4 text-gray`}>{estate?.description}</p>
                          </div>
                          <div className={`mt-1`}>
                            {user?.user?.id === estate?.user?.id ? (
                                <div>
                                  <button
                                    className={`btn btn-sm my-2 text-white text-center rounded py-2 px-2 ${styles.dugme1} ${styles.linija_ispod_dugmeta} ${styles.slova}`}
                                    onClick={() => setEditMode(true)}
                                  >
                                    Ažuriraj
                                  </button>
                                  <button
                                    className={`btn btn-sm ms-2 my-2 text-white text-center rounded py-2 px-2 ${styles.dugme2} ${styles.linija_ispod_dugmeta} ${styles.slova}`}
                                    onClick={confirmEstateDeletion}
                                  >
                                    Obriši
                                  </button>
                                </div>)
                              : (
                                <>
                                  {
                                    canAddToFavorite ?
                                      (<>
                                        <button
                                          className={`btn btn-outline-danger me-2`}
                                          onClick={handleAddToFavorite}>
                                          <FontAwesomeIcon icon={faHeart}/>
                                        </button>
                                      </>) :
                                      (<>
                                        <button
                                          className={`btn btn-danger me-2`}
                                          onClick={handleRemoveFromFavorite}>
                                          <FontAwesomeIcon icon={faHeart}/>
                                        </button>
                                      </>)
                                  }
                                </>
                              )}
                          </div>
                        </div>
                        <div className={`row mb-4`}>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Cena</h5>
                            <p className={`text-blue fs-5`}>{estate?.price} €</p>
                          </div>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Veličina</h5>
                            <p className={`text-blue fs-5`}>{estate?.squareMeters} m²</p>
                          </div>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Broj soba</h5>
                            <p className={`text-blue fs-5`}>{estate?.totalRooms}</p>
                          </div>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Kategorija</h5>
                            <p className={`text-blue fs-5`}>{getEstateCategoryTranslation(estate?.category)}</p>
                          </div>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Sprat</h5>
                            <p className={`text-blue fs-5`}>{estate?.floorNumber ?? "N/A"}</p>
                          </div>
                          <div className={`col-md-6 mb-3`}>
                            <h5 className={`text-golden`}>Kontakt</h5>
                            <Link className={`text-blue text-decoration-none fs-5 me-2 d-block`}
                                  to={`/user-profile/${estate?.user?.id}`}>
                              <FontAwesomeIcon icon={faContactCard} className={`me-2`}/>
                              {estate?.user?.username}
                            </Link>
                            <a href={`tel:${estate?.user?.phoneNumber}`}
                               className={`text-blue text-decoration-none fs-5 d-block`}>
                              <FontAwesomeIcon icon={faPhone} className={`me-2`}/>
                              {estate?.user?.phoneNumber}</a>
                          </div>
                        </div>
                      </div>
                    ) : (
                      <div className={`p-3`}>
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Naziv:</label>
                          <input
                            type="text"
                            className={`form-control ${styles.fields}`}
                            value={updatedTitle}
                            onChange={(e) => setUpdatedTitle(e.target.value)}
                          />
                        </div>
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Opis:</label>
                          <textarea
                            className={`form-control ${styles.fields}`}
                            value={updatedDescription}
                            onChange={(e) => setUpdatedDescription(e.target.value)}
                          ></textarea>
                        </div>
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Cena:</label>
                          <input
                            type="number"
                            className={`form-control ${styles.fields}`}
                            value={updatedPrice}
                            onChange={(e) => setUpdatedPrice(e.target.value)}
                          />
                        </div>
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Broj soba:</label>
                          <input
                            type="number"
                            className={`form-control ${styles.fields}`}
                            value={updatedTotalRooms}
                            onChange={(e) => setUpdatedTotalRooms(e.target.value)}
                          />
                        </div>
                        {updatedCategory != EstateCategory.House &&
                          <div className={`mb-3`}>
                            <label className={`form-label text-blue`}>Sprat:</label>
                            <input
                              type="number"
                              className={`form-control ${styles.fields}`}
                              value={updatedFloorNumber}
                              onChange={(e) => setUpdatedFloorNumber(e.target.value)}
                            />
                          </div>}
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Površina:</label>
                          <input
                            type="number"
                            className={`form-control ${styles.fields}`}
                            value={updatedSquareMeters}
                            onChange={(e) => setUpdatedSquareMeters(e.target.value)}
                          />
                        </div>
                        <div>
                          <label className={`form-label text-blue`}>Kategorija:</label>
                          <select
                            className={`form-select ${styles.fields} mb-3`}
                            value={updatedCategory}
                            onChange={handleCategoryChange}
                            required
                          >
                            {Object.values(EstateCategory).map((category) => (
                              <option key={category} value={category}>
                                {getEstateCategoryTranslation(category)}
                              </option>
                            ))}
                          </select>
                        </div>
                        <div className={`mb-3`}>
                          <label className={`form-label text-blue`}>Slike:</label>
                          <input
                            type="file"
                            className={`form-control ${styles.fields}`}
                            onChange={handlePicturesChange}
                            multiple
                          />
                        </div>
                      </div>
                    )}
                  </div>
                  <h3 className={`text-center text-golden mb-3`}>Lokacija</h3>
                  {!editMode ? (
                    <div className={`container-fluid p-0`}>
                      <MapWithMarker
                        lat={estate.latitude}
                        long={estate.longitude}
                        editMode={false}
                      />
                    </div>) : user?.user?.id === estate?.user?.id ? (
                    <>
                      <div style={{width: '100%', height: '500px', overflow: 'hidden'}}>
                        <MapWithMarker
                          lat={updatedLatitude}
                          long={updatedLongitude}
                          setLat={setUpdatedLatitude}
                          setLong={setUpdatedLongitude}
                          editMode={true}
                        />
                      </div>
                      <div className={`d-flex justify-content-end me-auto pe-3 my-1`}>
                        <button
                          className={`btn btn-sm my-2 text-white text-center rounded py-2 px-2 ${styles.dugme1} ${styles.linija_ispod_dugmeta} ${styles.slova}`}
                          onClick={handleUpdate}
                        >
                          Sačuvaj
                        </button>
                        <button
                          className={`btn btn-sm ms-2 my-2 text-white text-center rounded py-2 px-2 ${styles.dugme2} ${styles.linija_ispod_dugmeta} ${styles.slova}`}
                          onClick={() => handleCancelUpdate()}
                        >
                          Otkaži
                        </button>
                      </div>
                    </>
                  ) : (
                    <div className={`container-fluid p-0`}>
                      <MapWithMarker
                        lat={updatedLatitude}
                        long={updatedLongitude}
                        setLat={setUpdatedLatitude}
                        setLong={setUpdatedLongitude}
                        editMode={false}
                      />
                    </div>)}
                </div>

              </div>
            </>
          ) : (
            <p className={`text-center text-muted`}>Nema podataka o nekretnini.</p>
          )
        )}

        {/*Objave*/}
        <div className={`row`}>
          <div className={`col-md-4`}>
            <CreatePost onCreatePost={handleCreatePost}/>
          </div>

          <div className={`col-md-8 my-5`}>
            <h2 className={`text-blue`}>Objave</h2>
            {isPostsLoading ? (<>
              <p className={`text-center text-muted`}>Učitavanje objava...</p>
            </>) : (
              <>
                {posts.length > 0 ? posts.map(post => (
                  <PostCard key={post.id} post={post}/>
                )) : <div className={`d-flex justify-content-center`}>
                  <img src={noposts} alt="noposts" className={`img-fluid ${styles.slika}`}/>
                </div>
                }
              </>
            )}
            {totalPostsCount > 0 &&
              <Pagination totalLength={totalPostsCount} onPaginateChange={handlePaginateChange} currentPage={page}
                          perPage={pageSize}/>}
          </div>

          <div className={`my-4`}></div>
        </div>
      </div>
    </div>
  );
};