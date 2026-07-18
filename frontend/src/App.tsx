import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Routes, Route, Navigate, useNavigate } from "react-router-dom";
import { AuthProvider, useAuth } from "./auth/authProvider";
import { useMe } from "./api/vssClient";
import { Spinner } from "./ui";

import { Login } from "./features/auth/Login";
import { Signup } from "./features/auth/Signup";
import { CheckInbox } from "./features/auth/CheckInbox";
import { LinkRecord } from "./features/auth/LinkRecord";
import { LinkSuccess } from "./features/auth/LinkSuccess";
import { VendorConsole } from "./features/vendor/VendorConsole";
import { VendorProfile } from "./features/vendor/VendorProfile";
import { ChangeSubmitted } from "./features/vendor/ChangeSubmitted";

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, refetchOnWindowFocus: false } },
});

/** Sends the user to the right place based on auth + link state. */
function RootRedirect() {
  const { isAuthenticated } = useAuth();
  const { data: me, isLoading } = useMe();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (isLoading) return <Spinner label="Loading your portal…" />;
  return <Navigate to={me?.linkState === "Linked" ? "/console" : "/link"} replace />;
}

/** Gate for authenticated app routes. */
function RequireAuth({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

function Shell() {
  useNavigate(); // ensure router context is present for children
  return (
    <Routes>
      <Route path="/" element={<RootRedirect />} />
      <Route path="/login" element={<Login />} />
      <Route path="/signup" element={<Signup />} />
      <Route path="/check-inbox" element={<CheckInbox />} />
      <Route path="/link" element={<RequireAuth><LinkRecord /></RequireAuth>} />
      <Route path="/link/success" element={<RequireAuth><LinkSuccess /></RequireAuth>} />
      <Route path="/console" element={<RequireAuth><VendorConsole /></RequireAuth>} />
      <Route path="/profile" element={<Navigate to="/profile/company" replace />} />
      <Route path="/profile/:tab" element={<RequireAuth><VendorProfile /></RequireAuth>} />
      <Route path="/submitted" element={<RequireAuth><ChangeSubmitted /></RequireAuth>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <Shell />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  );
}
