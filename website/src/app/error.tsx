"use client";
export default function ErrorPage({reset}:{reset:()=>void}){return <main id="content" className="welcome"><h1>Site şu anda yüklenemiyor.</h1><p>Lütfen biraz sonra yeniden deneyin.</p><button onClick={()=>reset()}>Tekrar dene</button></main>}
