// No real credentials or external requests. Deliberately stale email claim.
export default class FixtureKeycloak {
 subject='77777777-7777-4777-8777-777777777777';token='fixture-only';tokenParsed={name:'Fixture User',email:'stale@example.test',email_verified:false};authenticated=true;
 hasRealmRole(){return false;}
 async init(){return true;}
 async updateToken(){return true;}
 async login(options:unknown){await fetch('/fixture-login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(options)});}
 clearToken(){this.authenticated=false;}
 async logout(){}
}
