import { getApiHealth } from "../../services/health";
export async function GET() {
  const status = await getApiHealth();
  return Response.json(
    { application: "BuildingFlow Web", api: status },
    { status: status === "healthy" ? 200 : 503 },
  );
}
