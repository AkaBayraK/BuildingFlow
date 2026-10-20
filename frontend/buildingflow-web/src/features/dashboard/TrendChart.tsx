"use client";
import {useEffect,useRef,useState} from "react";
import {useTranslations} from "next-intl";
import {formatDecimalCurrency} from "@/lib/format";
import {plot,type DashboardSummary} from "./model";
import {trendScale,trendBarLayout} from "./trend-bars";
type Locale="tr"|"en";
const monthLabel=(period:string,locale:Locale)=>new Intl.DateTimeFormat(locale==="en"?"en-GB":"tr-TR",{month:"short",timeZone:"UTC"}).format(new Date(period+"-01T00:00:00Z"));
export function TrendChart({data,locale}:{data:DashboardSummary;locale:Locale}){
 const t=useTranslations("dashboard"),[months,setMonths]=useState(6),[hover,setHover]=useState<number|null>(null),box=useRef<HTMLDivElement>(null),[width,setWidth]=useState(640);
 useEffect(()=>{const el=box.current;if(!el)return;const observer=new ResizeObserver(([entry])=>setWidth(Math.max(240,Math.round(entry.contentRect.width))));observer.observe(el);return()=>observer.disconnect();},[]);
 const rows=data.trend.slice(-months),height=width<480?220:250,top=14,bottom=32,plotHeight=height-top-bottom,layout=trendBarLayout(width,rows.length),scale=trendScale(rows.flatMap(row=>[plot(row.receipts),plot(row.expenses)]));
 const series=[{key:"receipts" as const,label:t("income"),color:"var(--chart-income)"},{key:"expenses" as const,label:t("expense"),color:"var(--chart-expense)"}];
 const y=(value:number)=>top+plotHeight*(1-value/scale.max),axis=(value:number)=>new Intl.NumberFormat(locale==="en"?"en-GB":"tr-TR",{notation:"compact",maximumFractionDigits:1}).format(value);
 return <section className="dashboard-panel dashboard-trend-panel" aria-labelledby="dash-trend"><div className="dashboard-trend-heading"><h2 id="dash-trend">{t("trendTitle")}</h2><label><span className="sr-only">{t("trendRange")}</span><select aria-label={t("trendRange")} value={months} onChange={e=>{setHover(null);setMonths(Number(e.target.value));}}>{[6,3].map(count=><option key={count} value={count}>{t("lastMonths",{count})}</option>)}</select></label></div>
 <div className="dashboard-chart dashboard-bar-chart" ref={box}><ul className="dashboard-legend">{series.map(s=><li key={s.key}><span style={{background:s.color}}/>{s.label}</li>)}</ul>
 <svg viewBox={`0 0 ${width} ${height}`} role="group" aria-label={t("trendTitle")} onMouseLeave={()=>setHover(null)}>
  {[0,.25,.5,.75,1].map(f=><g key={f}><line x1={layout.left} x2={width-layout.right} y1={y(scale.max*f)} y2={y(scale.max*f)} className="dashboard-grid-line"/><text x={layout.left-10} y={y(scale.max*f)+4} textAnchor="end" className="dashboard-axis">{axis(scale.max*f)}</text></g>)}
  {rows.map((row,i)=>{const center=layout.center(i),label=`${monthLabel(row.period,locale)} ${row.period.slice(0,4)} · ${t("income")}: ${formatDecimalCurrency(row.receipts,locale)} · ${t("expense")}: ${formatDecimalCurrency(row.expenses,locale)}`;return <g key={row.period} className="dashboard-bar-month" role="img" aria-label={label} tabIndex={0} onMouseEnter={()=>setHover(i)} onPointerDown={()=>setHover(i)} onFocus={()=>setHover(i)} onBlur={()=>setHover(null)} onKeyDown={e=>{if(e.key==="Escape")setHover(null);}}>
   <rect x={center-layout.slot*.46} y={top} width={layout.slot*.92} height={plotHeight} rx={6} className="dashboard-bar-hit"/>
   {series.map((s,index)=>{const value=plot(row[s.key]),x=center+(index===0?-layout.gap/2-layout.bar:layout.gap/2);return <rect key={s.key} className="dashboard-trend-bar" x={x} y={y(value)} width={layout.bar} height={Math.max(0,plotHeight*value/scale.max)} rx={4} fill={s.color}><title>{s.label}: {formatDecimalCurrency(row[s.key],locale)}</title></rect>;})}
   <text x={center} y={height-9} textAnchor="middle" className="dashboard-axis">{monthLabel(row.period,locale)}</text>
  </g>;})}
 </svg>
 {scale.empty&&<p className="dashboard-chart-empty">{t("trendEmpty")}</p>}
 {hover!==null&&rows[hover]&&<div className="dashboard-tooltip dashboard-bar-tooltip" style={{left:`${Math.max(20,Math.min(80,layout.center(hover)/width*100))}%`}} role="status"><strong>{monthLabel(rows[hover].period,locale)} {rows[hover].period.slice(0,4)}</strong>{series.map(s=><span key={s.key}><i style={{background:s.color}}/>{s.label}: {formatDecimalCurrency(rows[hover][s.key],locale)}</span>)}</div>}
 <table className="sr-only"><caption>{t("trendTitle")}</caption><thead><tr><th>{t("month")}</th><th>{t("income")}</th><th>{t("expense")}</th></tr></thead><tbody>{rows.map(row=><tr key={row.period}><td>{row.period}</td><td>{formatDecimalCurrency(row.receipts,locale)}</td><td>{formatDecimalCurrency(row.expenses,locale)}</td></tr>)}</tbody></table>
 </div><p className="dashboard-panel-note">{t("trendBasis")}</p></section>;
}
