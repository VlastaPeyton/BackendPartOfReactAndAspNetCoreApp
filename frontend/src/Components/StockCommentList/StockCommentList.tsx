
import { CommentGetFromBackend } from '../../Models/CommentGetFromBackend';
import StockCommentListItem from './StockCommentListItem';

type Props = {
    comments: CommentGetFromBackend[];
}

const StockCommentList = ({comments}: Props) => {
  return (
    // .map pravi listu i zahteva unique key (iako StockCommentListItem nema taj prop), da bi HTML pratio elemente liste, kao i uvek i prosledicu index jer znam da je unique i najlakse mi je njega
    // Obzirom da i tokom initial redner of StockComment, kada loading=undefined i comments=[] i tada renderujemo StockCommentList with comments=[] ne treba mi ternary ovde vise 
    <>
    {comments.map((comment : CommentGetFromBackend, index: number) => {return <StockCommentListItem key={index} comment={comment} />})}
    </>
  )
}

export default StockCommentList;