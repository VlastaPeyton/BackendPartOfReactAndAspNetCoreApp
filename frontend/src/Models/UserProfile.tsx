export type UserProfile = {
    userName: string;
    emailAddress: string;
}

/* Uspesan AccountController Register/Login Endpoint u .NET salje Frontendu Response oblika: StatusCode=200, a Body = NewUserDTO objekat sa UserName, Email, Token poljima, 
a meni ne trebaju sva 3 polja i zato Token polje ne uzimam, pa zato ga nema u UserProfile. Mapiranje se automatski radi, jer ova polja i u NewUserDTO imaju "ista" imena. */