import {Post} from "../../Interfaces/Post/Post.ts";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";
import {faCalendarAlt, faUser} from "@fortawesome/free-solid-svg-icons";
import {useNavigate} from "react-router";
import styles from "./PostCard.module.css";
import EstateCard from "../EstateCard/EstateCard.tsx";

type Props = {
  post: Post;
}

export const PostCard = ({post}:Props) => {
  const navigate = useNavigate();

  return (
    <div className={`card mb-4 shadow`}>
      <div className={`card-body d-flex justify-content-between`}>
        <div className={`left-content me-3`}>
          <h3 className={`text-gray`}>{post.title}</h3>
          <p className={`text-golden`} style={{ cursor: "pointer" }} onClick={() => navigate(`/user-profile/${post.author.id}`)}>
            <FontAwesomeIcon icon={faUser} className={`me-1`}/>
            {post.author.username}</p>
          <FontAwesomeIcon icon={faCalendarAlt} className={`text-blue me-2`}/>
          <span className={`text-blue text-small`}>
            {new Date(post.createdAt).toLocaleDateString("sr")}
          </span>
          <p className={`card-text content-text`} style={{whiteSpace:"pre-line"}}>{post.content}</p>

          <button className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`} onClick={() => navigate(`/forum/${post.id}`, {state: {post}})}>
            Pogledaj detalje
          </button>
        </div>

        {post.estate && <EstateCard estate={post.estate} type={1} canDelete={false}/>}

      </div>
    </div>
  );
};