import * as Yup from "yup";
import { yupResolver } from '@hookform/resolvers/yup';
import {useForm} from "react-hook-form";

type Props = {
  symbol: string;
  handleComment: (e: CommentFormInput) => void;
}

type CommentFormInput = {
  title: string;
  content: string;
}

// Yup je za validaciju. Za ovo nema validacija u BE, vec samo ovde.
const validation = Yup.object().shape({
  title: Yup.string().required("Title is requeired"), // Yup.string() - jer title je string in CommentFormInput
  content: Yup.string().required("Content is required")
  // Mora ista imena i redosled polja kao u CommentInputForm 
})

const StockCommentForm = ({symbol, handleComment}: Props) => {

  const {
    // Ova 4 useForm vraca i mora ovako da se zovu jer su im to built-in imena.
    register,     // Zbog register("title/content") u HTML mi je potrebno ovo. Objasnjeno u LoginPage.
    handleSubmit, /* Zbog onSubmit={handleSubmit(onSubmit)} mi je potrebno ovo. Prevents default automatski. Uzme sve iz forme u obliku CommentFormInput i prosledi u onSubmit.
                  Takodje, validira formu using yurResolver. Ako invalid forma, napravi formState.errors. Ako valid, napravi CommentInputForm objekat sa poljima iz forme i pozove onSubmit.*/
    reset,        // Da se isprazne Title i Content polja after comment has been submitted
    formState: {errors} // Destruktuira formState koji sadrzi vise polja, pa mi samo treba errors objekat koji sadrzi title i content polje. Ovo je isto kao formState.errors. Moze errors.title/content.message
  } = useForm<CommentFormInput>({resolver: yupResolver(validation)}) // Koristim yupResolver za validaciju of React Hook Form 

  const onSubmit = (data: CommentFormInput) => {
    handleComment(data); 
    reset();
  }

  // <label htmlFor="comment"..> povezano sa <textarea id="comment"...>
  return (
    <form className="mt-4 ml-4" onSubmit={handleSubmit(onSubmit)}> {/* Ne treba (e) => handleSubmit jer on sam ima prevent default u sebi + sam izdvoji sve iz forme i prosledi u onSubmit */}
      <input
        type="text"
        id="title"
        className="mb-3 bg-white border border-gray-300 text-gray-900 sm:text-sm rounded-lg focus:ring-primary-600 focus:border-primary-600 block w-full p-2.5 dark:bg-gray-700 dark:border-gray-600 dark:placeholder-gray-400 dark:text-white dark:focus:ring-blue-500 dark:focus:border-blue-500"
        placeholder="Title"
        {...register("title")}
      />
      {errors.title ? <p>{errors.title.message}</p> : ""}

      <div className="py-2 px-4 mb-4 bg-white rounded-lg rounded-t-lg border border-gray-200 dark:bg-gray-800 dark:border-gray-700">
        <label htmlFor="comment" className="sr-only">
          Your comment
        </label>
        <textarea
          id="comment"
          rows={6}
          className="px-0 w-full text-sm text-gray-900 border-0 focus:ring-0 focus:outline-none dark:text-white dark:placeholder-gray-400 dark:bg-gray-800"
          placeholder="Write a comment..."
          {...register("content")}
        >
        </textarea>
      </div>
      <button
        type="submit"
        className="inline-flex items-center py-2.5 px-4 text-xs font-medium text-center text-white bg-lightGreen rounded-lg focus:ring-4 focus:ring-primary-200 dark:focus:ring-primary-900 hover:bg-primary-800"
      >
        Post comment
      </button>
    </form>
  )
}
//<button type=submit > => Kliknuti na Post comment aktivira handleSubmit gore naveden u onSubmit u zaglavlju <form...> */}

export default StockCommentForm;