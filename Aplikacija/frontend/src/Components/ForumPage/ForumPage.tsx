import {CreatePost} from "../CreatePost/CreatePost.tsx";
import toast from "react-hot-toast";
import {createPostAPI, getAllPostsAPI} from "../../Services/PostService.tsx";
import {CreatePostDTO} from "../../Interfaces/Post/CreatePostDTO.ts";
import {ChangeEvent, useEffect, useState} from "react";
import {Post} from "../../Interfaces/Post/Post.ts";
import {PostCard} from "../PostCard/PostCard.tsx";
import {Pagination} from "../Pagination/Pagination.tsx";
import styles from './ForumPage.module.css'


export const ForumPage = () => {
  const [posts, setPosts] = useState<Post[]>([]);
  const [totalPostsCount, setTotalPostsCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [page, setPage] = useState<number>(1);
  const [pageSize, setPageSize] = useState<number>(10);
  const [searchTitle, setSearchTitle] = useState<string>("");

  useEffect(() => {
    loadPosts(searchTitle, page, pageSize);
  }, [searchTitle]);

  const handleCreatePost = async (title: string, content: string) => {
    try {
      const postDto: CreatePostDTO = {
        title,
        content,
        estateId: null
      }
      const response = await createPostAPI(postDto);

      if (response?.status == 200) {
        toast.success("Uspešno kreirana objava.");
        setPage(1);
        setSearchTitle("");
        await loadPosts("", 1, pageSize);
      }
    } catch {
      toast.error("Došlo je do greške prilikom kreiranja objave.");
    }
  }

  const handlePaginateChange = async (page: number, pageSize: number) => {
    setPage(page);
    setPageSize(pageSize);
    await loadPosts(searchTitle, page, pageSize);
  }

  const loadPosts = async (title: string, page: number, pageSize: number) => {
    try {
      setIsLoading(true);
      const response = await getAllPostsAPI(title, page, pageSize);

      if (response?.status == 200) {
        setPosts(response.data.data);
        setTotalPostsCount(response.data.totalLength);
      }
    } catch {
      toast.error("Došlo je do greške prilikom učitavanja objava.");
    } finally {
      setIsLoading(false);
    }
  }

  const handleSearchTitleChange = (event: ChangeEvent<HTMLInputElement>) => {
    setSearchTitle(event.target.value);
  };

  return (
    <div className={`container-fluid bg-beige`}>
      <div className="container">

        <div className={`row`}>
          <div className={`col-md-4`}>
            <CreatePost onCreatePost={handleCreatePost}/>

          </div>

          <div className={`col-md-8 my-5`}>
            <h2 className={`text-blue text-center`}>Objave</h2>
            <div className={`form-floating mb-3`}>
              <input
                type="text"
                className={`form-control ${styles.fields} ${styles.input_placeholder}`}
                id="searchTitle"
                placeholder="Pretražite objave po naslovu"
                onChange={handleSearchTitleChange}
                name="searchTitle"
                value={searchTitle}
              />
              <label htmlFor="searchTitle" className={`text-gray`}>
                Pretražite objave po naslovu
              </label>
            </div>
            {isLoading ? (<>
              <p className={`text-center text-muted`}>Učitavanje objava...</p>
            </>) : (
              <>
                {posts.length > 0 ?
                  posts.map(post => (
                  <PostCard key={post.id} post={post}/>
                ))
                :
                <p className={`text-center text-blue fs-5 mt-4`}>Trenutno ne postoji nijedna objava.</p>}
              </>
            )}
            {totalPostsCount > 0 &&
              <Pagination totalLength={totalPostsCount} onPaginateChange={handlePaginateChange} currentPage={page}
                          perPage={pageSize}/>}
          </div>
        </div>
      </div>

    </div>
  );
};