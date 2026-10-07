import "server-only";
export async function getApiHealth(): Promise<"healthy" | "unavailable"> {
  try {
    const response = await fetch(
      `${process.env.API_INTERNAL_URL ?? "http://localhost:5005"}/health`,
      { cache: "no-store", signal: AbortSignal.timeout(3000) },
    );
    return response.ok ? "healthy" : "unavailable";
  } catch {
    return "unavailable";
  }
}
