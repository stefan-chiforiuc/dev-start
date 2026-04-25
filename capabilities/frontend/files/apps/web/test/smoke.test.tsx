import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { IndexPage } from "../src/routes/index";

describe("IndexPage", () => {
  it("renders the project name", () => {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });
    render(
      <QueryClientProvider client={client}>
        <IndexPage />
      </QueryClientProvider>,
    );
    expect(screen.getByText("{{Name}}")).toBeInTheDocument();
  });
});
