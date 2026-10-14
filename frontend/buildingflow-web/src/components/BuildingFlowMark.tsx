import geometry from "@/brand/buildingflow-mark.json";

export function BuildingFlowMark({size = 44, variant = "primary"}: {size?: number; variant?: "primary" | "white" | "light"}) {
  const colors = variant === "light" ? {navy: "#F5FAFF", blue: "#69BFE4", teal: "#8BD0D6"} : variant === "white" ? {navy: "#FFFFFF", blue: "#FFFFFF", teal: "#FFFFFF"} : geometry.colors;
  return <svg width={size} height={size} viewBox={geometry.viewBox} aria-hidden="true" focusable="false">
    {geometry.paths.map((path, index) => <path key={index} d={path.d} fill="none" stroke={colors[path.tone as keyof typeof geometry.colors]} strokeWidth={geometry.strokeWidth} strokeLinecap="round" strokeLinejoin="round" />)}
  </svg>;
}
