export type CommentPost = {
    title: string;
    content: string;
}
/* Uspesan CommentController Create Endpoint u .NET salje Frontendu CreatedAtAction(nameof(GetById), new { id = comment.Id }, comment.ToCommentDTO()), sto znaci da 
Frontend prima Response oblika: StatusCode = 201, Location Header = https://localhost:port/api/comment/{id} i Body = CommentDTO, ali samo Body mi treba i to samo Title i Content
polja iz CommentDTO, pa zbog "istih" imena tih polja ovde i u .NET se uspesno mapiraju. Ostala CommentDTO polja mi ne trebaju i njih nisam uzeo iz backenda.
*/