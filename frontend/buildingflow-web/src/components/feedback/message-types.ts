export type MessageKind = "success" | "error" | "warning" | "info";
export const messageSemantics = {
  success: {role:"status",live:"polite",icon:"CircleCheck"},
  error: {role:"alert",live:"assertive",icon:"CircleX"},
  warning: {role:"status",live:"polite",icon:"TriangleAlert"},
  info: {role:"status",live:"polite",icon:"Info"}
} as const;
export function kindForStatus(status:number):MessageKind{return status===409?"warning":"error";}
