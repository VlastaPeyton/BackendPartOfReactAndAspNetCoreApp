import { useEffect, useState } from 'react'
import StockCommentForm from './StockCommentForm/StockCommentForm';
import { commentPostAPI, commentsGetAPI } from '../../Services/CommentService';
import { toast } from 'react-toastify';
import { CommentGetFromBackend } from '../../Models/CommentGetFromBackend';
import Spinner from '../Spinner/Spinner';
import StockCommentList from '../StockCommentList/StockCommentList';

type Props = {
  stockSymbol: string;
}

type CommentFormInput = {
  title: string;
  content: string;
}

const StockComment = ({stockSymbol}: Props) => {

  const [comments, setComments] = useState<CommentGetFromBackend[]>([]); // Tokom initial render ovo je []
  const [loading, setLoading] = useState<boolean>(); // Nema default value, pa je u initial render loading=undefined

  useEffect(() => {
    getComments();
  },[]) // Only when loading page to load comments, ali ako dodam novi comment aktivirace se handleComment da pozove getComments() da ocita sve comments opet

  const handleComment = async (input: CommentFormInput) => {
    // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u commentPostAPI koji je Frontend.
    await commentPostAPI(input.title, input.content, stockSymbol).then((result) => {
      // result.data je tipa CommentPost ako backend vratio StatusCode=2XX jer onda smo u commentPostAPI u try block
      // result je undefined, pa ne moze result.data, ako backend vratio StatusCode!=2XX (error) jer onda smo u commentPostAPI u catch block otisli
      // Nismo result? jer ne koristimo result.data ovde i onda dovoljno ovako.
      if (result){
        toast.success("Comment created uspesno");
        getComments(); // Da vidimo auziranu listu kad dodamo na licu mesta novi comment
      }
    }).catch((err) => {
      toast.warning(err);  // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska u commentPostAPI (ne u backendu)
    })
  }

  const getComments = async () => {
    setLoading(true);
    // Then-catch je isto kao da sam pisao try-catch. Catch mora ako dodje go greske u commentsGetAPI koji je Frontend.
    await commentsGetAPI(stockSymbol).then((result) => {
      // result.data je tipa CommentGetFromBackend[] ako backend vrati StatusCode=2XX jer onda smo u commentsGetAPi u try block
      // result je undefined, pa ne moze result.data, ako backend posalje StatusCode!=2XX (error) jer onda smo u commentsGetAPI u catch block otisli
      setLoading(false); // React re-renders this component once loading is set
      setComments(result?.data!); // Mora ! jer by default comments=null, pa ako result=undefined, ovo ostaje null. Isto i ovde re-renders once comments is set.
      // Zbog 2 uzastopna set vrv ce React samo 1 re-render da uradi, a ne 2. 
      // Mogo sam if(result) setComments(result.data) ali isto je kao linija iznad i onda u StockCommentList ne bi moralo {comments!} vec {comments}
    }).catch((err) => toast.warn(err));  // Prikaze mali pop-up u gornji desni ugao ekrana ako bude frontend greska u commentsGetAPI (ne u backendu)
  }

  // loading ? jer brze propagira kod iz poziva commentsGetAPI dovde gde renderuje nego sto se izvrsi ta async metoda i onda inicijalno loading=false i render prikaze spiner, ali onda async metoda kad zavrsi setLoading re-renders opet with loading=false i comments!=[] prikaze lepo 
  // During initial render, loading=undefined i comments=[] i renderuje se StockCommentList koja nema vise ternary operator jer comments moze biti prazna lista
  return (
    <div className="flex flex-col">
      {loading ? <Spinner /> : <StockCommentList comments={comments} /> }
      <StockCommentForm symbol={stockSymbol}  handleComment={handleComment}/> 
    </div>
  )
}

export default StockComment;