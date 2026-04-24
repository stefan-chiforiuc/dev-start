import { useQuery } from "@tanstack/react-query";

const apiBase = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000";

export function IndexPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ["health"],
    queryFn: async () => {
      const res = await fetch(`${apiBase}/health`);
      if (!res.ok) throw new Error(`${res.status}`);
      return (await res.json()) as { status: string };
    },
  });

  return (
    <main style={{ fontFamily: "system-ui", padding: 40 }}>
      <h1>{{Name}}</h1>
      <p>API status: {isLoading ? "…" : error ? "unreachable" : data?.status}</p>
    </main>
  );
}
