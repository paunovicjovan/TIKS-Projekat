import {useLocation, useNavigate, useParams} from "react-router";
import {useEffect, useState} from "react";
import {Post} from "../../Interfaces/Post/Post.ts";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faUser} from "@fortawesome/free-solid-svg-icons";
import {deletePostAPI, getPostById, updatePostAPI} from "../../Services/PostService.tsx";
import toast from "react-hot-toast";
import {CreateComment} from "../CreateComment/CreateComment.tsx";
import {createCommentAPI, deleteCommentAPI, getCommentsForPostAPI} from "../../Services/CommentService.tsx";
import {Comment} from "../../Interfaces/Comment/Comment.ts";
import {CommentCard} from "../CommentCard/CommentCard.tsx";
import {useAuth} from "../../Context/useAuth.tsx";
import Swal from 'sweetalert2';
import styles from "./PostPage.module.css";
import EstateCard from "../EstateCard/EstateCard.tsx";

export const PostPage = () => {

  const {postId} = useParams();
  const {user} = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [post, setPost] = useState<Post | null>(null);
  const [isPostLoading, setIsPostLoading] = useState<boolean>(false);
  const [isCommentsLoading, setIsCommentsLoading] = useState<boolean>(false);
  const [comments, setComments] = useState<Comment[]>([]);
  const [totalCommentsCount, setTotalCommentsCount] = useState<number>(0);
  const [newCommentContent, setNewCommentContent] = useState<string>("");
  const [isEditing, setIsEditing] = useState(false);
  const [editedTitle, setEditedTitle] = useState(post?.title || "");
  const [editedContent, setEditedContent] = useState(post?.content || "");
  const [scrollPosition, setScrollPosition] = useState(0);

  useEffect(() => {
    loadPost();

    setComments([]);
    loadMoreComments();

  }, [postId]);

  useEffect(() => {
    window.scrollTo({
      top: scrollPosition,
      behavior: "instant",
    })
  }, [scrollPosition]);

  const loadPost = async () => {
    try {
      setIsPostLoading(true);
      const response = await getPostById(postId!);
      if (response?.status == 200) {
        setPost(response.data);
        setEditedTitle(response.data.title);
        setEditedContent(response.data.content);
        setIsPostLoading(false);
      }
    } catch {
      toast.error("Došlo je do greške prilikom učitavanja objave.");
    } finally {
      setIsPostLoading(false);
    }
  }

  const handleAddComment = async () => {
    try {
      if (!newCommentContent) {
        toast.error("Unesite sadržaj komentara.");
        return;
      }
      const response = await createCommentAPI(newCommentContent, postId!);
      if (response?.status == 200) {
        toast.success("Uspešno dodat komentar.");
        setNewCommentContent('');
        setTotalCommentsCount(total => total + 1);
        setComments(comments => [response.data, ...comments])
      }
    } catch {
      toast.error("Došlo je do greške prilikom kreiranja komentara.");
    }
  }

  const loadMoreComments = async () => {
    try {
      setIsCommentsLoading(true);
      const scrollY = window.scrollY;
      const response = await getCommentsForPostAPI(postId!, comments.length, 5);
      if (response?.status == 200) {
        const newComments = response.data.data;
        setComments(prevComments => [
          ...prevComments,
          ...newComments.filter(newComment =>
            !prevComments.some(existingComment => existingComment.id === newComment.id)
          ),
        ]);
        setTotalCommentsCount(response.data.totalLength);
        setIsCommentsLoading(false);
      }
      setScrollPosition(scrollY);
    } catch {
      toast.error("Došlo je do greške prilikom učitavanja komentara.");
    } finally {
      setIsCommentsLoading(false);
    }
  }

  const confirmPostDeletion = async () => {
    Swal.fire({
      title: "Da li sigurno želite da obrišete objavu?",
      text: "Uz objavu će biti obrisani i svi njeni komentari!",
      icon: "warning",
      position: "top",
      showCancelButton: true,
      confirmButtonColor: "#8cc4da",
      cancelButtonColor: "#d33",
      cancelButtonText: "Otkaži",
      confirmButtonText: "Obriši"
    }).then(async (result) => {
      if (result.isConfirmed) {
        await handleDeletePost();
      }
    });
  }

  const handleDeletePost = async () => {
    try {
      const response = await deletePostAPI(postId!);
      if (response?.status == 204) {
        toast.success("Uspešno brisanje objave.");
        navigate('..');
      }
    } catch {
      toast.error("Došlo je do greške prilikom brisanja objave.");
    }
  }

  const handleCancelEdit = () => {
    setIsEditing(false);
    setEditedTitle(post?.title || "");
    setEditedContent(post?.content || "");
  };

  const handleSaveEdit = async () => {
    try {
      const response = await updatePostAPI(postId!, editedTitle, editedContent);
      if (response?.status === 200) {
        toast.success("Uspešno izmenjena objava.");
        if (location.state?.post)
          location.state.post = null;
        setPost(prev => {
          return prev ?
            {
              ...prev,
              title: editedTitle,
              content: editedContent,
            } :
            null
        });
        setIsEditing(false);
      }
    } catch {
      toast.error("Došlo je do greške prilikom izmene objave.");
    }
  };

  const confirmCommentDeletion = async (commentId: string) => {
    Swal.fire({
      title: "Da li sigurno želite da obrišete komentar?",
      icon: "warning",
      position: "top",
      showCancelButton: true,
      confirmButtonColor: "#8cc4da",
      cancelButtonColor: "#d33",
      cancelButtonText: "Otkaži",
      confirmButtonText: "Obriši"
    }).then(async (result) => {
      if (result.isConfirmed) {
        await handleDeleteComment(commentId);
      }
    });
  }

  const handleDeleteComment = async (id: string) => {
    try {
      const response = await deleteCommentAPI(id);
      if (response?.status == 204) {
        toast.success("Uspešno brisanje komentara.");
        setComments(comments => comments.filter(c => c.id !== id));
        setTotalCommentsCount(total => total - 1);
      }
    } catch {
      toast.error("Greška pri brisanju komentara.");
    }
  }

  return (
    <>
      <div className={`container-fluid bg-beige`}>
        <div className={`container mt-4`}>
          {isPostLoading ?
            (<><p className={`text-center text-muted`}>Učitavanje objave...</p></>) :
            (post && <>
              <div>

                {isEditing ? (
                  <div className={`d-flex flex-column gap-2 card rounded-3 p-4 shadow my-4`}>
                    <label className={`text-blue`} htmlFor="postTitle">Naslov:</label>
                    <input
                      type="text"
                      className={`form-control`}
                      value={editedTitle}
                      id="postTitle"
                      onChange={(e) => setEditedTitle(e.target.value)}
                      required
                    />
                    <label className={`text-blue`} htmlFor="postContent">Sadržaj:</label>
                    <textarea
                      className={`form-control`}
                      value={editedContent}
                      id={`postContent`}
                      onChange={(e) => setEditedContent(e.target.value)}
                      required
                    />
                    <div className={`mt-2 flex space-x-2`}>
                      <button onClick={handleSaveEdit}
                              className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 me-1 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}
                              disabled={!editedTitle || !editedContent}>
                        Sačuvaj
                      </button>
                      <button onClick={handleCancelEdit}
                              className={`btn-lg text-gray text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme2} ${styles.linija_ispod_dugmeta}`}>
                        Otkaži
                      </button>
                    </div>
                  </div>
                ) : (
                  <>
                    <div className={`card rounded-3 p-4 shadow my-4`}>
                      <div className={`card-body d-flex justify-content-between`}>
                        <div className={`left-content me-3`}>
                          <h2 className={`text-blue`}>{post.title}</h2>
                          <p className={`text-golden`}>
                            <FontAwesomeIcon icon={faUser} className={`me-1`}/>
                            {post?.author.username}
                          </p>
                          <p className={`content-text`}>{post.content}</p>

                          {post?.author.id == user?.id &&
                            <div className={`d-flex`}>
                              <button onClick={() => setIsEditing(true)}
                                      className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 me-1 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}>
                                Izmeni
                              </button>
                              <button onClick={confirmPostDeletion}
                                      className={`btn-lg text-gray text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme2} ${styles.linija_ispod_dugmeta}`}>
                                Obriši
                              </button>
                            </div>
                          }
                        </div>

                          {post.estate && <>
                            <EstateCard estate={post.estate} type={1} canDelete={false}/>
                          </>}
                      </div>
                    </div>
                  </>
                )}

              </div>
            </>)}

          <CreateComment content={newCommentContent} setContent={setNewCommentContent}
                         onCommentCreated={handleAddComment}/>

          <h2 className={`my-3 text-gray`}>Komentari</h2>

          <div className={`col-md-8 mb-5`}>
            {isCommentsLoading ? (<>
              <p className={`text-center text-muted`}>Učitavanje komentara...</p>
            </>) : (
              <>
                {comments.length > 0 && comments.map(comment => (
                  <CommentCard key={comment.id} comment={comment} onDelete={confirmCommentDeletion}/>
                ))}
              </>
            )}
            {comments.length < totalCommentsCount &&
              <button onClick={loadMoreComments}
                      className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}>Prikaži
                još</button>}
          </div>
        </div>
      </div>
    </>
  );
};