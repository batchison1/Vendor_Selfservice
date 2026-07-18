import { useNavigate } from "react-router-dom";
import { AuthLayout } from "./AuthLayout";
import { Button } from "../../ui";

export function CheckInbox() {
  const nav = useNavigate();
  return (
    <AuthLayout>
      <div style={{ width: 56, height: 56, borderRadius: 12, background: "var(--bg-accent-soft)", display: "flex", alignItems: "center", justifyContent: "center", color: "var(--color-teal)", marginBottom: 22 }}>
        <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="4" width="20" height="16" rx="2" /><path d="m2 7 10 6 10-6" /></svg>
      </div>
      <h2 style={{ fontSize: 28, margin: 0 }}>Check your inbox</h2>
      <p style={{ margin: "12px 0 0", fontSize: 15, color: "var(--fg-2)", lineHeight: 1.6 }}>
        We sent a verification link to <b style={{ color: "var(--fg-1)" }}>dana@northstarsupply.com</b>. Click the link in that email to confirm your address and continue.
      </p>
      <div style={{ marginTop: 24, padding: 16, background: "var(--bg-2)", border: "1px dashed var(--border-2)", borderRadius: 8, fontSize: 13, color: "var(--fg-2)" }}>
        Didn't get it? Check spam, or <a href="#" onClick={(e) => e.preventDefault()} style={{ fontWeight: 600 }}>resend the link</a>.
      </div>
      <Button variant="teal" style={{ width: "100%", marginTop: 24 }} onClick={() => nav("/link")}>
        I clicked the link (demo) →
      </Button>
    </AuthLayout>
  );
}
