import {redirect} from "next/navigation";
import {MyPortfolioView} from "@/features/my-portfolio/MyPortfolio";
export default function Page(){if(process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED!=="true")redirect("/");return <MyPortfolioView/>;}
