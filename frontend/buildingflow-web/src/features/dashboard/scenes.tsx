// İŞ20: Dashboard backgrounds are drawn by project code (SVG), so no third-party image or licence is added.
// One scene is chosen per page load; the shapes are seeded, so a scene always looks the same.
export type Scene={id:string;tone:"light"|"dark";sky:[string,string,string];body:string;light:"sun"|"moon";building:[string,string];window:string;lit:string;litRatio:number;tree:[string,string];ground:string;stars:boolean;seed:number};
export const SCENES:Scene[]=[
 {id:"morning",tone:"light",sky:["#cfe3ff","#eaf3ff","#f8fbff"],body:"#fde68a",light:"sun",building:["#e9dccd","#d8c6b3"],window:"#9cc3ee",lit:"#fff4c2",litRatio:.08,tree:["#6aa56b","#4c8a55"],ground:"#e6efe3",stars:false,seed:7},
 {id:"noon",tone:"light",sky:["#8fc4f7","#c9e4fb","#eef7ff"],body:"#fff3b0",light:"sun",building:["#f2ede6","#c9a587"],window:"#6fa7dc",lit:"#ffffff",litRatio:.04,tree:["#5c9d5f","#3f7f4a"],ground:"#dde9d6",stars:false,seed:19},
 {id:"sunset",tone:"light",sky:["#f9b58a","#fcd9a8","#fbeee0"],body:"#fb923c",light:"sun",building:["#c99a7a","#a9775a"],window:"#f3c58c",lit:"#ffe08a",litRatio:.3,tree:["#7b8f4e","#5e7340"],ground:"#efd9c4",stars:false,seed:31},
 {id:"dusk",tone:"dark",sky:["#1e2a5c","#4b3a7a","#9a6ea8"],body:"#f5e6c8",light:"moon",building:["#2c2f4f","#3b3560"],window:"#4a4f7a",lit:"#ffd78a",litRatio:.55,tree:["#25344a","#1d2a3c"],ground:"#1f2740",stars:true,seed:43},
 {id:"night",tone:"dark",sky:["#0b1430","#14234a","#1f3563"],body:"#e8edf7",light:"moon",building:["#1a2440","#22305a"],window:"#2a3866",lit:"#ffcf6e",litRatio:.62,tree:["#132036","#0f1a2c"],ground:"#111a30",stars:true,seed:57},
 {id:"spring",tone:"light",sky:["#bfe6dc","#e3f4ef","#f7fcfa"],body:"#fff1a8",light:"sun",building:["#efe6f3","#c7b6d6"],window:"#8fb8e0",lit:"#fff6d1",litRatio:.06,tree:["#7cc47a","#4fa36a"],ground:"#e2f2df",stars:false,seed:71},
];
/** Maps a random number in [0,1) to a scene; anything else falls back to the first scene. */
export function pickScene(random:number){return SCENES[Number.isFinite(random)&&random>=0&&random<1?Math.floor(random*SCENES.length):0];}
function rng(seed:number){let s=seed>>>0;return()=>{s=(s*1664525+1013904223)>>>0;return s/4294967296;};}
export function SceneArt({scene}:{scene:Scene}){
 const r=rng(scene.seed),W=1200,H=340,base=300;
 const towers:{x:number;w:number;h:number;cols:number;front:boolean}[]=[];
 for(let x=430,i=0;x<W;i++){const front=i%2===1,w=front?120+r()*50:90+r()*40,h=front?150+r()*90:120+r()*70;towers.push({x,w,h,cols:Math.max(3,Math.floor(w/26)),front});x+=w*(front?.92:.7);}
 const stars=scene.stars?Array.from({length:40},()=>({x:r()*W,y:r()*160,o:.3+r()*.6})):[];
 const trees=Array.from({length:14},(_,i)=>({x:380+i*62+r()*30,s:16+r()*18}));
 const g=`sky-${scene.id}`;
 return <svg className="dashboard-scene-art" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="xMaxYMax slice" aria-hidden="true" focusable="false">
  <defs><linearGradient id={g} x1="0" y1="0" x2="0" y2="1"><stop offset="0" stopColor={scene.sky[0]}/><stop offset=".6" stopColor={scene.sky[1]}/><stop offset="1" stopColor={scene.sky[2]}/></linearGradient></defs>
  <rect width={W} height={H} fill={`url(#${g})`}/>
  {stars.map((s,i)=><circle key={i} cx={s.x} cy={s.y} r={1.2} fill="#fff" opacity={s.o}/>)}
  <circle cx={scene.light==="sun"?560:620} cy={scene.light==="sun"?90:70} r={scene.light==="sun"?46:30} fill={scene.body} opacity={scene.light==="sun"?.85:.95}/>
  {scene.light==="moon"&&<circle cx={632} cy={62} r={26} fill={scene.sky[0]}/>}
  {towers.filter(t=>!t.front).concat(towers.filter(t=>t.front)).map((t,i)=>{const fill=t.front?scene.building[0]:scene.building[1],floors=Math.floor((t.h-24)/22),cw=(t.w-16)/t.cols;
   return <g key={i}><rect x={t.x} y={base-t.h} width={t.w} height={t.h} fill={fill} rx={3}/>
    <rect x={t.x-4} y={base-t.h-6} width={t.w+8} height={8} fill={scene.building[1]} rx={2}/>
    {Array.from({length:floors},(_,f)=>Array.from({length:t.cols},(_,c)=><rect key={`${f}-${c}`} x={t.x+8+c*cw+3} y={base-t.h+14+f*22} width={cw-6} height={13} rx={1.5} fill={r()<scene.litRatio?scene.lit:scene.window} opacity={t.front?.95:.75}/>))}
    {t.front&&Array.from({length:floors},(_,f)=><rect key={`b${f}`} x={t.x+4} y={base-t.h+28+f*22} width={t.w-8} height={2} fill={scene.building[1]} opacity={.7}/>)}
   </g>;})}
  <rect x={0} y={base} width={W} height={H-base} fill={scene.ground}/>
  {trees.map((t,i)=><g key={i}><rect x={t.x-2} y={base-t.s*.6} width={4} height={t.s*.8} fill={scene.tree[1]}/><circle cx={t.x} cy={base-t.s} r={t.s} fill={i%2?scene.tree[0]:scene.tree[1]}/><circle cx={t.x+t.s*.6} cy={base-t.s*.6} r={t.s*.7} fill={scene.tree[0]}/></g>)}
 </svg>;
}
