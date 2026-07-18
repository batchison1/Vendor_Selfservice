import type { CSSProperties, ButtonHTMLAttributes, InputHTMLAttributes, SelectHTMLAttributes, ReactNode } from "react";

/**
 * App-local presentational primitives styled with Univerus v4 tokens. On the
 * Univerus network these are the seam to swap for the real component library's
 * design components; the data/config/auth layer already uses the real APIs.
 */

type BtnVariant = "primary" | "teal" | "outline" | "ghost" | "success" | "danger";
const btnStyles: Record<BtnVariant, CSSProperties> = {
  primary: { background: "var(--color-navy)", color: "#fff", border: "none" },
  teal: { background: "var(--color-teal)", color: "#fff", border: "none" },
  success: { background: "var(--status-success)", color: "#fff", border: "none" },
  danger: { background: "#fff", color: "var(--status-danger)", border: "1px solid var(--border-2)" },
  outline: { background: "#fff", color: "var(--fg-1)", border: "1px solid var(--border-2)" },
  ghost: { background: "transparent", color: "var(--fg-2)", border: "none" },
};

export function Button({
  variant = "teal", style, ...rest
}: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: BtnVariant }) {
  return (
    <button
      {...rest}
      style={{
        display: "inline-flex", alignItems: "center", justifyContent: "center", gap: 8,
        padding: "10px 18px", borderRadius: "var(--radius-md)", fontFamily: "var(--font-sans)",
        fontWeight: 600, fontSize: 14, cursor: rest.disabled ? "not-allowed" : "pointer",
        opacity: rest.disabled ? 0.6 : 1, ...btnStyles[variant], ...style,
      }}
    />
  );
}

export function Card({ children, style }: { children: ReactNode; style?: CSSProperties }) {
  return (
    <div style={{ background: "#fff", border: "1px solid var(--border-1)", borderRadius: "var(--radius-lg)", ...style }}>
      {children}
    </div>
  );
}

export function CardHeader({ title, hint, right }: { title: string; hint?: string; right?: ReactNode }) {
  return (
    <div style={{ padding: "18px 22px", borderBottom: "1px solid var(--border-1)", display: "flex", alignItems: "center", justifyContent: "space-between" }}>
      <div>
        <div style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 16, color: "var(--fg-1)" }}>{title}</div>
        {hint && <div style={{ fontSize: 13, color: "var(--fg-2)", marginTop: 3 }}>{hint}</div>}
      </div>
      {right}
    </div>
  );
}

const PILL_COLORS: Record<string, [string, string]> = {
  approved: ["#DFF3E8", "#19663F"], active: ["#DFF3E8", "#19663F"], linked: ["#DFF3E8", "#19663F"],
  current: ["#DFF3E8", "#19663F"], synced: ["#DFF3E8", "#19663F"],
  pendingreview: ["#FFF4CC", "#8A6D00"], pending: ["#FFF4CC", "#8A6D00"], inreview: ["#FFF4CC", "#8A6D00"],
  awaitingdocs: ["#FFF4CC", "#8A6D00"], expiring: ["#FFF4CC", "#8A6D00"], pendinglink: ["#FFF4CC", "#8A6D00"],
  rejected: ["#FBE3E1", "#8A231E"], expired: ["#FBE3E1", "#8A231E"], error: ["#FBE3E1", "#8A231E"],
  matched: ["#D6EFF0", "#1F6F73"], verified: ["#D6EFF0", "#1F6F73"],
};

export function StatusPill({ status }: { status: string }) {
  const key = status.toLowerCase().replace(/[^a-z]/g, "");
  const [bg, fg] = PILL_COLORS[key] ?? ["#E6E7E8", "#4D4D4F"];
  return (
    <span style={{ display: "inline-block", padding: "3px 11px", borderRadius: "var(--radius-pill)", fontSize: 11, fontWeight: 600, background: bg, color: fg, whiteSpace: "nowrap" }}>
      {status}
    </span>
  );
}

export function Banner({ tone = "info", children }: { tone?: "info" | "success" | "warn"; children: ReactNode }) {
  const map = {
    info: ["var(--bg-accent-soft)", "var(--color-teal-700)", "1px solid var(--color-teal-300)"],
    success: ["#DFF3E8", "#19663F", "1px solid #A8D8BE"],
    warn: ["#FFF4CC", "#8A6D00", "1px solid #F0D060"],
  }[tone];
  return (
    <div style={{ background: map[0], color: map[1], border: map[2], borderRadius: 8, padding: "12px 16px", fontSize: 14, marginBottom: 16 }}>
      {children}
    </div>
  );
}

export function Label({ children }: { children: ReactNode }) {
  return (
    <label style={{ display: "block", fontSize: 12, fontWeight: 600, textTransform: "uppercase", letterSpacing: ".06em", color: "var(--fg-2)", marginBottom: 6 }}>
      {children}
    </label>
  );
}

const fieldBox: CSSProperties = {
  width: "100%", padding: "10px 12px", border: "1px solid var(--border-1)",
  borderRadius: "var(--radius-md)", fontSize: 14, color: "var(--fg-1)", outline: "none", background: "#fff",
};

export function TextField(props: InputHTMLAttributes<HTMLInputElement>) {
  return <input {...props} style={{ ...fieldBox, ...props.style }} />;
}

export function SelectField({ options, ...rest }: SelectHTMLAttributes<HTMLSelectElement> & { options: string[] }) {
  return (
    <select {...rest} style={{ ...fieldBox, ...rest.style }}>
      {options.map((o) => <option key={o} value={o}>{o}</option>)}
    </select>
  );
}

export function ReadonlyField({ value }: { value?: string | null }) {
  return (
    <div style={{ ...fieldBox, background: "var(--bg-2)", color: "var(--fg-2)" }}>{value ?? "—"}</div>
  );
}

export function Spinner({ label = "Loading…" }: { label?: string }) {
  return <div style={{ padding: 40, color: "var(--fg-3)", fontSize: 14 }}>{label}</div>;
}
