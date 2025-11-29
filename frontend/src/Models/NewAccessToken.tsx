// AxiosWithJWTForBacked.post expected return type kao sto sam radio u Services folderu
export type NewAccessToken = {
  accessToken: string; // Jer BE vraca Ok(new {accessToken=...}), pa mora isto ime argumenta
};