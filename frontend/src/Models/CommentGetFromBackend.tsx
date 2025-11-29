export type CommentGetFromBackend = {
    title: string;
    content: string;
    createdBy: string;
}
/* Uspesan CommentController GetAll Endpoint u .NET salje Frontendu Ok(commentDTOs), sto znaci da Frontend prima Response oblika: StatusCode=200, Headers = nebitno i Body = commentDTOs niz.
CommentDTO je element niza koji sadrzi razna polja, a meni samo trebaju Title,Content i CreatedBy, pa zbog "istih" imena tih polja ovde autoatski se mapira. Ostala CommentDTO polja mi ne trebaju
i njih nisam uzeo iz backenda. */