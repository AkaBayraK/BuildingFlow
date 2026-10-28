"use client";
import {useIdentity} from '@/auth/AuthGate';
import {PropertyModuleSelection} from '@/features/navigation/PropertyModuleSelection';
import {MyPortfolioView} from '@/features/my-portfolio/MyPortfolio';
export default function Page(){const identity=useIdentity();return ['SUPER_ADMIN','SITE_ADMIN','MANAGER'].some(r=>identity.hasRealmRole(r))?<PropertyModuleSelection section="common-areas"/>:<MyPortfolioView/>;}
