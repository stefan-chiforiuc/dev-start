import { createRootRoute, createRoute, Outlet } from "@tanstack/react-router";
import { IndexPage } from "./routes/index";

const rootRoute = createRootRoute({
  component: () => <Outlet />,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: "/",
  component: IndexPage,
});

export const routeTree = rootRoute.addChildren([indexRoute]);
