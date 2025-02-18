import { useEffect, useState } from "react";
import { Estate } from "../../Interfaces/Estate/Estate";
import { Pagination } from "../Pagination/Pagination";
import EstateCard from "../EstateCard/EstateCard";
import { EstateCategory, EstateCategoryTranslations } from "../../Enums/EstateCategory";
import { searchEstatesAPI } from "../../Services/EstateService";
import styles from './SearchEstate.module.css'

export const SearchEstate = () => {
  const [estates, setEstates] = useState<Estate[] | null>(null);
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [totalEstatesCount, setEstatesCount] = useState<number>(0);
  const [searchTitle, setTitleSearch] = useState<string>('');
  const [searchPriceMin, setPriceMinSearch] = useState<number | null>(null);
  const [searchPriceMax, setPriceMaxSearch] = useState<number | null>(null);
  const [searchCategory, setSearchCategory] = useState<EstateCategory[]>([]);

  const handleSearch = () => {
    loadEstates(page, pageSize);
  };

  useEffect(() => {
    loadEstates(page, pageSize);
  }, [page, pageSize]);

  const loadEstates = async (pageNumber: number, pageSizeNumber: number) => {
    setIsLoading(true);
    const result = await searchEstatesAPI(
      searchTitle ?? undefined,
      searchPriceMin ?? undefined,
      searchPriceMax ?? undefined,
      searchCategory ?? undefined,
      pageNumber ?? undefined,
      pageSizeNumber ?? undefined
    );

    if (result && result.totalLength > 0) {
      setEstates(result.data);
      setEstatesCount(result.totalLength);
    } else {
      setEstates(null);
      setEstatesCount(0);
    }
    setIsLoading(false);
  };

  const handlePaginateChange = (newPage: number, newPageSize: number) => {
    setPage(newPage);
    setPageSize(newPageSize);
  };

  const handleDelete = async () => {
    console.log(page * pageSize + 1 + " = " + pageSize);

    if (((page !== 1)) && ((page - 1) * pageSize + 1) === totalEstatesCount) {
      await loadEstates(page - 1, pageSize);
    } else
      await loadEstates(page, pageSize);
  };

  return (
    <>
      <div className={`container-fluid bg-beige p-4`}>
        <div className={`row justify-content-center gap-3`}>
          <div className={`col-xxl-3 col-xl-3 col-lg-4 col-md-5 col-sm-12 mb-4 rounded-3 bg-white shadow p-3`}>
            <h3 className={`text-blue`}>Pretraga Nekretnina</h3>

            <div>
              <label htmlFor="title" className={`text-golden my-2`}>Naziv:</label>
              <input
                id="title"
                type="text"
                value={searchTitle}
                onChange={(e) => setTitleSearch(e.target.value)}
                className={`form-control ${styles.fields}`}
              />
            </div>

            <div>
              <label htmlFor="priceMin" className={`text-golden my-2`}>Minimalna cena:</label>
              <input
                id="priceMin"
                type="number"
                value={searchPriceMin || ""}
                onChange={(e) => setPriceMinSearch(e.target.value ? +e.target.value : null)}
                className={`form-control ${styles.fields}`}
              />
            </div>

            <div>
              <label htmlFor="priceMax" className={`text-golden my-2`}>Maksimalna cena:</label>
              <input
                id="priceMax"
                type="number"
                value={searchPriceMax ?? ""}
                onChange={(e) => setPriceMaxSearch(e.target.value ? +e.target.value : null)}
                className={`form-control ${styles.fields}`}
              />
            </div>

            <div>
              <label className={`text-golden my-2`}>Kategorije:</label>
              {Object.values(EstateCategory).map((category) => (
                <div key={category} className={`text-gray my-2 ms-3`}>
                  <input
                    type="checkbox"
                    className={`form-check-input me-2 cursor-pointer`}
                    id={category}
                    name={category}
                    value={category}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setSearchCategory((prev) => [...(prev || []), category]);
                      } else {
                        setSearchCategory((prev) =>
                          prev ? prev.filter((item) => item !== category) : []
                        );
                      }
                    }}
                    checked={searchCategory?.includes(category)}
                  />
                  <label htmlFor={category}>{EstateCategoryTranslations[category]}</label>
                </div>
              ))
              }
            </div>

            <button
              className={`btn btn-sm my-2 text-white text-center rounded py-2 px-2 ${styles.dugme} ${styles.slova}`}
              onClick={() => handleSearch()}>Pretraži
            </button>
          </div>

          <div className={`col-xxl-8 col-xl-8 col-lg-7 col-md-6 col-sm-12 mb-4 bg-gray rounded-3 shadow`}>
            {isLoading ? (
              <div className={`text-center text-muted mt-3`}>Učitava se...</div>
            ) : (
              <>
                {estates != null && estates.length > 0 ? (
                  <div className={`d-flex flex-wrap justify-content-center gap-2 p-3`}>
                    {estates.map((estate) => (
                      <EstateCard
                        type={1}
                        loadEstates={handleDelete}
                        key={estate.id}
                        estate={estate}
                        refreshOnDeleteEstate={null}
                      />
                    ))}
                  </div>
                ) : (
                  <div className={`text-center text-muted mt-3 fs-4`}>Nema rezultata pretrage</div>
                )}

                {totalEstatesCount > 0 && (
                  <div className={`my-4`}>
                    <Pagination totalLength={totalEstatesCount} onPaginateChange={handlePaginateChange}
                      currentPage={page} perPage={pageSize} />
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      </div>
    </>
  );
};

export default SearchEstate;