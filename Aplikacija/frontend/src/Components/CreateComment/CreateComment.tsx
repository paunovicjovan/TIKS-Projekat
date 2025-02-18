import styles from "./CreateComment.module.css";

type Props = {
  content: string;
  setContent: (content: string) => void;
  onCommentCreated: () => void;
}

export const CreateComment = ({onCommentCreated, setContent, content}: Props) => {
  return (
    <>
      <div>
        <div className={`mb-3`}>
          <textarea
            className={`form-control`}
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Unesite komentar"
            rows={3}
          />
        </div>
        <button
          className={`btn-lg text-white text-center rounded-3 border-0 py-2 px-2 ${styles.slova} ${styles.dugme1} ${styles.linija_ispod_dugmeta}`}
          onClick={onCommentCreated}
          disabled={!content.trim()}
        >
          Kreiraj komentar
        </button>
      </div>
    </>
  );
};