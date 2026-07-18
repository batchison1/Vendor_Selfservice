import { useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { AppShell } from "../../layout/AppShell";
import { Card, Button, Spinner, StatusPill } from "../../ui";
import { useAdminChangeRequest, adminApi, adminQk } from "../../api/adminClient";

export function AdminChangeDetail() {
  const { id = "" } = useParams();
  const nav = useNavigate();
  const qc = useQueryClient();
  const { data: cr, isLoading } = useAdminChangeRequest(id);
  const [note, setNote] = useState("");

  const done = async () => {
    await Promise.all([
      qc.invalidateQueries({ queryKey: adminQk.stats }),
      qc.invalidateQueries({ queryKey: adminQk.changeRequests }),
      qc.invalidateQueries({ queryKey: adminQk.vendors }),
    ]);
    nav("/admin/change-requests");
  };
  const approve = useMutation({ mutationFn: () => adminApi.approveChange(id, note), onSuccess: done });
  const reject = useMutation({ mutationFn: () => adminApi.rejectChange(id, note), onSuccess: done });

  if (isLoading || !cr) return <AppShell title="Review change" crumb="Administration"><Spinner /></AppShell>;

  const decided = cr.status === "Approved" || cr.status === "Rejected";

  return (
    <AppShell title="Review change" crumb="Administration">
      <a href="#" onClick={(e) => { e.preventDefault(); nav("/admin/change-requests"); }} style={{ fontSize: 13, color: "var(--fg-2)" }}>← Back to change requests</a>
      <div style={{ display: "grid", gridTemplateColumns: "1.6fr 1fr", gap: 20, marginTop: 14, alignItems: "start" }}>
        <Card>
          <div style={{ padding: "18px 22px", borderBottom: "1px solid var(--border-1)", display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <div>
              <div style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 17 }}>{cr.vendorName} · {cr.section}</div>
              <div style={{ fontSize: 13, color: "var(--fg-2)", marginTop: 3 }}>{cr.code} · submitted by {cr.submittedByName} · {new Date(cr.submittedAt).toLocaleString()}</div>
            </div>
            <StatusPill status={cr.status} />
          </div>
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead><tr style={{ background: "var(--bg-2)" }}>
              {["Field", "Current (ERP)", "Requested"].map((c) => <th key={c} style={th}>{c}</th>)}
            </tr></thead>
            <tbody>
              {cr.diffs.map((d) => (
                <tr key={d.field} style={{ borderBottom: "1px solid #F0F1F2" }}>
                  <td style={{ ...td, fontWeight: 600 }}>{d.field}</td>
                  <td style={{ ...td, color: "var(--fg-2)", textDecoration: "line-through", fontFamily: "var(--font-mono)" }}>{d.fromValue}</td>
                  <td style={{ ...td, fontFamily: "var(--font-mono)" }}><span style={{ background: "#DFF3E8", color: "#19663F", padding: "2px 8px", borderRadius: 4, fontWeight: 600 }}>{d.toValue}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>

        <Card style={{ padding: 22, position: "sticky", top: 0 }}>
          <div style={{ fontFamily: "var(--font-display)", fontWeight: 600, fontSize: 15, marginBottom: 6 }}>Review decision</div>
          <p style={{ fontSize: 13, color: "var(--fg-2)", lineHeight: 1.5, margin: "0 0 14px" }}>Approving sends a PUT to the ERP vendor master via the configured endpoint.</p>
          <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "10px 12px", borderRadius: 8, background: "var(--bg-2)", fontSize: 12, color: "var(--fg-2)", marginBottom: 16, fontFamily: "var(--font-mono)" }}>PUT /vendors/·/master</div>
          {decided ? (
            <div style={{ fontSize: 14, color: "var(--fg-2)" }}>This request is <b>{cr.status}</b>.</div>
          ) : (
            <>
              <textarea placeholder="Add a note (optional)" value={note} onChange={(e) => setNote(e.target.value)} style={{ width: "100%", minHeight: 72, padding: "10px 12px", border: "1px solid var(--border-1)", borderRadius: 6, fontSize: 13, color: "var(--fg-1)", outline: "none", resize: "vertical" }} />
              <Button variant="teal" style={{ width: "100%", marginTop: 14 }} disabled={approve.isPending || reject.isPending} onClick={() => approve.mutate()}>
                {approve.isPending ? "Approving…" : "Approve & push to ERP"}
              </Button>
              <Button variant="danger" style={{ width: "100%", marginTop: 10 }} disabled={approve.isPending || reject.isPending} onClick={() => reject.mutate()}>
                {reject.isPending ? "Rejecting…" : "Reject change"}
              </Button>
            </>
          )}
        </Card>
      </div>
    </AppShell>
  );
}

const th = { padding: "10px 22px", textAlign: "left" as const, fontSize: 11, fontWeight: 600, textTransform: "uppercase" as const, letterSpacing: ".1em", color: "var(--fg-2)", borderBottom: "1px solid var(--border-1)" };
const td = { padding: "14px 22px", fontSize: 13, color: "var(--fg-1)" };
