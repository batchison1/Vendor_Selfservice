import type { ReactNode } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../auth/authProvider";
import { useMe } from "../api/vssClient";

const ICONS: Record<string, string> = {
  home: "M3 9.5 12 3l9 6.5V21a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1z",
  company: "M3 21V7a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v14M3 21h18M7 9h2M7 13h2M7 17h2M12 9h1M12 13h1",
  docs: "M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8zM14 2v6h6M9 13h6M9 17h4",
  tag: "M20.6 13.4 12 22l-9-9V4h9zM7.5 7.5h.01",
};

interface NavItem { label: string; icon: keyof typeof ICONS; to: string; }

const VENDOR_NAV: NavItem[] = [
  { label: "Home", icon: "home", to: "/console" },
  { label: "Company profile", icon: "company", to: "/profile/company" },
  { label: "Documents", icon: "docs", to: "/profile/documents" },
  { label: "Category codes", icon: "tag", to: "/profile/categories" },
];

function initials(name: string) {
  return name.split(" ").map((p) => p[0]).slice(0, 2).join("").toUpperCase();
}

export function AppShell({ title, crumb, children }: { title: string; crumb: string; children: ReactNode }) {
  const nav = useNavigate();
  const loc = useLocation();
  const { account, logout } = useAuth();
  const { data: me } = useMe();

  const connected = me?.linkState === "Linked";

  return (
    <div style={{ display: "flex", minHeight: "100vh" }}>
      {/* sidebar */}
      <aside style={{ width: 236, flex: "0 0 236px", background: "var(--color-navy)", color: "#fff", display: "flex", flexDirection: "column", padding: "20px 0" }}>
        <div style={{ padding: "0 20px 22px", display: "flex", alignItems: "center", gap: 11, borderBottom: "1px solid rgba(255,255,255,.09)" }}>
          <div style={{ width: 34, height: 34, borderRadius: 6, background: "var(--color-teal)", display: "flex", alignItems: "center", justifyContent: "center", fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 17 }}>V</div>
          <div>
            <div style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 15 }}>Univerus VSS</div>
            <div style={{ fontSize: 10, letterSpacing: ".14em", textTransform: "uppercase", color: "#7AC8CC" }}>Vendor portal</div>
          </div>
        </div>

        <nav style={{ padding: "16px 12px", flex: 1, display: "flex", flexDirection: "column", gap: 2 }}>
          {VENDOR_NAV.map((item) => {
            const active = loc.pathname === item.to || (item.to.startsWith("/profile") && loc.pathname.startsWith("/profile") && item.to === loc.pathname);
            return (
              <button key={item.to} onClick={() => nav(item.to)} style={{
                width: "100%", display: "flex", alignItems: "center", gap: 11, padding: "10px 13px",
                border: "none", borderLeft: active ? "3px solid var(--color-teal)" : "3px solid transparent",
                borderRadius: 4, cursor: "pointer", background: active ? "rgba(52,167,173,.18)" : "transparent",
                color: active ? "#fff" : "rgba(255,255,255,.72)", fontFamily: "var(--font-sans)", fontSize: 14, fontWeight: active ? 600 : 500, textAlign: "left",
              }}>
                <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><path d={ICONS[item.icon]} /></svg>
                <span>{item.label}</span>
              </button>
            );
          })}
        </nav>

        <div style={{ padding: "0 14px" }}>
          <div style={{ padding: "12px 14px", borderRadius: 8, background: "rgba(255,255,255,.06)", display: "flex", alignItems: "center", gap: 11 }}>
            <div style={{ width: 32, height: 32, borderRadius: 999, background: "var(--color-teal)", display: "flex", alignItems: "center", justifyContent: "center", fontWeight: 700, fontSize: 12 }}>{initials(account?.name ?? "?")}</div>
            <div style={{ flex: 1, minWidth: 0, lineHeight: 1.25 }}>
              <div style={{ fontWeight: 600, fontSize: 13, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{account?.name}</div>
              <div style={{ fontSize: 11, color: "rgba(255,255,255,.6)", whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{me?.vendorName ?? "Northstar Supply Co."}</div>
            </div>
          </div>
          <button onClick={() => { logout(); nav("/login"); }} style={{ width: "100%", marginTop: 8, padding: 9, border: "none", borderRadius: 6, background: "transparent", color: "rgba(255,255,255,.55)", fontFamily: "var(--font-sans)", fontSize: 12, cursor: "pointer" }}>Sign out</button>
        </div>
      </aside>

      {/* main */}
      <div style={{ flex: 1, minWidth: 0, display: "flex", flexDirection: "column" }}>
        <header style={{ height: 62, flex: "0 0 62px", background: "#fff", borderBottom: "1px solid var(--border-1)", display: "flex", alignItems: "center", justifyContent: "space-between", padding: "0 28px" }}>
          <div>
            <div style={{ fontSize: 11, color: "var(--fg-3)", letterSpacing: ".06em" }}>{crumb}</div>
            <h1 style={{ fontSize: 19, lineHeight: 1.1 }}>{title}</h1>
          </div>
          <div style={{ display: "inline-flex", alignItems: "center", gap: 7, padding: "6px 12px", borderRadius: 999, background: connected ? "var(--bg-accent-soft)" : "#FFF4CC", color: connected ? "var(--color-teal-700)" : "#8A6D00", fontSize: 12, fontWeight: 600 }}>
            <span style={{ width: 8, height: 8, borderRadius: 999, background: "currentColor" }} />
            {connected ? `Linked · ${me?.vendorNumber}` : "Not linked"}
          </div>
        </header>
        <main style={{ flex: 1, overflow: "auto", padding: 28 }}>{children}</main>
      </div>
    </div>
  );
}
