import {useState} from "react";
import toast from "react-hot-toast";
import styles from "./CreatePost.module.css";

type Props = {
  onCreatePost: (title: string, content: string) => Promise<void>;
}

export const CreatePost = ({onCreatePost}:Props) => {
  const [title, setTitle] = useState<string>('');
  const [content, setContent] = useState<string>('');

  const handleSubmit = async () => {
    if (!title || !content) {
      toast.error("Molimo vas da popunite sve obavezne podatke.");
      return;
    }

    onCreatePost(title, content).then(_ => {
      setTitle("");
      setContent("");
    });
  };

  return (
    <div className={`container mt-5`}>
      <h2 className={`mb-2 text-blue`}>Kreiraj Objavu</h2>
      <div className={`card p-4 shadow`}>
        <div className={`form-group mb-3`}>
          <label htmlFor="title" className={`form-label text-golden`}>
            Naslov:
          </label>
          <input
            type="text"
            id="title"
            className={`form-control ${styles.fields}`}
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Unesite naslov..."
          />
        </div>

        <div className={`form-group mb-3`}>
          <label htmlFor="content" className={`form-label text-golden`}>
            Sadržaj:
          </label>
          <textarea
            id="content"
            className={`form-control ${styles.fields}`}
            value={content}
            onChange={(e) => setContent(e.target.value)}
            rows={5}
            placeholder="Unesite sadržaj objave..."
          />
        </div>

        <button
          className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}
          onClick={handleSubmit}
        >
          Objavi
        </button>
      </div>
    </div>
  );
};