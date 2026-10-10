// Public settings only; never put client secrets here.
export const authConfig = {
  authority: process.env.NEXT_PUBLIC_OIDC_AUTHORITY ?? "http://localhost:8080/realms/buildingflow",
  clientId: process.env.NEXT_PUBLIC_OIDC_CLIENT_ID ?? "buildingflow-web",
};
