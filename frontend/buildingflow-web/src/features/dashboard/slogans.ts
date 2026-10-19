// İŞ20: the top bar shows one of these project-written slogans (TR/EN keys ui.shellSlogan1..N), chosen once per page load.
export const SLOGAN_COUNT=8;
/** Maps a random number in [0,1) to a slogan number 1..SLOGAN_COUNT; out-of-range input falls back to the first slogan. */
export function pickSlogan(random:number){return Number.isFinite(random)&&random>=0&&random<1?Math.floor(random*SLOGAN_COUNT)+1:1;}
