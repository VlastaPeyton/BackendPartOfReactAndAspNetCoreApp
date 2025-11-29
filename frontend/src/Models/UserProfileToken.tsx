export type UserProfileToken = {
    userName :string;
    emailAddress: string;
    token: string; // JWT
}
/* Uspesan AccountController Register/Login Endpoint u .NET salje Frontendu Response oblika: StatusCode=200, a Body = NewUserDTO objekat sa UserName, Email, Token i RefreshToken poljima, 
a meni trebaju prva 3 polja, pa ih zato UserProfileToken ima. Mapiranje se automatski radi, jer ova polja i u NewUserDTO imaju "ista" imena. */