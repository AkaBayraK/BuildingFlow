// Test-only adapter: no real credentials, OIDC endpoints or account mutations.
export default class FixtureKeycloak {
  subject='77777777-7777-4777-8777-777777777777';
  tokenParsed={name:'Test Kullanıcısı',preferred_username:'fixture'};
  authenticated=true;
  hasRealmRole(role:string){return ['SITE_ADMIN','ACCOUNTANT'].includes(role);}
  async init(){return true;}
  async updateToken(){return true;}
  async login(){}
  clearToken(){this.authenticated=false;}
  logout(){throw new Error('Fixture logout must not be used as real Keycloak acceptance.');}
}
