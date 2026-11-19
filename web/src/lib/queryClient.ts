import { QueryClient } from "@tanstack/react-query";
import axios from "axios";

/**
 * React Query keeps server data in a cache: pages that need the same data (dashboard and profile both show the
 * profile) share one request, and a save refreshes what it changed.
 */
export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        // Retrying a 4xx can't help (the request itself is wrong, or the data isn't there); retry network blips only.
        retry: (failureCount, error) => {
          const status = axios.isAxiosError(error) ? error.response?.status : undefined;
          return status !== undefined && status < 500 ? false : failureCount < 2;
        },
        refetchOnWindowFocus: false,
      },
    },
  });
}
