// Public OIDC settings only. Session handling and PKCE login follow in a later lesson.
export const authConfig = {
  authority: process.env.NEXT_PUBLIC_OIDC_AUTHORITY,
  clientId: process.env.NEXT_PUBLIC_OIDC_CLIENT_ID,
};
